# Database Seeding Documentation

## Overview

The ChristianLibrary API implements automated database seeding to populate initial data required for development and testing. Seeding runs automatically on application startup and is designed to be idempotent (safe to run multiple times).

---

## What Gets Seeded

### 1. Roles (2)

| Role Name | Description |
|-----------|-------------|
| **Admin** | System administrators with full access |
| **Member** | Regular users of the Christian Library system |

### 2. Admin User (1)

| Property | Value |
|----------|-------|
| **Email** | admin@christianlibrary.com |
| **Password** | Admin@123 ⚠️ **Change in production!** |
| **Username** | admin@christianlibrary.com |
| **Email Confirmed** | true |
| **Role** | Admin |
| **Is Active** | true |

**User Profile:**
- First Name: System
- Last Name: Administrator
- Bio: System administrator account

### 3. Sample Books (6)

| Title | Author | Genre | Condition | ISBN | Available |
|-------|--------|-------|-----------|------|-----------|
| Mere Christianity | C.S. Lewis | Theology | Good | 978-0060652920 | Yes |
| The Cost of Discipleship | Dietrich Bonhoeffer | Christian Living | Very Good | 978-0684815008 | Yes |
| Knowing God | J.I. Packer | Theology | Good | 978-0830816507 | Yes |
| The Pilgrim's Progress | John Bunyan | Allegory | Acceptable | 978-0140430196 | Yes |
| The Screwtape Letters | C.S. Lewis | Fiction | Very Good | 978-0060652937 | No |
| Basic Christianity | John Stott | Apologetics | Good | 978-0830834136 | Yes |

All books are owned by the admin user.

---

## Implementation Architecture

### File Structure

```
src/
├── ChristianLibrary.Data/
│   ├── Seed/
│   │   └── DbSeeder.cs                 # Core seeding logic
│   └── Extensions/
│       └── DatabaseExtensions.cs       # DI extension method
│
└── ChristianLibrary.API/
    └── Program.cs                      # Seeding integration
```

### Key Classes

#### **DbSeeder.cs**
Location: `src/ChristianLibrary.Data/Seed/DbSeeder.cs`

**Responsibilities:**
- Seed roles into ASP.NET Identity
- Create and configure admin user
- Create admin user profile
- Seed sample books with proper relationships

**Dependencies:**
- `ApplicationDbContext` - Database access
- `UserManager<ApplicationUser>` - User management
- `RoleManager<IdentityRole>` - Role management
- `ILogger<DbSeeder>` - Structured logging

**Key Methods:**
```csharp
public async Task SeedAsync()                  // Main entry point
private async Task SeedRolesAsync()            // Seeds Admin and Member roles
private async Task SeedAdminUserAsync()        // Seeds admin user with profile
private async Task SeedSampleBooksAsync()      // Seeds 6 sample books
```

#### **DatabaseExtensions.cs**
Location: `src/ChristianLibrary.Data/Extensions/DatabaseExtensions.cs`

**Responsibilities:**
- Provide extension method for IServiceProvider
- Handle dependency injection scope creation
- Centralized error handling for seeding process

**Usage:**
```csharp
await app.Services.SeedDatabaseAsync();
```

---

## Seeding Flow

### Startup Sequence

```
1. Application starts (Program.cs)
   ↓
2. Services configured (DI container)
   ↓
3. App built (builder.Build())
   ↓
4. SeedDatabaseAsync() called
   ↓
5. DbSeeder instantiated with dependencies
   ↓
6. Database migrations applied
   ↓
7. Roles seeded (if not exist)
   ↓
8. Admin user seeded (if not exist)
   ↓
9. Sample books seeded (if none exist)
   ↓
10. Application continues startup
```

### Idempotency Strategy

Each seeding method checks for existing data before inserting:

**Roles:**
```csharp
if (!await _roleManager.RoleExistsAsync(roleName))
{
    // Create role
}
```

**Admin User:**
```csharp
var adminUser = await _userManager.FindByEmailAsync(adminEmail);
if (adminUser == null)
{
    // Create admin user
}
```

**Books:**
```csharp
if (await _context.Books.AnyAsync())
{
    // Skip seeding - books already exist
    return;
}
```

---

## Logging

All seeding operations include structured logging using Serilog:

### Log Levels Used

| Level | When Used |
|-------|-----------|
| **Information** | Successful operations, progress updates |
| **Warning** | Expected issues (e.g., data already exists, admin user not found for books) |
| **Error** | Failed operations with exception details |

### Example Log Output

```
[INF] Starting database seeding...
[INF] Seeding roles...
[INF] Created role: Admin
[INF] Created role: Member
[INF] Seeding admin user...
[INF] Created admin user: admin@christianlibrary.com
[INF] Added admin user to Admin role
[INF] Created admin user profile
[INF] Seeding sample books...
[INF] Seeded 6 sample books
[INF] Database seeding completed successfully
```

### Structured Logging Examples

```csharp
_logger.LogInformation("Created role: {RoleName}", roleName);
_logger.LogInformation("Seeded {Count} sample books", sampleBooks.Count);
_logger.LogError(ex, "An error occurred while seeding the database");
```

---

## Configuration

### Modifying Seeded Data

#### Change Admin Credentials

Edit `src/ChristianLibrary.Data/Seed/DbSeeder.cs` lines 87-88:

```csharp
const string adminEmail = "youradmin@example.com";
const string adminPassword = "YourSecurePassword123!";
```

⚠️ **Security Note:** Never commit production credentials to source control. Consider using:
- Environment variables
- Azure Key Vault
- User Secrets (for development)

#### Add Additional Roles

Edit `src/ChristianLibrary.Data/Seed/DbSeeder.cs` line 55:

```csharp
string[] roles = { "Admin", "Member", "Moderator", "LibraryOwner" };
```

#### Modify Sample Books

Edit the `sampleBooks` list in `DbSeeder.cs` starting at line 160:

```csharp
var sampleBooks = new List<Book>
{
    new Book
    {
        Title = "Your Book Title",
        Author = "Author Name",
        ISBN = "978-XXXXXXXXXX",
        Publisher = "Publisher Name",
        PublicationYear = 2024,
        Description = "Book description",
        Genre = "Genre",
        Condition = BookCondition.Good,
        IsAvailable = true,
        OwnerId = adminUser.Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    // Add more books...
};
```

#### Disable Sample Books (Production)

Comment out the book seeding call in `DbSeeder.cs` line 43:

```csharp
// await SeedSampleBooksAsync();  // Disable for production
```

---

## Integration with Program.cs

### Configuration in Program.cs

```csharp
// After app.Build()
Log.Information("Starting database seeding...");
await app.Services.SeedDatabaseAsync();
```

**Important:** Seeding must occur:
1. ✅ After `app.Build()` - app instance exists
2. ✅ Before `app.Run()` - before request handling starts
3. ✅ After DbContext registration - services available

### Required Service Registrations

```csharp
// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity (required for UserManager and RoleManager)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
```

---

## Testing and Verification

### Manual Verification

#### 1. Check Console Logs
Look for successful seeding messages on application startup.

#### 2. Query Database
```sql
-- Check roles
SELECT * FROM AspNetRoles;

-- Check admin user
SELECT * FROM AspNetUsers WHERE Email = 'admin@christianlibrary.com';

-- Check user roles
SELECT u.Email, r.Name
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id;

-- Check books
SELECT COUNT(*) FROM Books;  -- Should return 6
SELECT Title, Author, Genre FROM Books ORDER BY Title;
```

#### 3. Test API Endpoints
```bash
# Get all books
GET https://localhost:7xxx/api/books

# Should return 6 books
```

### Automated Testing

Consider creating integration tests for seeding:

```csharp
[Fact]
public async Task Seeding_CreatesExpectedRoles()
{
    // Arrange
    var services = CreateTestServices();
    
    // Act
    await services.SeedDatabaseAsync();
    
    // Assert
    var roles = await _roleManager.Roles.ToListAsync();
    Assert.Equal(2, roles.Count);
    Assert.Contains(roles, r => r.Name == "Admin");
    Assert.Contains(roles, r => r.Name == "Member");
}
```

---

## Error Handling

### Common Issues and Solutions

#### Issue: "Connection string not found"
**Cause:** `appsettings.json` missing or incorrect
**Solution:** Verify `ConnectionStrings:DefaultConnection` exists in `appsettings.json`

#### Issue: "Cannot insert duplicate key"
**Cause:** Attempting to seed data that already exists
**Solution:** Seeding should be idempotent - check logs for which check failed

#### Issue: "Admin user not found when seeding books"
**Cause:** Admin user creation failed but books seeding continued
**Solution:** Check admin user seeding logs for errors; may need to fix user creation first

#### Issue: Identity errors when creating admin user
**Cause:** Password doesn't meet complexity requirements
**Solution:** Update password in `DbSeeder.cs` or adjust password requirements in `Program.cs`

### Exception Handling

All seeding methods include try-catch blocks:

```csharp
try
{
    // Seeding logic
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error occurred while seeding...");
    throw;  // Re-throw to prevent app from starting with partial data
}
```

---

## Security Considerations

### Development vs Production

| Aspect | Development | Production |
|--------|-------------|------------|
| **Admin Password** | Simple (Admin@123) | Complex, from Key Vault |
| **Sample Books** | Enabled | Disabled |
| **Seeding on Startup** | Enabled | May be disabled |
| **Logging Level** | Debug | Information |

### Best Practices

1. **Never commit production credentials** to source control
2. **Change default admin password** immediately after first deployment
3. **Disable sample data** in production environments
4. **Use environment-specific configuration** (appsettings.Production.json)
5. **Consider separate seeding scripts** for production data
6. **Implement proper role-based access control** (RBAC) after seeding

### Production Deployment

For production, consider:

```csharp
// Only seed in Development
if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDatabaseAsync();
}
```

Or use a separate migration/seeding script:
```bash
dotnet run --project src/ChristianLibrary.Data.Seeder
```

---

## Maintenance

### Adding New Seed Data

1. Create new method in `DbSeeder.cs`:
```csharp
private async Task SeedNewEntityAsync()
{
    _logger.LogInformation("Seeding new entity...");
    
    if (await _context.NewEntities.AnyAsync())
    {
        _logger.LogInformation("New entities already exist");
        return;
    }
    
    // Add seeding logic
}
```

2. Call from `SeedAsync()`:
```csharp
await SeedNewEntityAsync();
```

3. Test locally before deploying

### Removing Seed Data

To remove all seeded data and start fresh:

```sql
-- Delete in reverse order of dependencies
DELETE FROM Books;
DELETE FROM UserProfiles;
DELETE FROM AspNetUserRoles;
DELETE FROM AspNetUsers;
DELETE FROM AspNetRoles;
```

Then restart the application to re-seed.

---

## Performance Considerations

### Seeding Performance

Current implementation is optimized for small datasets:
- 2 roles
- 1 admin user
- 6 books

**For large datasets**, consider:
1. **Bulk inserts** instead of individual Add() calls
2. **Disabling change tracking** during seeding
3. **Separate seeding process** (console app or migration)
4. **Database backup/restore** instead of seeding

### Example Bulk Insert

```csharp
_context.ChangeTracker.AutoDetectChangesEnabled = false;
_context.Books.AddRange(largeBookList);
await _context.SaveChangesAsync();
_context.ChangeTracker.AutoDetectChangesEnabled = true;
```

---

## Future Enhancements

Potential improvements to the seeding infrastructure:

1. **Configuration-based seeding** - Load seed data from JSON/XML files
2. **Environment-specific seeds** - Different data for Dev/Test/Staging
3. **Versioned seeding** - Track which seeds have been applied
4. **Seed rollback** - Ability to undo seeding operations
5. **Seed health checks** - Verify seeded data integrity
6. **Performance monitoring** - Log seeding duration
7. **Conditional seeding** - Based on feature flags or environment variables

---

## Related Documentation

- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Identity](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [Serilog Structured Logging](https://github.com/serilog/serilog/wiki)
- [Project README](../README.md)
- US-01.02: Configure Entity Framework Core and database
- US-01.05: Configure logging with Serilog

---

## Change Log

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2026-01-05 | 1.0 | Initial seeding implementation | Trent Cooper |

---

## Contact

For questions or issues related to database seeding:
- Review this documentation first
- Check application logs for seeding errors
- Verify database state with SQL queries
- Review seeding code in `DbSeeder.cs`

---

**Last Updated:** January 5, 2026  
**Status:** ✅ Complete and Tested  
**User Story:** US-01.02 (part of EF Core configuration)
