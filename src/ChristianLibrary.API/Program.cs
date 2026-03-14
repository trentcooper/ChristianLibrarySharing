using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using ChristianLibrary.API.Configuration;
using ChristianLibrary.Services.Configuration;
using ChristianLibrary.Services;
using ChristianLibrary.Services.Interfaces;
using ChristianLibrary.Data.Context;
using ChristianLibrary.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

// Create the logger first (before building the app)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up ChristianLibrary.API");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings.json
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.File(
            path: "Logs/christianlibrary-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        ));

    // Add services to the container with JSON options to handle circular references
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.WriteIndented = true;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        // Add file upload support - MUST BE FIRST!
        options.MapType<IFormFile>(() => new OpenApiSchema
        {
            Type = "string",
            Format = "binary"
        });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter 'Bearer' followed by a space and your JWT token. Example: Bearer eyJhbGc..."
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Configure Database
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json");
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));

    // Configure Identity
    builder.Services
        .AddIdentityConfiguration()
        .ConfigureIdentityTokenLifespan(TimeSpan.FromHours(24));

    // Configure JWT Settings
    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
    if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
    {
        throw new InvalidOperationException("JWT settings are not properly configured in appsettings.json");
    }

    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

    // Configure JWT Authentication
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Error("JWT Authentication failed: {Message}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var userName = context.Principal?.Identity?.Name ?? "Unknown";
                    Log.Information("JWT Token validated successfully for user: {User}", userName);
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    var token = authHeader?.Split(" ").Last();
                    if (token != null && token.Length > 20)
                    {
                        Log.Information("JWT Token received: {TokenPreview}...", token.Substring(0, 20));
                    }

                    return Task.CompletedTask;
                }
            };
        });

    // Register application services
    // Register application services
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IJwtService, JwtService>();
    builder.Services.AddScoped<IBookService, BookService>();
    
    // Register ImageSharp-based image processing service
    builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();

    // Configure ProfileService with uploads path (factory registration)
    builder.Services.AddScoped<IProfileService>(sp =>
    {
        var context = sp.GetRequiredService<ApplicationDbContext>();
        var logger = sp.GetRequiredService<ILogger<ProfileService>>();
        var imageService = sp.GetRequiredService<IImageProcessingService>();
    
        // Path for profile pictures (relative to wwwroot)
        var uploadsPath = Path.Combine(
            Directory.GetCurrentDirectory(), 
            "wwwroot", 
            "uploads", 
            "profile-pictures"
        );
    
        return new ProfileService(context, logger, imageService, uploadsPath);
    });
    
    // Add CORS - put this with your other builder.Services calls
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowReactDevServer",
            policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
    });

    // Isbn Lookup Service
    builder.Services.AddHttpClient<IIsbnLookupService, IsbnLookupService>();

    WebApplication app;
    try
    {
        Log.Information("🚀 Building application...");
        app = builder.Build();
        Log.Information("✅ Application built successfully");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot set default value"))
    {
        // Specific handler for type mismatch errors in Entity Configuration
        Log.Fatal("╔════════════════════════════════════════════════════════════╗");
        Log.Fatal("║  ⚠️  ENTITY CONFIGURATION ERROR DETECTED  ⚠️              ║");
        Log.Fatal("╚════════════════════════════════════════════════════════════╝");
        Log.Fatal("");
        Log.Fatal("🔍 This usually means:");
        Log.Fatal("   1. A Fluent API configuration has wrong type for default value");
        Log.Fatal("   2. Check *Configuration.cs files in Data/Configurations/");
        Log.Fatal("   3. Look for HasDefaultValue() with mismatched type");
        Log.Fatal("");
        Log.Fatal("📋 Error Details:");
        Log.Fatal("   {Message}", ex.Message);
        Log.Fatal("");

        // Extract property name from error message
        if (ex.Message.Contains("property '"))
        {
            var propStart = ex.Message.IndexOf("property '") + 10;
            var propEnd = ex.Message.IndexOf("'", propStart);
            if (propEnd > propStart)
            {
                var propertyName = ex.Message.Substring(propStart, propEnd - propStart);
                Log.Fatal("👉 CHECK CONFIGURATION FOR PROPERTY: {Property}", propertyName);
                Log.Fatal("");
            }
        }

        // Extract entity name from error message
        if (ex.Message.Contains("entity type '"))
        {
            var entityStart = ex.Message.IndexOf("entity type '") + 13;
            var entityEnd = ex.Message.IndexOf("'", entityStart);
            if (entityEnd > entityStart)
            {
                var entityName = ex.Message.Substring(entityStart, entityEnd - entityStart);
                Log.Fatal("👉 CHECK FILE: {Entity}Configuration.cs", entityName);
                Log.Fatal("");
            }
        }

        Log.Fatal("💡 TIP: Look for .HasDefaultValue(\"string\") when property is an enum!");
        Log.Fatal("");

        throw; // Re-throw so EF tools see it
    }
    catch (Exception ex)
    {
        Log.Fatal("╔════════════════════════════════════════════════════════════╗");
        Log.Fatal("║  ❌  APPLICATION BUILD FAILED  ❌                         ║");
        Log.Fatal("╚════════════════════════════════════════════════════════════╝");
        Log.Fatal("");
        Log.Fatal("Error Type: {Type}", ex.GetType().Name);
        Log.Fatal("Error Message: {Message}", ex.Message);
        Log.Fatal("");

        // Log all inner exceptions
        var innerEx = ex.InnerException;
        var depth = 1;
        while (innerEx != null)
        {
            Log.Fatal("Inner Exception #{Depth}:", depth);
            Log.Fatal("  Type: {Type}", innerEx.GetType().Name);
            Log.Fatal("  Message: {Message}", innerEx.Message);
            Log.Fatal("");
            innerEx = innerEx.InnerException;
            depth++;
        }

        throw;
    }

    Log.Information("🔧 Configuring HTTP request pipeline...");
    

    //Seed the database
    Log.Information("Starting database seeding...");
    await app.Services.SeedDatabaseAsync();
    

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        };
    });

    app.UseHttpsRedirection();
    app.UseStaticFiles(); 

    app.UseAuthentication(); // Must come before UseAuthorization
    // Use CORS - put this BEFORE app.UseAuthorization()
    app.UseCors("AllowReactDevServer");
    app.UseAuthorization();

    app.MapControllers();

    // Test endpoint to verify logging
    app.MapGet("/test-logging", (ILogger<Program> logger) =>
    {
        logger.LogTrace("This is a TRACE log");
        logger.LogDebug("This is a DEBUG log");
        logger.LogInformation("This is an INFORMATION log");
        logger.LogWarning("This is a WARNING log");
        logger.LogError("This is an ERROR log");
        logger.LogCritical("This is a CRITICAL log");

        return Results.Ok(new
        {
            message = "Logging test completed. Check console and Logs/ folder for output.",
            timestamp = DateTime.UtcNow
        });
    });

    Log.Information("ChristianLibrary.API started successfully");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}