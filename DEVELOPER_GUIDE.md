# Developer Guide

## 🧱 Project Structure

- **SmartCampus.API**: The API Layer.
  - `Controllers/`: HTTP Endpoints (keep them thin).
  - `Hubs/`: SignalR Hubs.
- **SmartCampus.Business**: The core logic.
  - `Services/`: Business rules, validation, DB calls.
  - `DTOs/`: Shapes of data sent to/from API.
- **SmartCampus.DataAccess**: Database configuration.
- **SmartCampus.Entities**: Database models.

## 🤝 Coding Conventions

- **Naming**: PascalCase for methods/classes, camelCase for local variables.
- **Async/Await**: Always use `async Task` for I/O bound operations.
- **DI**: Use Constructor Injection for dependencies.
- **Validation**: Use FluentValidation (if integrated) or simple checks in Service layer.
- **Error Handling**: Throw specific exceptions (`InvalidOperationException`) in Service layer; Controller catches or Middleware handles them.

## 🧪 Testing

We use **xUnit** and **Moq**.

#### How to add a new test:
1. Go to `SMARTCAMPUS.Tests`.
2. Create a new file `[Name]Tests.cs`.
3. Use `Mock<IService>` to isolate the unit under test.

```csharp
[Fact]
public async Task Should_Return_True_When_Valid()
{
    // Arrange
    var mock = new Mock<IDependency>();
    var service = new MyService(mock.Object);
    
    // Act
    var result = await service.DoWork();
    
    // Assert
    Assert.True(result);
}
```

## 🔄 Contribution Workflow
1. Create a `feature/branch-name`.
2. Commit changes.
3. Run tests locally (`dotnet test`).
4. Open a Pull Request.
