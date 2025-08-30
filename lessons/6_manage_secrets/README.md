## 🌐 Overview

This setup provisions an **Azure-hosted ASP.NET Core application** that securely retrieves secrets from **Azure Key Vault** at runtime. The flow is:

1. **Infrastructure**

   * A **Resource Group** contains all resources.
   * An **App Service Plan (Windows S1)** provides the compute environment.
   * An **App Service (Web App)** runs the ASP.NET Core application.

2. **Identity & Security**

   * The Web App is assigned a **System-Assigned Managed Identity**, eliminating the need for credentials in code or config.
   * An **Azure Key Vault** securely stores sensitive configuration values.
   * A **Key Vault Access Policy** grants the Web App’s identity **read-only** access (`Get`, `List`) to the secrets.

3. **Secret Injection**

   * Secrets (e.g., connection strings) are stored in Key Vault.
   * App Service Application Settings use **Key Vault references** to fetch secrets at runtime.
   * These values are automatically exposed as **environment variables** inside the Web App.

4. **Application Configuration**

   * The ASP.NET Core application reads configuration in this order:

     * Defaults in `appsettings.json`.
     * Environment variables injected from Key Vault, which **override defaults**.
   * This ensures secrets are **never hardcoded** or exposed in plain text.

**Result:**

* A secure, least-privilege architecture where secrets live only in Key Vault and are dynamically injected into the app at runtime.
* Developers and Terraform code never handle secrets directly.

---

## 1. Create an App Service Plan (Windows S1)

An **App Service Plan** defines the underlying compute resources that host your web apps. It specifies:

* **Operating system** (Windows or Linux)
* **Pricing tier** (S1 is a Standard tier, providing scaling and SLA guarantees)

We need this plan before creating the App Service itself because the App Service runs inside it.

**Terraform code:**

```hcl
resource "azurerm_app_service_plan" "example_plan" {
  name                = "example-appserviceplan"
  location            = azurerm_resource_group.example.location
  resource_group_name = azurerm_resource_group.example.name
  kind                = "Windows"

  sku {
    tier = "Standard"
    size = "S1"
  }
}
```

---

## 2. Create an App Service (Web App)

An **App Service** is the actual web application environment that runs your ASP.NET Core application.

* It runs inside the App Service Plan you just created.
* This is where you will deploy your ASP.NET Core app.

**Terraform code:**

```hcl
resource "azurerm_app_service" "example_app" {
  name                = "example-webapp"
  location            = azurerm_resource_group.example.location
  resource_group_name = azurerm_resource_group.example.name
  app_service_plan_id = azurerm_app_service_plan.example_plan.id
}
```

---

## 3. Create a Key Vault

Azure **Key Vault** securely stores secrets, keys, and certificates.

* Purpose: Instead of storing secrets (like connection strings or API keys) in plain text (e.g., in `appsettings.json`), we store them in Key Vault.
* This ensures secrets are **encrypted at rest**, **restricted by access policies**, and **not exposed** in code or Terraform state.

**Terraform code:**

```hcl
resource "azurerm_key_vault" "example_kv" {
  name                        = "example-keyvault123"
  location                    = azurerm_resource_group.example.location
  resource_group_name         = azurerm_resource_group.example.name
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  sku_name                    = "standard"
  purge_protection_enabled    = true
  soft_delete_retention_days  = 7
}
```

---

## 4. Give App Service Access to Key Vault

To allow the **App Service** to read secrets from the Key Vault:

* We must enable **Managed Identity** for the App Service. This avoids using credentials in plain text.
* Then, we grant that identity an **access policy** in Key Vault with only the minimum required permissions (least privilege principle).

**Terraform code:**

```hcl
# Enable Managed Identity for App Service
resource "azurerm_app_service" "example_app" {
  name                = "example-webapp"
  location            = azurerm_resource_group.example.location
  resource_group_name = azurerm_resource_group.example.name
  app_service_plan_id = azurerm_app_service_plan.example_plan.id

  identity {
    type = "SystemAssigned"
  }
}

# Allow App Service to get secrets from Key Vault
resource "azurerm_key_vault_access_policy" "example_policy" {
  key_vault_id = azurerm_key_vault.example_kv.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_app_service.example_app.identity.principal_id

  secret_permissions = ["Get", "List"]
}
```

* **SystemAssigned Identity**: Azure automatically manages a service principal for your App Service.
* **Access Policy**: Grants only `Get` and `List` permissions for secrets. No write/delete, because the app should only *read* secrets, not manage them.

---

## 5. Inject Key Vault Secrets into App Service

Azure allows mapping Key Vault secrets directly into **App Service Application Settings**.

* These settings are automatically exposed as environment variables in the app runtime.
* ASP.NET Core can then read them securely without hardcoding values.

**Terraform code example:**

```hcl
resource "azurerm_app_service" "example_app" {
  # (rest of config same as above)

  app_settings = {
    "KeyVault__MySecret" = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.example_secret.id})"
  }
}
```

Here:

* `@Microsoft.KeyVault(...)` tells Azure App Service to fetch the secret from Key Vault at runtime.
* The secret is injected as an environment variable (`KeyVault__MySecret`).

---

## 6. Add Secrets to Key Vault

Example of storing a database connection string in Key Vault:

**Terraform code:**

```hcl
resource "azurerm_key_vault_secret" "example_secret" {
  name         = "DbConnectionString"
  value        = "Server=myserver.database.windows.net;Database=mydb;User Id=dbuser;Password=SuperSecretPassword;"
  key_vault_id = azurerm_key_vault.example_kv.id
}
```

---

## 7. Use Key Vault Secrets in ASP.NET Core

In your **ASP.NET Core application**:

* By default, `appsettings.json` provides config values.
* We want to override these values with environment variables injected by Azure from Key Vault.
* ASP.NET Core supports hierarchical config using `__` as separators.

**Example:**

`appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "placeholder-value"
  }
}
```

App will override this if environment variable `ConnectionStrings__DefaultConnection` is set (from Key Vault).

**Program.cs modification:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Load environment variables (injected from Azure)
builder.Configuration.AddEnvironmentVariables();

var app = builder.Build();
```

* This ensures any values injected from Key Vault will overwrite `appsettings.json`.
* No secrets are checked into source control.
* Only the App Service identity has access (least privilege).

---

## 8. Applying Least Privilege Principle

* **Terraform State**: Never put secrets directly in Terraform variables or state. Use Key Vault.
* **Key Vault Access Policy**: Only grant `Get` and `List` permissions to the App Service identity. No write/delete.
* **Secrets in Code**: Never hardcode secrets in `appsettings.json` or code. Use environment variable injection.
* **Separation of Duties**: Developers should not see production secrets. Only the app reads them at runtime.

