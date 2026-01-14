# Getting Started with Blazor Applications

## What is Blazor?

Blazor is a web framework from Microsoft that allows you to build interactive web applications using C# instead of JavaScript. It's part of the ASP.NET Core ecosystem and offers two main hosting models:
- **Blazor Server**: Runs on the server with real-time communication via SignalR
- **Blazor WebAssembly**: Runs client-side in the browser using WebAssembly

## Prerequisites

Before you start, make sure you have:

1. **.NET SDK** (version 6.0 or later)
   - Download from: https://dotnet.microsoft.com/download
   - Verify installation: `dotnet --version`

2. **Code Editor** (choose one):
   - Visual Studio 2022 (recommended for full IDE experience)
   - Visual Studio Code with C# extension
   - JetBrains Rider

## Creating Your First Blazor Application

### Option 1: Using .NET CLI (Command Line)

Open a terminal/command prompt and run:

```bash
# Create a new Blazor Server application
dotnet new blazorserver -n MyBlazorApp

# Or create a Blazor WebAssembly application
dotnet new blazorwasm -n MyBlazorApp

# Navigate to the project directory
cd MyBlazorApp

# Run the application
dotnet run
```

### Option 2: Using Visual Studio

1. Open Visual Studio 2022
2. Click "Create a new project"
3. Search for "Blazor"
4. Choose "Blazor Server App" or "Blazor WebAssembly App"
5. Configure your project name and location
6. Click "Create"

## Understanding the Project Structure

When you create a new Blazor application, you'll see this structure:

```
MyBlazorApp/
├── Components/           # Reusable UI components
│   ├── Layout/          # Layout components (navigation, etc.)
│   └── Pages/           # Page components (routable views)
├── wwwroot/             # Static files (CSS, JS, images)
├── Program.cs           # Application entry point
├── appsettings.json     # Configuration settings
└── MyBlazorApp.csproj   # Project file
```

### Key Files Explained:

- **Program.cs**: Configures services and the request pipeline
- **Components/App.razor**: Root component of your application
- **Components/Routes.razor**: Defines routing configuration
- **Components/Layout/MainLayout.razor**: Main layout template
- **Components/Pages/**: Contains your page components (Home.razor, Counter.razor, etc.)

## Running Your Application

### From Command Line:
```bash
dotnet run
```

### From Visual Studio:
Press `F5` or click the "Run" button

Your application will start, typically at `https://localhost:5001` or `http://localhost:5000`.

## Basic Blazor Concepts

### 1. Components
Blazor applications are built using components - reusable pieces of UI. Components are defined in `.razor` files:

```razor
@page "/hello"

<h3>Hello, @Name!</h3>

@code {
    [Parameter]
    public string Name { get; set; } = "World";
}
```

### 2. Routing
Use the `@page` directive to make a component routable:

```razor
@page "/products"
@page "/products/{id:int}"

<h3>Product Details</h3>
<p>Product ID: @Id</p>

@code {
    [Parameter]
    public int Id { get; set; }
}
```

### 3. Data Binding
Blazor supports various binding scenarios:

```razor
<input @bind="currentValue" />
<button @onclick="HandleClick">Click me</button>

@code {
    private string currentValue = "";
    
    private void HandleClick()
    {
        // Handle button click
    }
}
```

### 4. Dependency Injection
Register services in `Program.cs`:

```csharp
builder.Services.AddScoped<IMyService, MyService>();
```

Inject in components:

```razor
@inject IMyService MyService

@code {
    protected override async Task OnInitializedAsync()
    {
        var data = await MyService.GetDataAsync();
    }
}
```

## Common Patterns

### 1. Creating a Simple Counter Component

```razor
@page "/counter"

<h1>Counter</h1>
<p>Current count: @currentCount</p>
<button @onclick="IncrementCount">Click me</button>

@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
```

### 2. Working with Forms

```razor
@using System.ComponentModel.DataAnnotations

<EditForm Model="@person" OnValidSubmit="@HandleValidSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />
    
    <div>
        <label>Name:</label>
        <InputText @bind-Value="person.Name" />
        <ValidationMessage For="@(() => person.Name)" />
    </div>
    
    <button type="submit">Submit</button>
</EditForm>

@code {
    private Person person = new();

    private void HandleValidSubmit()
    {
        // Process valid form submission
    }

    public class Person
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = "";
    }
}
```

## Next Steps

Once you're comfortable with the basics:

1. **Learn about Blazor lifecycle methods** (OnInitialized, OnParametersSet, etc.)
2. **Explore component communication** (parameters, cascading values, event callbacks)
3. **Add authentication and authorization**
4. **Work with APIs and HTTP requests**
5. **Style your application** (CSS, Bootstrap, component libraries)
6. **Deploy your application** (Azure, IIS, Docker)

## Useful Resources

- [Official Blazor Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [Blazor University](https://blazor-university.com/)
- [Awesome Blazor](https://github.com/AdrienTorris/awesome-blazor) - Community resources
- [Blazor Samples](https://github.com/dotnet/blazor-samples)

## Tips for Success

1. **Start simple** - Begin with basic components and gradually add complexity
2. **Use the browser dev tools** - Blazor works great with F12 developer tools
3. **Leverage existing .NET knowledge** - Most C# patterns work in Blazor
4. **Join the community** - Stack Overflow, Discord, and GitHub discussions are very helpful
5. **Practice regularly** - Build small projects to reinforce concepts

Happy coding with Blazor! 🚀

---

*This guide covers the fundamentals. As you progress, you'll discover Blazor's power in building modern, interactive web applications with the language and tools you already know.*