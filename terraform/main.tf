# Configure the Azure provider
provider "azurerm" {
  features {}
  subscription_id = "8b6001cd-a329-4999-8dbb-3b3261bb100a"
}

# Application Insights
resource "azurerm_application_insights" "api" {
  name                = "api-${var.environment}-${lower(replace(var.location, " ", ""))}"
  location            = var.location
  resource_group_name = var.resource_group_name
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.logspace.id

  depends_on = [azurerm_log_analytics_workspace.logspace]
}

# Log Analytics Workspace (required for Application Insights)
resource "azurerm_log_analytics_workspace" "logspace" {
  name                = "log-${var.environment}-${lower(replace(var.location, " ", ""))}"
  location            = var.location
  resource_group_name = var.resource_group_name 
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

# Key Vault
resource "azurerm_key_vault" "kv" {
  name                        = "kv-${lower(replace(var.location, " ", ""))}-${var.environment}"
  location                    = var.location
  resource_group_name         = var.resource_group_name 
  enabled_for_disk_encryption = true
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days  = 7
  purge_protection_enabled    = false
  sku_name                    = "standard"

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = [
      "Get", "List", "Set", "Delete", "Recover", "Backup", "Restore", "Purge"
    ]
  }
}

# App Service Plan
resource "azurerm_service_plan" "plan" {
  name                = "plan-${lower(replace(var.location, " ", ""))}-win-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name 
  os_type             = "Windows"
  sku_name            = "S1"
}

# First App Service
resource "azurerm_windows_web_app" "webapp" {
  name                = "app-${lower(replace(var.location, " ", ""))}-win-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name 
  service_plan_id     = azurerm_service_plan.plan.id

  site_config {
    application_stack {
      current_stack  = "dotnet"
      dotnet_version = "v8.0"
    }
  }

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.api.instrumentation_key
  }
}

# Second App Service (cred-serv1)
resource "azurerm_windows_web_app" "cred_serv1" {
  name                = "cred-serv1-${lower(replace(var.location, " ", ""))}-win-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name 
  service_plan_id     = azurerm_service_plan.plan.id

  site_config {
    application_stack {
      current_stack  = "dotnet"
      dotnet_version = "v8.0"
    }
  }

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.api.instrumentation_key
  }
}

# SQL Server
resource "azurerm_mssql_server" "dbserv" {
  name                         = "db-${lower(replace(var.location, " ", ""))}2-${var.environment}"
  location                     = var.location
  resource_group_name          = var.resource_group_name 
  version                      = "12.0"
  administrator_login          = "sqladmin"
  administrator_login_password = "P@ssw0rd!1234" # In production, use Azure Key Vault for secrets
  minimum_tls_version          = "1.2"

  azuread_administrator {
    login_username = "AzureAD Admin"
    object_id      = data.azurerm_client_config.current.object_id
  }
}

# Allow Azure services to access SQL Server
resource "azurerm_mssql_firewall_rule" "allow_azure" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.dbserv.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# SQL Database
resource "azurerm_mssql_database" "creditcard" {
  name         = "CreditCard"
  server_id    = azurerm_mssql_server.dbserv.id
  collation    = "SQL_Latin1_General_CP1_CI_AS"
  license_type = "LicenseIncluded"
  sku_name     = "GP_S_Gen5_1"
  max_size_gb  = 2
}

# Data source for current Azure client config
data "azurerm_client_config" "current" {}