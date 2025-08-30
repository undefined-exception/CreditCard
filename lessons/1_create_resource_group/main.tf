resource "azurerm_resource_group" "main" {
  name     = "rg-${var.environment}-main"
  location = var.location
  
  tags = {
    Environment = var.environment
    CreatedBy   = "Terraform"
  }
}