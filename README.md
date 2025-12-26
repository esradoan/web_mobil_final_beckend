# Smart Campus Backend System

Smart Campus is a comprehensive university management system designed to streamline academic operations, attendance tracking, and campus services. This repository contains the **Backend API** built with ASP.NET Core.

## 🚀 Features

### Core Modules
- **Authentication**: JWT-based auth with Role-Based Access Control (Student, Faculty, Admin).
- **Academic Management**: Courses, Sections, Enrollments, Grades, Transcripts.
- **Attendance Tracking**: QR Code & GPS-based attendance with real-time validation.
- **Campus Life**: Meal menu management, event scheduling, and digital wallet.

### Advanced Features (Part 4)
- **Notification System**: Real-time alerts via SignalR + Database & Email fallback.
- **Analytics & Reporting**: PDF/Excel exports for attendance and section reports.
- **IoT Integration**: Real-time sensor monitoring (Temperature, Noise, Light) for classrooms.

## 🛠️ Tech Stack

- **Framework**: .NET 8 (ASP.NET Core Web API)
- **Database**: MySQL (Entity Framework Core 8)
- **Real-time**: SignalR (WebSockets)
- **Reporting**: QuestPDF, CSV Helper
- **Testing**: xUnit, Moq, EF Core InMemory
- **Documentation**: Swagger/OpenAPI

## 📦 Installation

### Prerequisites
- .NET 8 SDK
- MySQL Server (8.0+)
- Visual Studio 2022 or VS Code

### Steps
1. **Clone the repository**
   ```bash
   git clone https://github.com/your-repo/smart-campus-backend.git
   cd smart-campus-backend
   ```

2. **Configure Database**
   Update `appsettings.json` with your MySQL connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=smart_campus_db;User=root;Password=your_password;"
   }
   ```

3. **Run Migrations & Start**
   The application automatically handles migrations on startup.
   ```bash
   cd SmartCampus.API
   dotnet run
   ```

4. **Access Swagger**
   Open your browser at `http://localhost:5226/swagger`

## 🧪 Running Tests

```bash
cd SMARTCAMPUS.Tests
dotnet test
```

## 👥 Contributors
- **Backend Team**: [Your Name]
- **Frontend Team**: [Name]

## 📄 License
MIT License
