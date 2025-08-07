# Credit Card Application System - Azure Learning Project

## Overview

This repository was inspired by a System Design interview question, showcasing a complete Azure infrastructure implementation for a credit card application processing system. The project leverages Microsoft Learn's free Azure subscription for experimentation and learning purposes.

**Important**: This is not meant for production use - it's purely for educational purposes.

## Key Features

- **Infrastructure as Code**: Terraform automates the setup of secure Azure infrastructure
- **Configuration & Secret Management**: Connection strings stored securely in Azure Key Vault
- **Resilient Distributed System**:
  - Background service for async processing
  - Polly retry strategies for transient errors
  - Proper timeout configurations
- **Observability**: Integrated Application Insights for monitoring
- **CI/CD**: Automated deployment via GitHub Actions

## System Architecture

The application:
1. Accepts credit card applications (storing as "Pending" in DB)
2. Uses a background service to poll status updates
3. Updates application status ("Approved"/"Denied")
4. Notifies users of status changes

Includes a mock credit bureau API for testing purposes.

## Prerequisites

- Microsoft Learn Azure sandbox subscription ([Activate here](https://learn.microsoft.com/en-us/training/modules/publish-app-service-static-web-app-api/4-exercise-static-web-apps?pivots=angular&source=learn))
- Terraform installed locally
- Azure CLI installed

## Setup Instructions

1. **Azure Login**:
   ```bash
   az login
   ```
   - Select "Concierge Subscription"
   - In Azure Portal, switch directory to "Concierge Subscription"

2. **Configure Terraform**:
   - Copy your subscription ID to:
     - `main.tf`
     - `terraform.tfvars`
   - Add the default resource group name to `terraform.tfvars`

3. **Deploy Infrastructure**:
   ```bash
   terraform init
   terraform plan
   terraform apply
   ```
   *Note: Deployment takes ~5 minutes*

## CI/CD Configuration

Add these secrets to your GitHub repository:

1. `AZURE_WEBAPP_NAME_API1` (e.g., `app-eastus2-win-sbx-b9`)
2. `AZURE_PUBLISH_PROFILE_API1` (content from Azure Portal)
3. `AZURE_WEBAPP_NAME_API2` (e.g., `cred-serv1-eastus2-win-sbx-b9`)
4. `AZURE_PUBLISH_PROFILE_API2` (content from Azure Portal)

Push to main branch to trigger deployment.

## Accessing the Application

The entry point will be: `app-eastus2-win-sbx-b9.azurewebsites.net` (exact name may vary based on your terraform config)

## Security Notes

- Database only accessible via configured VNet
- Key Vault uses System Assigned Identities for secure access
- *Limitation*: Azure sandbox doesn't allow AzureConfig service creation

## Ensuring Unique Resource Names
To avoid naming collisions in Azure's global namespace, the Terraform configuration includes random suffixes for resource names. Here's how it works:

The main Terraform configuration appends a random suffix (like b9) to service names

This ensures your deployment URLs are globally unique (e.g., app-eastus2-win-sbx-b9.azurewebsites.net)

## Support

This repository is provided "as-is" for learning purposes. For issues or questions, please contact the author: Lőrinc Sándor.

## Cleanup

Remember that the Microsoft Learn sandbox automatically tears down resources after several hours.
