# Custom Configuration in ASP.NET Core API

This guide explains how to add custom configuration sections to an ASP.NET Core API project using the traditional `Startup.cs` approach, and how to properly inject, override, and debug these configurations.

## Table of Contents
1. [Understanding Configuration in ASP.NET Core](#understanding-configuration)
2. [Creating Custom Configuration Sections](#creating-custom-configurations)
3. [Configuration Binding and Validation](#configuration-binding)
4. [Dependency Injection](#dependency-injection)
5. [Configuration Overrides](#configuration-overrides)
6. [Debugging Configuration](#debugging-configuration)
7. [Best Practices](#best-practices)

## Understanding Configuration in ASP.NET Core <a name="understanding-configuration"></a>

ASP.NET Core uses a key-value pair based configuration system that can read from multiple sources:
- `appsettings.json` files
- Environment variables
- Command-line arguments
- User secrets (for development)
- Azure Key Vault
- Custom providers

Configuration sources are loaded in a specific order, with later sources overriding earlier ones.

## Creating Custom Configuration Sections <a name="creating-custom-configurations"></a>

### 1. Define Configuration Classes

Create strongly-typed configuration classes:

```csharp
// Models/Configurations/DatabaseSettings.cs
public class DatabaseSettings
{
    public string ConnectionString { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableLogging { get; set; }
}

// Models/Configurations/JwtSettings.cs
public class JwtSettings
{
    public string Secret { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int ExpirationMinutes { get; set; } = 60;
}

// Models/Configurations/ExternalApiSettings.cs
public class ExternalApiSettings
{
    public string BaseUrl { get; set; }
    public string ApiKey { get; set; }
    public int RetryCount { get; set; } = 3;
}
```

### 2. Add Configuration to appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",
  
  // Custom configuration sections
  "DatabaseSettings": {
    "ConnectionString": "Server=localhost;Database=MyAppDb;Trusted_Connection=true;",
    "TimeoutSeconds": 45,
    "EnableLogging": true
  },
  
  "JwtSettings": {
    "Secret": "your-super-secret-key-here",
    "Issuer": "your-app",
    "Audience": "your-app-users",
    "ExpirationMinutes": 120
  },
  
  "ExternalApiSettings": {
    "BaseUrl": "https://api.example.com",
    "ApiKey": "your-api-key",
    "RetryCount": 5
  }
}
```

## Configuration Binding and Validation <a name="configuration-binding"></a>

### Configure Services in Startup.cs

```csharp
// Startup.cs
public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Bind configuration sections to strongly-typed classes
        services.Configure<DatabaseSettings>(Configuration.GetSection("DatabaseSettings"));
        services.Configure<JwtSettings>(Configuration.GetSection("JwtSettings"));
        services.Configure<ExternalApiSettings>(Configuration.GetSection("ExternalApiSettings"));
        
        // Optional: Add validation
        services.Configure<DatabaseSettings>(settings =>
        {
            if (string.IsNullOrEmpty(settings.ConnectionString))
                throw new ArgumentNullException(nameof(settings.ConnectionString));
        });

        services.AddControllers();
        // Add other services...
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Configure pipeline...
    }
}
```

## Dependency Injection <a name="dependency-injection"></a>

### What Dependency Injection Means

Dependency Injection (DI) is a design pattern that implements Inversion of Control (IoC) between classes and their dependencies. In ASP.NET Core, the built-in DI container:
- Manages object creation and lifetime
- Promotes loose coupling
- Makes testing easier
- Provides configuration through constructor parameters

### Injecting Configuration

#### Option 1: IOptions<T> Pattern (Recommended)

```csharp
// Services/DatabaseService.cs
public class DatabaseService
{
    private readonly DatabaseSettings _databaseSettings;

    public DatabaseService(IOptions<DatabaseSettings> databaseOptions)
    {
        _databaseSettings = databaseOptions.Value;
        
        // Use the configuration
        var connectionString = _databaseSettings.ConnectionString;
        var timeout = _databaseSettings.TimeoutSeconds;
    }
}

// Controllers/UsersController.cs
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly JwtSettings _jwtSettings;
    private readonly ExternalApiSettings _apiSettings;

    public UsersController(
        IOptions<JwtSettings> jwtOptions,
        IOptions<ExternalApiSettings> apiOptions)
    {
        _jwtSettings = jwtOptions.Value;
        _apiSettings = apiOptions.Value;
    }

    [HttpGet]
    public IActionResult GetUsers()
    {
        // Use configured values
        var tokenExpiration = _jwtSettings.ExpirationMinutes;
        var apiUrl = _apiSettings.BaseUrl;
        
        return Ok(new { message = "Configuration injected successfully" });
    }
}
```

#### Option 2: Direct IConfiguration Injection (Less Recommended)

```csharp
public class SomeService
{
    private readonly IConfiguration _configuration;

    public SomeService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // Access specific values
        var connectionString = _configuration["DatabaseSettings:ConnectionString"];
        var timeout = _configuration.GetValue<int>("DatabaseSettings:TimeoutSeconds");
    }
}
```

## Configuration Overrides <a name="configuration-overrides"></a>

### 1. Environment-specific appsettings

Create `appsettings.Development.json`:

```json
{
  "DatabaseSettings": {
    "ConnectionString": "Server=localhost;Database=MyAppDb_Dev;Trusted_Connection=true;",
    "EnableLogging": true
  },
  "ExternalApiSettings": {
    "BaseUrl": "https://api-dev.example.com"
  }
}
```

### 2. User Secrets (Development Only)

```bash
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:Secret" "super-secret-dev-key"
dotnet user-secrets set "ExternalApiSettings:ApiKey" "dev-api-key-123"
```

### 3. Environment Variables

```bash
# Set environment variables (Linux/macOS)
export DatabaseSettings__ConnectionString="Server=prod-db;Database=MyAppDb;User=prod-user;Password=prod-password;"
export JwtSettings__Secret="super-secret-prod-key"

# Windows Command Prompt
set DatabaseSettings__ConnectionString=Server=prod-db;Database=MyAppDb;User=prod-user;Password=prod-password;
set JwtSettings__Secret=super-secret-prod-key

# Windows PowerShell
$env:DatabaseSettings__ConnectionString = "Server=prod-db;Database=MyAppDb;User=prod-user;Password=prod-password;"
$env:JwtSettings__Secret = "super-secret-prod-key"
```

### 4. Azure App Service Configuration

In Azure Portal, go to your App Service → Configuration → Application settings:

| Name | Value |
|------|-------|
| DatabaseSettings:ConnectionString | Server=azure-db;Database=MyAppDb;... |
| JwtSettings:Secret | azure-super-secret-key |
| ExternalApiSettings:ApiKey | azure-api-key |

## Debugging Configuration <a name="debugging-configuration"></a>

### 1. Logging Configuration Values

```csharp
// Startup.cs
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        
        // Log configuration for debugging
        var databaseSettings = Configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>();
        var jwtSettings = Configuration.GetSection("JwtSettings").Get<JwtSettings>();
        
        var logger = app.ApplicationServices.GetService<ILogger<Startup>>();
        logger.LogInformation("Database Connection: {Connection}", databaseSettings.ConnectionString);
        logger.LogInformation("JWT Expiration: {Minutes} minutes", jwtSettings.ExpirationMinutes);
    }
    
    // Rest of configuration...
}
```

### 2. Configuration Debug Controller

```csharp
// Controllers/ConfigDebugController.cs
[ApiController]
[Route("api/debug/config")]
public class ConfigDebugController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly DatabaseSettings _dbSettings;
    private readonly JwtSettings _jwtSettings;

    public ConfigDebugController(
        IConfiguration configuration,
        IOptions<DatabaseSettings> dbOptions,
        IOptions<JwtSettings> jwtOptions)
    {
        _configuration = configuration;
        _dbSettings = dbOptions.Value;
        _jwtSettings = jwtOptions.Value;
    }

    [HttpGet]
    public IActionResult GetConfiguration()
    {
        var configData = new
        {
            // Raw configuration values
            RawConnectionString = _configuration["DatabaseSettings:ConnectionString"],
            RawJwtSecret = _configuration["JwtSettings:Secret"],
            
            // Strongly-typed values
            DatabaseSettings = _dbSettings,
            JwtSettings = new
            {
                _jwtSettings.Secret,
                _jwtSettings.Issuer,
                _jwtSettings.Audience,
                _jwtSettings.ExpirationMinutes
            },
            
            // Environment information
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            AllDatabaseSettings = _configuration.GetSection("DatabaseSettings").GetChildren()
                .ToDictionary(x => x.Key, x => x.Value)
        };

        return Ok(configData);
    }
}
```

### 3. Using ILogger for Configuration Debugging

```csharp
public class Startup
{
    private readonly ILogger<Startup> _logger;

    public Startup(IConfiguration configuration, ILogger<Startup> logger)
    {
        Configuration = configuration;
        _logger = logger;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Log configuration values during startup
        var dbSettings = Configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>();
        _logger.LogInformation("Database Configuration: {@DatabaseSettings}", dbSettings);
        
        services.Configure<DatabaseSettings>(Configuration.GetSection("DatabaseSettings"));
        // ...
    }
}
```

## Best Practices <a name="best-practices"></a>

1. **Use strongly-typed configurations** instead of magic strings with `IConfiguration`
2. **Validate configuration values** during application startup
3. **Use environment-specific configuration files** for different environments
4. **Never store secrets** in version control - use user secrets or environment variables
5. **Use `IOptions<T>` pattern** for dependency injection
6. **Consider using `IOptionsSnapshot<T>`** for reloadable configuration
7. **Use descriptive section names** that match your configuration classes
8. **Provide default values** in your configuration classes when appropriate

```
