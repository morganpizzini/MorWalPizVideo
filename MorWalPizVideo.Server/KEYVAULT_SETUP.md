# Azure KeyVault Integration

This document explains how to configure and use Azure KeyVault integration in the MorWalPizVideo.Server application.

## Overview

The application now supports Azure KeyVault integration through a feature flag system. When enabled, the application can securely retrieve configuration values from Azure KeyVault, allowing you to store sensitive information like connection strings, API keys, and other secrets securely.

## Configuration

### Feature Flag

The KeyVault integration is controlled by the `EnableKeyVault` feature flag in the `FeatureManagement` section of your configuration files.

### appsettings.json
```json
{
  "KeyVaultUrl": "https://your-keyvault-name.vault.azure.net/",
  "FeatureManagement": {
    "EnableKeyVault": true
  }
}
```

### appsettings.Development.json
```json
{
  "FeatureManagement": {
    "EnableKeyVault": false
  }
}
```

## Authentication

The implementation uses `DefaultAzureCredential` which supports multiple authentication methods in the following order:

1. **Environment Variables** - Service Principal authentication
   - `AZURE_CLIENT_ID`
   - `AZURE_CLIENT_SECRET`
   - `AZURE_TENANT_ID`

2. **Managed Identity** - When running in Azure (App Service, Azure Functions, etc.)

3. **Azure CLI** - When developing locally with Azure CLI logged in
   ```bash
   az login
   ```

4. **Visual Studio** - When developing with Visual Studio signed in to Azure

5. **Azure PowerShell** - When using Azure PowerShell

## Setup Steps

### 1. Create Azure KeyVault
```bash
# Create a resource group (if needed)
az group create --name myResourceGroup --location eastus

# Create KeyVault
az keyvault create --name myKeyVault --resource-group myResourceGroup --location eastus
```

### 2. Add Secrets to KeyVault
```bash
# Add secrets
az keyvault secret set --vault-name myKeyVault --name "MorWalPizDatabase--ConnectionString" --value "your-connection-string"
az keyvault secret set --vault-name myKeyVault --name "BlobStorage--ConnectionString" --value "your-blob-connection-string"
az keyvault secret set --vault-name myKeyVault --name "RecaptchaSecretKey" --value "your-recaptcha-key"
```

**Note**: Use double dashes (`--`) in secret names to represent configuration hierarchy. For example, `MorWalPizDatabase--ConnectionString` becomes `MorWalPizDatabase:ConnectionString` in configuration.

### 3. Grant Access Permissions

#### For Development (Azure CLI)
```bash
# Get your user principal ID
az ad user show --id your-email@domain.com --query objectId --output tsv

# Grant access
az keyvault set-policy --name myKeyVault --object-id YOUR_OBJECT_ID --secret-permissions get list
```

#### For Production (Service Principal)
```bash
# Create service principal
az ad sp create-for-rbac --name myApp --role reader --scopes /subscriptions/SUBSCRIPTION_ID/resourceGroups/myResourceGroup/providers/Microsoft.KeyVault/vaults/myKeyVault

# Grant KeyVault access
az keyvault set-policy --name myKeyVault --spn CLIENT_ID --secret-permissions get list
```

### 4. Configure Application Settings

#### For Local Development
Set environment variables or use Azure CLI login:
```bash
az login
```

#### For Azure App Service
Set application settings:
- `AZURE_CLIENT_ID`: Your service principal client ID
- `AZURE_CLIENT_SECRET`: Your service principal client secret
- `AZURE_TENANT_ID`: Your Azure tenant ID

Or enable System Assigned Managed Identity and grant KeyVault access.

## Usage Examples

### Configuration Hierarchy
When KeyVault is enabled, secrets override local configuration values. The configuration precedence is:

1. KeyVault secrets (highest priority)
2. Environment variables
3. appsettings.{Environment}.json
4. appsettings.json (lowest priority)

### Secret Naming Convention
- Use double dashes (`--`) to represent configuration sections
- Example: `MorWalPizDatabase--ConnectionString` → `MorWalPizDatabase:ConnectionString`

### Common Secrets to Store in KeyVault
- `MorWalPizDatabase--ConnectionString`: MongoDB connection string
- `BlobStorage--ConnectionString`: Azure Blob Storage connection string
- `RecaptchaSecretKey`: Google reCAPTCHA secret key
- `YouTubeApiKey`: YouTube Data API key
- Any other sensitive configuration values

## Error Handling

The implementation includes robust error handling:

- If KeyVault is unreachable, the application logs a warning and continues with local configuration
- If the KeyVault URL is not configured, a warning is logged
- The application will not fail to start due to KeyVault issues

## Security Best Practices

1. **Principle of Least Privilege**: Only grant necessary permissions (get, list) to secrets
2. **Environment Separation**: Use different KeyVaults for different environments
3. **Secret Rotation**: Regularly rotate secrets and update KeyVault
4. **Monitoring**: Enable KeyVault logging and monitoring
5. **Network Security**: Consider using Private Endpoints for KeyVault access

## Troubleshooting

### Common Issues

1. **Authentication Failed**
   - Ensure proper authentication method is configured
   - Check Azure CLI login: `az account show`
   - Verify service principal credentials

2. **Access Denied**
   - Check KeyVault access policies
   - Verify the application has `get` and `list` permissions

3. **KeyVault Not Found**
   - Verify the KeyVault URL is correct
   - Ensure the KeyVault exists in the specified subscription

### Debugging

Enable detailed logging by setting log level to `Debug` in appsettings:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Azure": "Debug"
    }
  }
}
```

## Example Production Configuration

### appsettings.Production.json
```json
{
  "KeyVaultUrl": "https://myapp-prod-kv.vault.azure.net/",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Azure": "Warning"
    }
  },
  "FeatureManagement": {
    "EnableKeyVault": true,
    "EnableDev": false,
    "EnableSwagger": false,
    "EnableMock": false
  }
}
```

This setup ensures secure, scalable configuration management for your production environment.
