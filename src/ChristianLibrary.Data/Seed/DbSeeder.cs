using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChristianLibrary.Data.Seed;

public class DbSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<DbSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            // Ensure database is created
            await _context.Database.MigrateAsync();

            // Seed in order
            await SeedRolesAsync();
            await SeedAdminUserAsync();
            await SeedSampleBooksAsync();
            await SeedSampleLoansAsync(); // NEW: Seed sample loans

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        _logger.LogInformation("Seeding roles...");

        string[] roles = { "Admin", "Member" };

        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole(roleName);
                var result = await _roleManager.CreateAsync(role);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {RoleName}", roleName);
                }
                else
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}",
                        roleName,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogInformation("Role {RoleName} already exists", roleName);
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        _logger.LogInformation("Seeding admin user...");

        const string adminEmail = "admin@christianlibrary.com";
        const string adminPassword = "Admin@123"; // TODO: Change in production!

        var adminUser = await _userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true,
                IsDeleted = false
            };

            var result = await _userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Created admin user: {Email}", adminEmail);

                // Add to Admin role
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation("Added admin user to Admin role");

                // Create user profile
                var profile = new UserProfile
                {
                    UserId = adminUser.Id,
                    FirstName = "System",
                    LastName = "Administrator",
                    Bio = "System administrator account",
                    Visibility = ProfileVisibility.Public
                };

                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created admin user profile");
            }
            else
            {
                _logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            _logger.LogInformation("Admin user already exists");
        }
    }

    private async Task SeedSampleBooksAsync()
    {
        _logger.LogInformation("Seeding sample books...");

        if (await _context.Books.AnyAsync())
        {
            _logger.LogInformation("Books already exist, skipping sample data");
            return;
        }

        // Get admin user as the owner of sample books
        var adminUser = await _userManager.FindByEmailAsync("admin@christianlibrary.com");
        if (adminUser == null)
        {
            _logger.LogWarning("Admin user not found, skipping book seeding");
            return;
        }

        var sampleBooks = new List<Book>
        {
            new Book
            {
                Title = "Mere Christianity",
                Author = "C.S. Lewis",
                Isbn = "978-0060652920",
                Publisher = "HarperOne",
                PublicationYear = 1952,
                Description =
                    "A theological book by C.S. Lewis, adapted from a series of BBC radio talks made between 1941 and 1944.",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Good,
                IsAvailable = true,
                OwnerId = adminUser.Id
            },
            new Book
            {
                Title = "The Cost of Discipleship",
                Author = "Dietrich Bonhoeffer",
                Isbn = "978-0684815008",
                Publisher = "Touchstone",
                PublicationYear = 1937,
                Description = "A compelling statement of the demands of sacrifice and ethical consistency from a man whose life ended in martyrdom.",
                Genre = BookGenre.ChristianLiving,
                Condition = BookCondition.VeryGood,
                IsAvailable = true,
                OwnerId = adminUser.Id
            },
            new Book
            {
                Title = "Knowing God",
                Author = "J.I. Packer",
                Isbn = "978-0830816507",
                Publisher = "InterVarsity Press",
                PublicationYear = 1973,
                Description =
                    "A classic work on the character and nature of God, and the implications for Christian living.",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Good,
                IsAvailable = true,
                OwnerId = adminUser.Id
            },
            new Book
            {
                Title = "The Pilgrim's Progress",
                Author = "John Bunyan",
                Isbn = "978-0140430196",
                Publisher = "Penguin Classics",
                PublicationYear = 1678,
                Description =
                    "A Christian allegory following the journey of Christian from the City of Destruction to the Celestial City.",
                Genre = BookGenre.Fiction,
                Condition = BookCondition.Acceptable,
                IsAvailable = true,
                OwnerId = adminUser.Id
            },
            new Book
            {
                Title = "The Screwtape Letters",
                Author = "C.S. Lewis",
                Isbn = "978-0060652937",
                Publisher = "HarperOne",
                PublicationYear = 1942,
                Description =
                    "A series of letters from a senior demon to his nephew, providing insight into Christian spiritual warfare.",
                Genre = BookGenre.Fiction,
                Condition = BookCondition.VeryGood,
                IsAvailable = false, // Mark as unavailable for variety
                OwnerId = adminUser.Id
            },
            new Book
            {
                Title = "Basic Christianity",
                Author = "John Stott",
                Isbn = "978-0830834136",
                Publisher = "InterVarsity Press",
                PublicationYear = 1958,
                Description = "A clear, classic presentation of the essential truths of Christianity.",
                Genre = BookGenre.Apologetics,
                Condition = BookCondition.Good,
                IsAvailable = true,
                OwnerId = adminUser.Id
            }
        };

        _context.Books.AddRange(sampleBooks);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} sample books", sampleBooks.Count);
    }

    /// <summary>
    /// Seeds sample loan data for testing borrowing functionality
    /// Creates a sample borrower user and 2 borrow requests (1 approved, 1 pending)
    /// </summary>
    private async Task SeedSampleLoansAsync()
    {
        _logger.LogInformation("Seeding sample borrow requests...");

        // Check if borrow requests already exist
        if (await _context.BorrowRequests.AnyAsync())
        {
            _logger.LogInformation("Borrow requests already exist, skipping seeding");
            return;
        }

        // Get admin user (will be the lender)
        var adminUser = await _userManager.FindByEmailAsync("admin@christianlibrary.com");
        if (adminUser == null)
        {
            _logger.LogWarning("Admin user not found, cannot seed borrow requests");
            return;
        }

        // Create a borrower user if one doesn't exist
        const string borrowerEmail = "borrower@test.com";
        const string borrowerPassword = "Borrower@123";
        var borrowerUser = await _userManager.FindByEmailAsync(borrowerEmail);

        if (borrowerUser == null)
        {
            _logger.LogInformation("Creating sample borrower user...");

            borrowerUser = new ApplicationUser
            {
                UserName = borrowerEmail,
                Email = borrowerEmail,
                EmailConfirmed = true,
                IsActive = true,
                IsDeleted = false
            };

            var result = await _userManager.CreateAsync(borrowerUser, borrowerPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Created borrower user: {Email}", borrowerEmail);

                // Add to Member role
                await _userManager.AddToRoleAsync(borrowerUser, "Member");

                // Create borrower profile
                var borrowerProfile = new UserProfile
                {
                    UserId = borrowerUser.Id,
                    FirstName = "John",
                    LastName = "Borrower",
                    Bio = "Sample borrower user for testing",
                    City = "Portland",
                    State = "OR",
                    ZipCode = "97201",
                    Visibility = ProfileVisibility.Public
                };

                _context.UserProfiles.Add(borrowerProfile);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created borrower user profile");
            }
            else
            {
                _logger.LogError("Failed to create borrower user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }
        }
        else
        {
            _logger.LogInformation("Borrower user already exists");
        }

        // Get first 2 available books to create borrow requests for
        var books = await _context.Books
            .Where(b => b.OwnerId == adminUser.Id && b.IsAvailable)
            .Take(2)
            .ToListAsync();

        if (!books.Any())
        {
            _logger.LogWarning("No available books found for admin user, cannot seed borrow requests");
            return;
        }

        var sampleRequests = new List<BorrowRequest>();
        var now = DateTime.UtcNow;

        // Request 1: Approved request with loan period starting yesterday, due in 5 days
        if (books.Count >= 1)
        {
            sampleRequests.Add(new BorrowRequest
            {
                BookId = books[0].Id,
                BorrowerId = borrowerUser.Id,
                LenderId = adminUser.Id,
                Status = BorrowRequestStatus.Approved,
                RequestedStartDate = now.AddDays(-1),
                RequestedEndDate = now.AddDays(5), // Due in 5 days
                Message = "I'd love to read this classic work on Christian apologetics!",
                ResponseMessage = "Approved! Enjoy the book.",
                RespondedAt = now.AddDays(-1),
                ExpiresAt = now.AddDays(7) // Request expires in 7 days
            });

            // Mark book as unavailable since it's approved
            books[0].IsAvailable = false;
        }

        // Request 2: Pending request (not yet approved)
        if (books.Count >= 2)
        {
            sampleRequests.Add(new BorrowRequest
            {
                BookId = books[1].Id,
                BorrowerId = borrowerUser.Id,
                LenderId = adminUser.Id,
                Status = BorrowRequestStatus.Pending,
                RequestedStartDate = now,
                RequestedEndDate = now.AddDays(14), // Requesting for 2 weeks
                Message = "This looks like a great read! May I borrow it?",
                ResponseMessage = null, // No response yet
                RespondedAt = null,
                ExpiresAt = now.AddDays(3) // Request expires in 3 days if not responded to
            });

            // Don't mark as unavailable - it's just pending
        }

        _context.BorrowRequests.AddRange(sampleRequests);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} sample borrow requests (1 approved, 1 pending)", sampleRequests.Count);
    }
}