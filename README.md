# ChristianLibrarySharing

A full-stack book sharing platform built with .NET 9 and React, demonstrating modern enterprise architecture patterns, clean code principles, and cloud-ready design.

## 🎯 Project Purpose

This project showcases my ability to architect and build production-grade full-stack applications using modern .NET and React technologies, with a focus on:
- Enterprise-level architecture and design patterns
- Secure authentication and authorization
- RESTful API design
- Modern frontend development with React
- Database design and Entity Framework Core
- Test-driven development practices

## ✨ Technical Implementation

### Backend (.NET 9 Web API)
- **Authentication & Authorization**: JWT-based auth with ASP.NET Core Identity
- **Clean Architecture**: Separation of concerns across API, Services, Domain, and Data layers
- **Repository Pattern**: Generic repository with Unit of Work for data access
- **Entity Framework Core**: Code-first approach with migrations and seed data
- **Image Processing**: Profile picture upload with thumbnail generation
- **RESTful API Design**: Following best practices with proper HTTP methods and status codes

### Frontend (React Admin Portal)
- **React 18** with modern hooks (useState, useEffect, useContext)
- **Authentication Flow**: Login, protected routes, JWT token management
- **Admin Features**: User management, book approval queue, dashboard analytics
- **API Integration**: Centralized API service with error handling
- **Responsive Design**: Mobile-friendly interface

### Database (SQL Server)
- **Domain Entities**: Users, Books, Loans, Borrow Requests, Messages, Notifications
- **Relationships**: Proper foreign keys and navigation properties
- **Enums**: Type-safe status tracking (BookCondition, LoanStatus, etc.)
- **Audit Fields**: Created/Updated timestamps on all entities

### Testing Strategy
- **Unit Tests**: Repository and service layer testing
- **Integration Tests**: End-to-end API testing
- **E2E Tests**: Full application workflow testing

## 🏗️ Architecture Highlights

**Clean Architecture Principles:**
```
┌─────────────────────────────────────┐
│         API Layer                    │
│  (Controllers, Configuration)        │
├─────────────────────────────────────┤
│       Services Layer                 │
│  (Business Logic, DTOs)              │
├─────────────────────────────────────┤
│       Domain Layer                   │
│  (Entities, Interfaces, Enums)       │
├─────────────────────────────────────┤
│        Data Layer                    │
│  (EF Core, Repositories, Migrations) │
└─────────────────────────────────────┘
```

**Key Design Patterns:**
- Repository Pattern with Unit of Work
- Dependency Injection throughout
- DTO pattern for API responses
- Service layer abstraction
- Configuration-based settings

## 🔧 Technology Stack

**Backend:**
- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core 9
- ASP.NET Core Identity
- JWT Bearer Authentication
- SixLabors.ImageSharp (image processing)
- SQL Server

**Frontend:**
- React 18
- Vite (build tool)
- React Router (navigation)
- Axios (HTTP client)
- CSS3

**Development Tools:**
- Visual Studio / Rider / VS Code
- Git & GitHub
- Azure DevOps (project management)
- SQL Server Management Studio

## 🚀 Features Implemented

### Authentication & Authorization
- User registration with email confirmation
- JWT-based login/logout
- Role-based access control (Admin, User)
- Password reset functionality
- Secure token management

### User Management (Admin)
- View all registered users
- User profile management
- Role assignment
- Account status tracking

### Book Management
- Book approval queue for admins
- Book listing and details
- Book condition tracking
- Availability status

### Profile Management
- Profile picture upload with thumbnails
- Location settings
- Notification preferences
- Privacy settings (profile visibility)

### Dashboard
- User statistics
- Book statistics
- Recent activity tracking

## 🎓 Key Learning Outcomes

Building this project deepened my expertise in:
- **Full-stack .NET Development**: Modern API design with .NET 9
- **Authentication Patterns**: Implementing secure JWT-based auth from scratch
- **Database Design**: Complex entity relationships with EF Core
- **React Development**: Building production-quality admin interfaces
- **Clean Architecture**: Maintaining separation of concerns at scale
- **Image Processing**: Server-side image manipulation and optimization
- **API Design**: RESTful principles and proper HTTP semantics

## 🔮 Planned Enhancements

**AI Integration** (In Progress):
- Azure OpenAI-powered book recommendations
- Semantic search for book discovery
- AI-generated book summaries

**Additional Features**:
- Real-time notifications with SignalR
- Public-facing book discovery interface
- Advanced search and filtering
- Book reviews and ratings
- Loan history and analytics

**Infrastructure**:
- Azure App Service deployment
- CI/CD pipeline with GitHub Actions
- Docker containerization
- Application Insights monitoring

## 💼 Why This Project Matters

This project demonstrates my ability to:
- ✅ Architect full-stack applications from the ground up
- ✅ Implement modern .NET and React development patterns
- ✅ Build secure, scalable APIs with proper authentication
- ✅ Design and manage complex data relationships
- ✅ Write clean, maintainable, well-organized code
- ✅ Follow enterprise best practices and design patterns
- ✅ Work across the entire technology stack

This is a **portfolio demonstration project** showcasing technical architecture and implementation skills rather than a production-deployed application. The focus is on code quality, architectural decisions, and modern development practices.

## 📝 Local Development Setup

### Prerequisites
- .NET 9 SDK
- Node.js 18+
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 / Rider / VS Code

### Backend Setup
```bash
cd src/ChristianLibrary.API
dotnet restore
dotnet ef database update
dotnet run
```

API runs at: `https://localhost:7001`

### Frontend Setup
```bash
cd src/ChristianLibrary.AdminPortal
npm install
npm run dev
```

Admin portal runs at: `http://localhost:5173`

### Default Admin Credentials
Created via database seeder:
- Email: `admin@christianlibrary.com`
- Password: `Admin@123`

## 📫 Contact

**Trent Cooper**
- LinkedIn: [linkedin.com/in/trent-s-cooper](https://linkedin.com/in/trent-s-cooper)
- Email: trent.cooper@example.com
- GitHub: [@trentcooper](https://github.com/trentcooper)

---

*This project represents approximately 3 months of development work (November 2025 - January 2026) and showcases modern full-stack .NET development capabilities.*