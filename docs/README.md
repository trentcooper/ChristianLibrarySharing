# Christian Library Sharing System

A .NET-based platform enabling Christians to share books from their personal libraries.

## Project Structure

```
ChristianLibrarySharing/
├── src/
│   ├── ChristianLibrary.Domain/       # Domain models
│   ├── ChristianLibrary.Data/         # Data access
│   ├── ChristianLibrary.Services/     # Business logic
│   ├── ChristianLibrary.API/          # REST API
│   ├── ChristianLibrary.Web/          # Blazor Web UI
│   └── ChristianLibrary.Common/       # Utilities
├── tests/
│   ├── ChristianLibrary.UnitTests/
│   ├── ChristianLibrary.IntegrationTests/
│   └── ChristianLibrary.E2ETests/
└── docs/
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- SQL Server 2019+ or PostgreSQL 13+
- Visual Studio 2022 / VS Code / Rider

### Setup

1. Restore dependencies:
   ```bash
   dotnet restore
   ```

2. Build the solution:
   ```bash
   dotnet build
   ```

3. Run the API:
   ```bash
   dotnet run --project src/ChristianLibrary.API
   ```

4. Run the Web application:
   ```bash
   dotnet run --project src/ChristianLibrary.Web
   ```

## Testing

Run all tests:
```bash
dotnet test
```

## Architecture

This solution follows Clean Architecture principles:

- **Domain Layer**: Pure business entities and interfaces
- **Data Layer**: Repository pattern, EF Core
- **Services Layer**: Business logic and workflows
- **API Layer**: REST endpoints
- **Web Layer**: Blazor UI
- **Common Layer**: Shared utilities

## Next Steps

1. Configure Entity Framework Core (US-01.02)
2. Set up authentication infrastructure (Epic 2)
3. Start implementing domain models

## Documentation

See the /docs folder for additional documentation.
