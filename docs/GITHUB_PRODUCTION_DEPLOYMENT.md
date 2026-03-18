# GitHub Production Deployment Setup

This document provides all the required parameters and manual setup steps for deploying the MorWalPizVideo project to Azure using GitHub Actions.

## Overview

The project uses GitHub Actions for CI/CD with the following deployment strategy:

- **Frontend Applications**: Deployed as Docker containers to Azure Web App for Containers
  - `back-office-spa`
  - `morwalpizvideo.client`

- **Backend APIs**: Deployed as .NET applications to Azure Web Apps
  - `MorWalPizVideo.BackOffice`
  - `MorWalPizVideo.ServerAPI`

## Prerequisites

- Azure subscription with appropriate permissions
- Azure CLI installed locally
- GitHub repository with admin access
- .NET 10.0 SDK
- Node.js 22.x
- Docker (for local testing)

## GitHub Environment Configuration

### Create GitHub Environment

1. Navigate to your GitHub repository
2. Go to **Settings** → **Environments**
3. Click **New environment**
4. Name it: `production`
5. Click **Configure environment**

### Required Secrets

Add the following secrets to the `production` environment:

| Secret Name | Description | How to Obtain |
|-------------|-------------|---------------|
| `AZURE_CLIENT_ID` | Azure Service Principal Client ID | Created during Azure AD App Registration |
| `AZURE_TENANT_ID` | Azure Active Directory Tenant ID | Found in Azure Portal → Azure Active Directory → Overview |
| `AZURE_SUBSCRIPTION_ID` | Azure Subscription ID | Found in Azure Portal → Subscriptions |
| `AZURE_CLIENT_SECRET` | Azure Service Principal Client Secret | Created during Service Principal setup |

### Required Variables

Add the following variables to the `production` environment:

| Variable Name | Description | Example Value |
|---------------|-------------|---------------|
| `AZURE_CONTAINER_REGISTRY_LOGIN_SERVER` | ACR login server URL | `myregistry.azurecr.io` |
| `BACK_OFFICE_SPA_APP_NAME` | Azure Web App name for back-office-spa | `morwalpiz-backoffice-spa` |
| `MORWALPIZVIDEO_CLIENT_APP_NAME` | Azure Web App name for client | `morwalpiz-client` |
| `BACKOFFICE_API_APP_NAME` | Azure Web App name for BackOffice API | `morwalpiz-backoffice-api` |
| `SERVERAPI_APP_NAME` | Azure Web App name for ServerAPI | `morwalpiz-serverapi` |

## Azure Resource Setup

### 1. Create Resource Group

```bash
az group create \
  --name rg-morwalpiz-prod \
  --location eastus
```

### 2. Create Azure Container Registry

```bash
az acr create \
  --resource-group rg-morwalpiz-prod \
  --name morwalpizregistry \
  --sku Standard \
  --admin-enabled false
```

**Note the login server URL** (e.g., `morwalpizregistry.azurecr.io`)

### 3. Create Service Principal for GitHub Actions

```bash
# Get the ACR resource ID
ACR_ID=$(az acr show --name morwalpizregistry --query id --output tsv)

# Get the subscription ID
SUBSCRIPTION_ID=$(az account show --query id --output tsv)

# Create service principal with required permissions
az ad sp create-for-rbac \
  --name "github-actions-morwalpiz" \
  --role "Contributor" \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-morwalpiz-prod \
  --sdk-auth

# Grant ACR push/pull permissions
az role assignment create \
  --assignee <CLIENT_ID_FROM_PREVIOUS_COMMAND> \
  --role "AcrPush" \
  --scope $ACR_ID
```

Save the output JSON - you'll need:
- `clientId` → `AZURE_CLIENT_ID`
- `clientSecret` → `AZURE_CLIENT_SECRET`
- `tenantId` → `AZURE_TENANT_ID`
- `subscriptionId` → `AZURE_SUBSCRIPTION_ID`

### 4. Configure Federated Credentials for OIDC (Recommended)

Instead of using client secrets, configure OIDC for passwordless authentication:

```bash
# Create federated credential for production environment
az ad app federated-credential create \
  --id <APPLICATION_OBJECT_ID> \
  --parameters '{
    "name": "github-actions-production",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:YOUR_GITHUB_USERNAME/MorWalPizVideo:environment:production",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

### 5. Create Azure Web Apps for Frontend (Containers)

#### Back Office SPA
```bash
az webapp create \
  --resource-group rg-morwalpiz-prod \
  --plan asp-morwalpiz-prod \
  --name morwalpiz-backoffice-spa \
  --deployment-container-image-name morwalpizregistry.azurecr.io/back-office-spa:latest

# Configure ACR integration
az webapp config container set \
  --name morwalpiz-backoffice-spa \
  --resource-group rg-morwalpiz-prod \
  --docker-custom-image-name morwalpizregistry.azurecr.io/back-office-spa:latest \
  --docker-registry-server-url https://morwalpizregistry.azurecr.io

# Enable managed identity and grant ACR access
az webapp identity assign \
  --resource-group rg-morwalpiz-prod \
  --name morwalpiz-backoffice-spa

PRINCIPAL_ID=$(az webapp identity show --resource-group rg-morwalpiz-prod --name morwalpiz-backoffice-spa --query principalId --output tsv)

az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "AcrPull" \
  --scope $ACR_ID

# Configure port
az webapp config appsettings set \
  --resource-group rg-morwalpiz-prod \
  --name morwalpiz-backoffice-spa \
  --settings WEBSITES_PORT=80
```

#### MorWalPizVideo Client
```bash
az webapp create \
  --resource-group rg-morwalpiz-prod \
  --plan asp-morwalpiz-prod \
  --name morwalpiz-client \
  --deployment-container-image-name morwalpizregistry.azurecr.io/morwalpizvideo-client:latest

# Configure ACR integration
az webapp config container set \
  --name morwalpiz-client \
  --resource-group rg-morwalpiz-prod \
  --docker-custom-image-name morwalpizregistry.azurecr.io/morwalpizvideo-client:latest \
  --docker-registry-server-url https://morwalpizregistry.azurecr.io

# Enable managed identity and grant ACR access
az webapp identity assign \
  --resource-group rg-morwalpiz-prod \
  --name morwalpiz-client

PRINCIPAL_ID=$(az webapp identity show --resource-group rg-morwalpiz-prod --name morwalpiz-client --query principalId --output tsv)

az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "AcrPull" \
  --scope $ACR_ID

# Configure port
az webapp config appsettings set \
  --resource-group rg-morwalpiz-prod \
  --name morwalpiz-client \
  --settings WEBSITES_PORT=80
```

### 6. Create Azure Web Apps for Backend APIs

#### BackOffice API
```bash
az webapp create \
  --resource-group rg-morwalpiz-prod \
  --plan asp-morwalpiz-prod \
  --name morwalpiz-backoffice-api \
  --runtime "DOTNET:10.0"

# Configure app settings (add your specific configuration)
az webapp config appsettings set \
  --resource-group rg-morwalpiz-prod \
  --name morwalpiz-backoffice-api \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production
```

#### ServerAPI
```bash
az webapp create \
  --resource-group rg-morwalpiz-prod \
  --plan asp-morwalpiz-prod \
  --name morwalpiz-serverapi \
  --runtime "DOTNET:10.0"

# Configure app settings (add your specific configuration)
az webapp config appsettings set \
  --resource-group rg-morwalpiz-prod \
  --name morwalpiz-serverapi \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production
```

### 7. Configure App Service Plans

If you need to create a new App Service Plan:

```bash
# For Linux containers (frontends)
az appservice plan create \
  --name asp-morwalpiz-prod \
  --resource-group rg-morwalpiz-prod \
  --is-linux \
  --sku B1

# For Windows/.NET apps (backends) - if needed separately
az appservice plan create \
  --name asp-morwalpiz-api-prod \
  --resource-group rg-morwalpiz-prod \
  --sku B1
```

## Application Configuration

### Frontend Environment Variables

Configure runtime environment variables for each frontend Web App:

#### Back Office SPA
```bash
az webapp config appsettings set \
  --resource-group rg-morwalpiz-prod \
  --name morwalpiz-backoffice-spa \
  --settings \
    VITE_API_BASE_URL=https://morwalpiz-backoffice-api.azurewebsites.net
```

#### MorWalPizVideo Client
```bash
az webapp config appsettings set \
  --resource-group rg-morwalpiz-prod \
  --name morwalpiz-client \
  --settings \
    VITE_API_BASE_URL=https://morwalpiz-serverapi.azurewebsites.net
```

### Backend Configuration

Add application-specific settings for each API. Common settings include:

```bash
# Example for BackOffice API
az webapp config appsettings set \
  --resource-group rg-morwalpiz-prod \
  --name morwalpiz-backoffice-api \
  --settings \
    MorWalPizDatabase__ConnectionString="<mongodb-connection-string>" \
    AzureConfig__OpenAi__OpenAiKey="<openai-key>" \
    AzureConfig__OpenAi__OpenAiEndpoint="<openai-endpoint>" \
    JwtSettings__Secret="<jwt-secret>" \
    KeyVaultUrl="<keyvault-url>"
```

## Manual Setup Checklist

### Azure Setup
- [ ] Create Azure Resource Group
- [ ] Create Azure Container Registry
- [ ] Create Service Principal with appropriate roles
- [ ] Configure federated credentials for OIDC (optional but recommended)
- [ ] Create App Service Plan(s)
- [ ] Create Azure Web Apps for frontends (2x container apps)
- [ ] Create Azure Web Apps for backends (2x .NET apps)
- [ ] Configure ACR pull permissions for Web App managed identities
- [ ] Set WEBSITES_PORT=80 for frontend container apps

### GitHub Setup
- [ ] Create `production` environment in GitHub
- [ ] Add `AZURE_CLIENT_ID` secret
- [ ] Add `AZURE_TENANT_ID` secret
- [ ] Add `AZURE_SUBSCRIPTION_ID` secret
- [ ] Add `AZURE_CLIENT_SECRET` secret (if not using OIDC)
- [ ] Add `AZURE_CONTAINER_REGISTRY_LOGIN_SERVER` variable
- [ ] Add `BACK_OFFICE_SPA_APP_NAME` variable
- [ ] Add `MORWALPIZVIDEO_CLIENT_APP_NAME` variable
- [ ] Add `BACKOFFICE_API_APP_NAME` variable
- [ ] Add `SERVERAPI_APP_NAME` variable

### Application Configuration
- [ ] Configure frontend environment variables (API URLs)
- [ ] Configure backend app settings (database, secrets, etc.)
- [ ] Set up Azure Key Vault (if using)
- [ ] Configure MongoDB connection strings
- [ ] Configure external service APIs (YouTube, Discord, Telegram, etc.)
- [ ] Set up SSL certificates (handled by Azure by default)
- [ ] Configure custom domains (if needed)

### Validation
- [ ] Test CI workflow runs successfully
- [ ] Trigger manual deployment for each application
- [ ] Verify Docker images are pushed to ACR
- [ ] Verify Web Apps pull and run correct images
- [ ] Test frontend applications load correctly
- [ ] Test backend APIs respond correctly
- [ ] Verify environment variables are loaded
- [ ] Check application logs for errors
- [ ] Test end-to-end functionality

## Troubleshooting

### Docker Image Build Fails
- Check Dockerfile syntax
- Verify all required files are included in build context
- Review GitHub Actions logs for specific errors

### Azure Web App Deployment Fails
- Verify service principal has correct permissions
- Check Web App logs: `az webapp log tail --name <app-name> --resource-group <rg-name>`
- Ensure ACR credentials are correctly configured
- Verify managed identity has AcrPull role

### Application Not Starting
- Check WEBSITES_PORT is set to 80 for containers
- Review application logs in Azure Portal
- Verify environment variables are set correctly
- Check for missing configuration values

### Authentication Issues
- Verify service principal credentials are current
- Check federated credential configuration if using OIDC
- Ensure subscription ID and tenant ID are correct

## Workflow Triggers

All deployment workflows can be triggered:
1. **Automatically**: On push to `main` branch when relevant files change
2. **Manually**: Via GitHub Actions UI using workflow_dispatch

## Security Best Practices

1. **Use OIDC authentication** instead of long-lived secrets
2. **Store sensitive values** in Azure Key Vault
3. **Use managed identities** for Azure resource authentication
4. **Rotate secrets** regularly
5. **Limit service principal** permissions to minimum required
6. **Enable diagnostic logging** for all Azure resources
7. **Configure network restrictions** on production resources

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Web Apps Documentation](https://docs.microsoft.com/azure/app-service/)
- [Azure Container Registry Documentation](https://docs.microsoft.com/azure/container-registry/)
- [Azure OIDC with GitHub Actions](https://docs.microsoft.com/azure/developer/github/connect-from-azure)