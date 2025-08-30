# Azure App Service: Hosting Backend APIs

## What is Azure App Service?

Azure App Service is a fully managed platform for building, deploying, and scaling web apps. It supports multiple programming languages (.NET, Java, Node.js, Python, etc.) and allows you to host:
- Web Applications
- RESTful APIs
- Mobile Backends
- WebJobs

For backend APIs (like ASP.NET Core), App Service provides:
- Automatic scaling
- High availability
- Security compliance
- Deployment slots
- Continuous deployment integration

## App Service Plan: The Hosting Foundation

An App Service Plan defines the compute resources for your apps:
- **Pricing tier**: Free, Shared, Basic, Standard, Premium, Isolated
- **Instance size**: Small, Medium, Large, etc.
- **Scale-out capabilities**: Number of instances
- **Region**: Geographic location

Your App Service runs within an App Service Plan, which determines performance characteristics and cost.

## Hosting an ASP.NET Core API in App Service

### Accessing Your API
Once deployed, your API will be accessible at:
- `https://{your-app-name}.azurewebsites.net/api/{endpoint}`
- Or via a custom domain if configured

### Environment Configuration

#### AppSettings in ASP.NET Core
ASP.NET Core uses a hierarchical configuration system:

1. **appsettings.json** - Base configuration
2. **appsettings.{Environment}.json** - Environment-specific settings
3. **Environment variables** - Highest priority, override all files

**Example appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyDatabase;Trusted_Connection=true;"
  },
  "ApiSettings": {
    "ApiUrl": "https://localhost:7000",
    "Timeout": 30
  }
}
```

**Example appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyDatabase;Trusted_Connection=true;"
  },
  "ApiSettings": {
    "ApiUrl": "https://localhost:7000",
    "Timeout": 60
  }
}
```

#### Setting Environment in Azure Portal

1. Navigate to your App Service in Azure Portal
2. Go to **Configuration** → **Application settings**
3. Add a new setting:
   - **Name**: `ASPNETCORE_ENVIRONMENT`
   - **Value**: `Production`, `Staging`, or your custom environment
   - Click **OK** and **Save**

This environment variable determines which appsettings file is loaded:
- `Development` → appsettings.Development.json
- `Production` → appsettings.Production.json (if exists) or appsettings.json

#### Setting Other Environment Variables

In the same **Application settings** section, you can add any configuration values that will override those in appsettings.json:

- **Name**: `ApiSettings:Timeout`
- **Value**: `90`

This would override the Timeout value regardless of what's in your configuration files.

## Terraform Example: App Service Plan and App Service

```hcl
# Configure the Azure provider
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

# Create a resource group
resource "azurerm_resource_group" "example" {
  name     = "my-api-resources"
  location = "West Europe"
}

# Create App Service Plan
resource "azurerm_service_plan" "example" {
  name                = "my-api-appservice-plan"
  resource_group_name = azurerm_resource_group.example.name
  location            = azurerm_resource_group.example.location
  os_type             = "Windows"
  sku_name            = "B1" # Basic tier, 1 instance
}

# Create App Service
resource "azurerm_windows_web_app" "example" {
  name                = "my-dotnet-api-app"
  resource_group_name = azurerm_resource_group.example.name
  location            = azurerm_service_plan.example.location
  service_plan_id     = azurerm_service_plan.example.id

  site_config {
    application_stack {
      current_stack  = "dotnet"
      dotnet_version = "v8.0"
    }
    always_on = false # Set to true for production
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "ApiSettings:Timeout"    = "45"
  }
}

```

## Deploying Your API Using Publish Profile

### Steps to Deploy:

1. **Build your ASP.NET Core API**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Download Publish Profile**
   - Go to your App Service in Azure Portal
   - Click **Get publish profile**
   - Save the `.PublishSettings` file locally

3. **Deploy using Visual Studio**
   - Right-click your project → **Publish**
   - Select **Import Profile** and choose your downloaded file
   - Click **Publish**

4. **Deploy using Command Line (Zip Deploy)**
   - Install Azure CLI: `az login`
   - Zip your publish folder: `zip -r site.zip ./publish/*`
   - Deploy:
     ```bash
     az webapp deployment source config-zip \
       --resource-group my-api-resources \
       --name my-dotnet-api-app \
       --src site.zip
     ```

5. **Verify Deployment**
   - Visit `https://my-dotnet-api-app.azurewebsites.net/swagger` (if Swagger is configured)
   - Or test an API endpoint: `https://my-dotnet-api-app.azurewebsites.net/api/values`

### Troubleshooting
- Check logs in Azure Portal: App Service → **App Service logs**
- Enable detailed error messages:
  - In Application Settings, add:
    - `ASPNETCORE_DETAILEDERRORS = true`
- Use Kudu console for advanced troubleshooting:
  - `https://my-dotnet-api-app.scm.azurewebsites.net`

## Best Practices
1. Use deployment slots for staging before production
2. Configure custom domains for production apps
3. Set up monitoring and alerts
4. Implement CI/CD pipelines for automated deployments
5. Use Key Vault for sensitive configuration values
