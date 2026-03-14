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
                CreatedAt = DateTime.UtcNow,
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
                    Visibility = ProfileVisibility.Public, // ← ADDED THIS LINE
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
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
                Description = "A theological book by C.S. Lewis, adapted from a series of BBC radio talks made between 1941 and 1944.",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Good,
                IsAvailable = true,
                OwnerId = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                OwnerId = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Book
            {
                Title = "Knowing God",
                Author = "J.I. Packer",
                Isbn = "978-0830816507",
                Publisher = "InterVarsity Press",
                PublicationYear = 1973,
                Description = "A classic work on the character and nature of God, and the implications for Christian living.",
                Genre = BookGenre.Theology,
                Condition = BookCondition.Good,
                IsAvailable = true,
                OwnerId = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Book
            {
                Title = "The Pilgrim's Progress",
                Author = "John Bunyan",
                Isbn = "978-0140430196",
                Publisher = "Penguin Classics",
                PublicationYear = 1678,
                Description = "A Christian allegory following the journey of Christian from the City of Destruction to the Celestial City.",
                Genre = BookGenre.Fiction,
                Condition = BookCondition.Acceptable,
                IsAvailable = true,
                OwnerId = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Book
            {
                Title = "The Screwtape Letters",
                Author = "C.S. Lewis",
                Isbn = "978-0060652937",
                Publisher = "HarperOne",
                PublicationYear = 1942,
                Description = "A series of letters from a senior demon to his nephew, providing insight into Christian spiritual warfare.",
                Genre = BookGenre.Fiction,
                Condition = BookCondition.VeryGood,
                IsAvailable = false, // Mark as unavailable for variety
                OwnerId = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                OwnerId = adminUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Books.AddRange(sampleBooks);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} sample books", sampleBooks.Count);
    }
}