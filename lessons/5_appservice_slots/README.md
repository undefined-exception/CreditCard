# Azure App Service: Hosting Backend APIs with Zero Downtime Deployments

## What is Azure App Service?

Azure App Service is a fully managed platform for building, deploying, and scaling web apps and APIs. It supports multiple programming languages (.NET, Java, Node.js, Python, etc.) and provides built-in infrastructure maintenance, security patching, and scaling capabilities.

For backend APIs (like ASP.NET Core C# APIs), App Service provides:
- HTTP/HTTPS endpoints
- Built-in load balancing
- Automatic scaling
- Deployment slots for staging
- Integration with Azure DevOps and GitHub Actions

## App Service Plan: The Hosting Container

An **App Service Plan** defines the compute resources (CPU, memory, storage) that your app runs on. It determines:
- **Pricing tier** (Free, Shared, Basic, Standard, Premium, Isolated)
- **Instance size** (number of cores and amount of memory)
- **Scaling capabilities** (manual vs. automatic scaling)
- **Region** where your app is hosted

Your API runs within an App Service Plan, which can host multiple apps sharing the same resources.

## Hosting an ASP.NET Core Backend API

### Accessing Your API
Once deployed, your API is accessible via:
- **Default URL**: `https://{your-app-name}.azurewebsites.net`
- **Custom domains**: Configure your own domain through Azure Portal
- **API endpoints**: `https://{your-app-name}.azurewebsites.net/api/endpoint`

### Environment Variables Configuration

In Azure App Service, you can set environment variables that override your `appsettings.json` values:

#### Azure Portal Configuration:
1. Navigate to your App Service → Configuration → Application settings
2. Add new settings under "Application settings"

#### Example Configuration:
```json
// In your appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "LocalDevelopmentConnectionString"
  },
  "ApiSettings": {
    "ApiKey": "local-key",
    "BaseUrl": "https://localhost:5001"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

#### Azure Environment Variables:
Set these in Azure App Service Configuration:
- `ConnectionStrings:DefaultConnection` = `Server=prod-db;Database=myapp;User Id=user;Password=pass;`
- `ApiSettings:ApiKey` = `production-api-key-12345`
- `ApiSettings:BaseUrl` = `https://api.myapp.com`
- `ASPNETCORE_ENVIRONMENT` = `Production`

#### Accessing in Code:
```csharp
public class MyService
{
    private readonly IConfiguration _configuration;
    
    public MyService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection");
    }
    
    public string GetApiKey()
    {
        return _configuration["ApiSettings:ApiKey"];
    }
}
```

## Deployment Slots for Zero Downtime Deployments

### What are Deployment Slots?

Deployment slots are live apps with their own hostnames and configurations. They enable you to:
- Test new versions before making them production
- Perform zero-downtime deployments
- Roll back quickly if issues arise

### Typical Slot Setup:
- **Production slot**: Live version serving real traffic
- **Staging slot**: Where you deploy and test new versions

### Zero Downtime Deployment Process:

1. **Deploy to Staging**: Push your new code to the staging slot
   ```bash
   az webapp deployment source config-zip \
     --src ./publish.zip \
     --name my-app \
     --slot staging \
     --resource-group my-resource-group
   ```

2. **Test in Staging**: Verify the new version works correctly
   - Access via: `https://{your-app-name}-staging.azurewebsites.net`
   - Test all endpoints and functionality

3. **Swap Slots**: When ready, swap staging with production
   ```bash
   az webapp deployment slot swap \
     --name my-app \
     --resource-group my-resource-group \
     --slot staging \
     --target-slot production
   ```

4. **The Swap Process**:
   - Staging slot is warmed up (if auto-swap is configured)
   - Swap occurs instantly by switching hostnames
   - Old production becomes new staging
   - No downtime for users

### Version Endpoint for Confirmation

Create a version endpoint to verify which code version is running:

```csharp
// VersionController.cs
[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    [HttpGet]
    public IActionResult GetVersion()
    {
        var versionInfo = new
        {
            Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(),
            BuildDate = GetBuildDate(),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            SlotName = Environment.GetEnvironmentVariable("WEBSITE_SLOT_NAME") ?? "Production"
        };
        
        return Ok(versionInfo);
    }
    
    private DateTime GetBuildDate()
    {
        var assembly = Assembly.GetEntryAssembly();
        var filePath = assembly?.Location;
        return filePath != null ? File.GetLastWriteTime(filePath) : DateTime.MinValue;
    }
}
```

**Usage:**
- Production: `GET https://my-app.azurewebsites.net/api/version`
- Staging: `GET https://my-app-staging.azurewebsites.net/api/version`

### Ensuring Successful Slot Swaps

1. **Validate App Settings**: Ensure both slots have appropriate environment-specific settings
2. **Connection Strings**: Verify database connections work in both environments
3. **Warm-up**: Enable application initialization to pre-load the app
   ```xml
   <!-- In your .csproj -->
   <PropertyGroup>
     <AspNetCoreHostingModel>OutOfProcess</AspNetCoreHostingModel>
     <AspNetCoreModuleName>AspNetCoreModuleV2</AspNetCoreModuleName>
   </PropertyGroup>
   ```

4. **Health Checks**: Implement health check endpoints
   ```csharp
   services.AddHealthChecks()
           .AddSqlServer(Configuration.GetConnectionString("DefaultConnection"));
   ```

5. **Test Before Swap**: Always test thoroughly in the staging slot
6. **Monitor After Swap**: Use Azure Monitor to watch for errors post-swap

### Auto-Swap Configuration

For automated deployments, configure auto-swap:
```bash
az webapp deployment slot auto-swap \
  --name my-app \
  --resource-group my-resource-group \
  --slot staging \
  --auto-swap-slot production
```

## Best Practices

1. **Slot-Specific Settings**: Mark settings as "slot setting" in Azure Portal to prevent them from swapping
2. **Database Migrations**: Run migrations before swapping, not during deployment
3. **Monitoring**: Set up Application Insights for both slots
4. **Rollback Plan**: Keep previous deployment artifacts for quick rollback
5. **Traffic Routing**: Use deployment slots with testing in production for canary deployments

By leveraging Azure App Service and deployment slots, you can achieve reliable, zero-downtime deployments for your backend APIs while maintaining full control over the release process.