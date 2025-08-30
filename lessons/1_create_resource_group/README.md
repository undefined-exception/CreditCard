# Azure Resource Groups: Purpose and Creation with Terraform & Terragrunt

## What is an Azure Resource Group?

An Azure Resource Group is a **logical container** that holds related resources for an Azure solution. It acts as an organizational unit where you can deploy, manage, and monitor Azure resources that share the same lifecycle, permissions, and policies.

## Purpose of Resource Groups

### 1. **Logical Organization**
- Group related resources that belong to the same application or solution
- Example: Web app, database, storage account for a single application

### 2. **Lifecycle Management**
- All resources in a group share the same lifecycle
- Delete the entire group to remove all contained resources
- Deploy, update, or delete resources together

### 3. **Access Control (RBAC)**
- Apply role-based access control at the resource group level
- Permissions granted to the resource group apply to all resources within it

### 4. **Cost Management**
- View and manage costs for all resources in a group collectively
- Apply tags and policies at the group level

### 5. **Policy Enforcement**
- Apply Azure Policies to resource groups
- Ensure compliance across all contained resources

## Key Characteristics

- **Region**: Resource groups exist in a specific Azure region
- **Naming**: Must be unique within your subscription
- **Resources**: Can contain resources from different regions
- **Hierarchy**: Resources can only belong to one resource group

## Creating a Resource Group with Terraform

### Basic Terraform Example

```hcl
# main.tf
terraform {
  required_version = ">= 1.0"
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
  name     = "my-example-rg"
  location = "East US"
  
  tags = {
    Environment = "Development"
    Project     = "ExampleProject"
  }
}

# Output the resource group details
output "resource_group_name" {
  value = azurerm_resource_group.example.name
}

output "resource_group_location" {
  value = azurerm_resource_group.example.location
}
```

### Terraform with Variables

```hcl
# variables.tf
variable "environment" {
  description = "The deployment environment"
  type        = string
  default     = "dev"
}

variable "location" {
  description = "The Azure region"
  type        = string
  default     = "East US"
}

# main.tf
resource "azurerm_resource_group" "main" {
  name     = "rg-${var.environment}-main"
  location = var.location
  
  tags = {
    Environment = var.environment
    CreatedBy   = "Terraform"
  }
}
```

## Deployment Commands

### Terraform Commands
```bash
# Initialize
terraform init

# Plan deployment
terraform plan

# Apply changes
terraform apply

# Destroy resources
terraform destroy
```

## Best Practices

1. **Naming Convention**: Use consistent naming (e.g., `rg-environment-purpose`)
2. **Tagging**: Apply meaningful tags for cost tracking and management
3. **Least Privilege**: Assign minimal required permissions at resource group level
4. **Environment Separation**: Use separate resource groups for dev, staging, prod
5. **State Management**: Store Terraform state in a separate storage account
