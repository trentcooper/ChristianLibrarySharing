using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Notifications.Interfaces;
using ChristianLibrary.Notifications.Messages;
using ChristianLibrary.Notifications.Rules;
using ChristianLibrary.Services.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChristianLibrary.Services.BackgroundServices
{
    /// <summary>
    /// Daily background job (US-06.10) that evaluates open loans and publishes due-date / overdue
    /// reminder notifications. Thin orchestrator: it owns scheduling and all I/O (query loans, build
    /// context, publish, stamp the loan) and delegates every decision to <see cref="ReminderRuleEvaluator"/>
    /// and message-shaping to <see cref="LoanReminderNotificationFactory"/>.
    /// </summary>
    /// <remarks>
    /// Scheduling sleeps until the next run hour, then runs one pass; restart-safe and cannot drift or
    /// double-fire. "Today" is UTC, to match the UTC-stored Loan.DueDate. Delivery contract: per-loan,
    /// publish-then-stamp-then-save, at-least-once; each loan is isolated so one failure can't sink the batch.
    /// </remarks>
    public sealed class ReminderBackgroundService : BackgroundService
    {
        private static readonly LoanStatus[] OpenStatuses =
            [LoanStatus.Active, LoanStatus.Overdue, LoanStatus.ExtensionRequested];

        private const int CourtesyLeadDays = 3;   // earliest tier (DueSoon) fires at T-3

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ReminderRuleEvaluator _evaluator;
        private readonly LoanReminderNotificationFactory _notificationFactory;
        private readonly INotificationPublisher _publisher;
        private readonly ReminderServiceSettings _settings;
        private readonly ILogger<ReminderBackgroundService> _logger;

        public ReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ReminderRuleEvaluator evaluator,
            LoanReminderNotificationFactory notificationFactory,
            INotificationPublisher publisher,
            IOptions<ReminderServiceSettings> settings,
            ILogger<ReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _evaluator = evaluator;
            _notificationFactory = notificationFactory;
            _publisher = publisher;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.Enabled)
            {
                _logger.LogInformation("Reminder service disabled via configuration; not scheduling.");
                return;
            }

            // TODO(cloud-hardening: TD-203, TD-204): single-instance assumption. With more than one
            // instance running, this loop would double-fire; needs a distributed lock / leader election.
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var delay = NextRunTime(now, _settings.RunHour) - now;

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;   // shutting down
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    await ProcessRemindersAsync(DateTime.UtcNow.Date, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reminder run failed.");
                }
            }
        }

        /// <summary>The next occurrence of the run hour at or after <paramref name="now"/>.</summary>
        internal static DateTime NextRunTime(DateTime now, int runHour)
        {
            var todayRun = now.Date.AddHours(runHour);
            return now < todayRun ? todayRun : todayRun.AddDays(1);
        }

        /// <summary>Opens a scope for the scoped DbContext, then runs one pass.</summary>
        private async Task ProcessRemindersAsync(DateTime today, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await ProcessLoansAsync(db, today, cancellationToken);
        }

        /// <summary>
        /// One reminder pass over the candidate loans. Context-injected so the smoke test can drive it
        /// against an in-memory database without the hosting scope machinery.
        /// </summary>
        internal async Task ProcessLoansAsync(ApplicationDbContext db, DateTime today, CancellationToken cancellationToken)
        {
            var cutoff = today.Date.AddDays(CourtesyLeadDays);

            var candidates = await db.Loans
                .Where(l => !l.IsDeleted)
                .Where(l => l.ReturnedDate == null)
                .Where(l => OpenStatuses.Contains(l.Status))
                .Where(l => l.DueDate <= cutoff)
                .Include(l => l.Book)
                .Include(l => l.Borrower).ThenInclude(u => u.Profile)
                .Include(l => l.Lender).ThenInclude(u => u.Profile)
                .ToListAsync(cancellationToken);

            var rules = SystemDefaultReminderRules.All;
            var published = 0;

            foreach (var loan in candidates)
            {
                try
                {
                    var borrowerProfile = loan.Borrower.Profile;
                    var lenderProfile = loan.Lender.Profile;

                    if (borrowerProfile is null || lenderProfile is null)
                    {
                        _logger.LogWarning(
                            "Loan {LoanId} is missing borrower or lender profile data; skipping.", loan.Id);
                        continue;
                    }

                    var context = new ReminderEvaluationContext(
                        BorrowerOptedOutOfCourtesyReminders: !borrowerProfile.NotifyOnDueDate,
                        LenderOptedOutOfReminderCopies: !lenderProfile.NotifyOnLoanReminderCopies);

                    var firedRule = _evaluator.Evaluate(loan, today, rules, context);
                    if (firedRule is null)
                        continue;

                    var notification = _notificationFactory.Create(loan, firedRule, today, context);
                    await _publisher.PublishAsync(notification, cancellationToken);

                    // Publish succeeded -> stamp with the ACTUAL days-from-due (not the rule's nominal offset).
                    loan.LastReminderCategory = firedRule.Category;
                    loan.LastReminderOffsetDays = (today.Date - loan.DueDate.Date).Days;
                    loan.LastReminderSentAt = DateTime.UtcNow;
                    loan.RemindersSent += 1;

                    await db.SaveChangesAsync(cancellationToken);
                    published++;
                }
                catch (Exception ex)
                {
                    // Per-loan isolation: one bad loan must not sink the batch.
                    _logger.LogError(ex, "Failed to process reminder for loan {LoanId}.", loan.Id);
                }
            }

            _logger.LogInformation(
                "Reminder pass complete for {Date:yyyy-MM-dd}: {Candidates} candidate loan(s), {Published} reminder(s) published.",
                today.Date, candidates.Count, published);
        }
    }
}