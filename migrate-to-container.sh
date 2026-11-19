# Fill in the actual deployed resource names and a published commit SHA.
RG="MorWalPiz"
APP="range-api"
ACR="MorWalPizACR"
IMAGE_TAG="latest"

az login
# Optional if you have multiple subscriptions:
az account set --subscription "your-subscription-id"

# Confirm the app's plan is Linux. If this returns false, stop:
# a Linux container requires a Linux App Service plan.
PLAN_ID=$(az webapp show --resource-group "$RG" --name "$APP" --query serverFarmId -o tsv)
az appservice plan show --ids "$PLAN_ID" --query reserved -o tsv

ACR_ID=$(az acr show --name "$ACR" --query id -o tsv)
ACR_LOGIN_SERVER=$(az acr show --name "$ACR" --query loginServer -o tsv)
IMAGE="$ACR_LOGIN_SERVER/shooting-range-client:$IMAGE_TAG"

# Let the app pull from the private registry using managed identity.
PRINCIPAL_ID=$(az webapp identity assign \
  --resource-group "$RG" --name "$APP" --query principalId -o tsv)

az role assignment create \
  --assignee-object-id "$PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --role AcrPull \
  --scope "$ACR_ID"

az webapp config set \
  --resource-group "$RG" --name "$APP" \
  --generic-configurations '{"acrUseManagedIdentityCreds": true}'

# Switch the existing app to the published container image.
az webapp config container set \
  --resource-group "$RG" --name "$APP" \
  --docker-custom-image-name "$IMAGE" \
  --docker-registry-server-url "https://$ACR_LOGIN_SERVER"



az webapp restart --resource-group "$RG" --name "$APP"

# Verify the configured image.
az webapp config show \
  --resource-group "$RG" --name "$APP" \
  --query linuxFxVersion -o tsv