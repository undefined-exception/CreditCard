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

# Virtual Network
resource "azurerm_virtual_network" "vnet" {
  name                = "vnet-${var.environment}-${lower(replace(var.location, " ", ""))}"
  address_space       = ["10.0.0.0/16"]
  location            = var.location
  resource_group_name = var.resource_group_name
}

# Subnet for App Services
resource "azurerm_subnet" "appservice" {
  name                 = "snet-appservice-${var.environment}"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = ["10.0.1.0/24"]

  delegation {
    name = "appservice-delegation"

    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
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
}

data "azurerm_client_config" "current" {}

# Key Vault access policy for current user
resource "azurerm_key_vault_access_policy" "current_user" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  secret_permissions = [
    "Get", "List", "Set", "Delete", "Recover", "Backup", "Restore", "Purge"
  ]
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

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_stack {
      current_stack  = "dotnet"
      dotnet_version = "v8.0"
    }
  }

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.api.instrumentation_key
    "SQLConnectionString"            = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.sql_connection_string.id})"
    "CreditBureauApiConfig__BaseEndpoint"                   = "https://${azurerm_windows_web_app.cred_serv1.default_hostname}"
  }
}

# Key Vault access policy for first app service
resource "azurerm_key_vault_access_policy" "webapp" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_windows_web_app.webapp.identity[0].principal_id

  secret_permissions = [
    "Get", "List"
  ]
}

# Second App Service (cred-serv1)
resource "azurerm_windows_web_app" "cred_serv1" {
  name                = "cred-serv1-${lower(replace(var.location, " ", ""))}-win-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name 
  service_plan_id     = azurerm_service_plan.plan.id

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_stack {
      current_stack  = "dotnet"
      dotnet_version = "v8.0"
    }
  }

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.api.instrumentation_key
    "SQLConnectionString"            = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.sql_connection_string.id})"
  }
}

# Key Vault access policy for second app service
resource "azurerm_key_vault_access_policy" "cred_serv1" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_windows_web_app.cred_serv1.identity[0].principal_id

  secret_permissions = [
    "Get", "List"
  ]
}

# VNet integration for App Services
resource "azurerm_app_service_virtual_network_swift_connection" "webapp" {
  app_service_id = azurerm_windows_web_app.webapp.id
  subnet_id      = azurerm_subnet.appservice.id
}

resource "azurerm_app_service_virtual_network_swift_connection" "cred_serv1" {
  app_service_id = azurerm_windows_web_app.cred_serv1.id
  subnet_id      = azurerm_subnet.appservice.id
}

# SQL Server (with only SQL authentication)
resource "azurerm_mssql_server" "dbserv" {
  name                         = "db-${lower(replace(var.location, " ", ""))}2-${var.environment}"
  location                     = var.location
  resource_group_name          = var.resource_group_name 
  version                      = "12.0"
  administrator_login          = "sqladmin"
  administrator_login_password = "P@ssw0rd!1234" # In production, consider using a more secure method
  minimum_tls_version          = "1.2"
}

# SQL Connection String
resource "azurerm_key_vault_secret" "sql_connection_string" {
  name         = "sql-connection-string"
  value        = "Server=tcp:${azurerm_mssql_server.dbserv.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.creditcard.name};Persist Security Info=False;User ID=${azurerm_mssql_server.dbserv.administrator_login};Password=${azurerm_mssql_server.dbserv.administrator_login_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  key_vault_id = azurerm_key_vault.kv.id

  depends_on = [
    azurerm_key_vault_access_policy.current_user
  ]
}

# Allow Azure services to access SQL Server
resource "azurerm_mssql_firewall_rule" "allow_azure" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.dbserv.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# SQL Database (updated to remove license_type for serverless SKU)
resource "azurerm_mssql_database" "creditcard" {
  name                        = "CreditCard"
  server_id                   = azurerm_mssql_server.dbserv.id
  collation                   = "SQL_Latin1_General_CP1_CI_AS"
  sku_name                    = "GP_S_Gen5_1"
  max_size_gb                 = 2
  auto_pause_delay_in_minutes = 60 # Automatically pause after 60 minutes of inactivity
  min_capacity                = 0.5 # Minimum compute capacity when database is active
  zone_redundant              = false
  
  # For serverless databases, these properties help control costs
  lifecycle {
    ignore_changes = [
      # Ignore changes to auto_pause_delay_in_minutes and min_capacity
      # as they might be adjusted manually for cost optimization
      auto_pause_delay_in_minutes,
      min_capacity
    ]
  }
}
