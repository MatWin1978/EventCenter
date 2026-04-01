#!/usr/bin/env bash
# setup.sh — einmalig ausführen, um Azure-Infrastruktur zu erstellen
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.env"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Fehler: $ENV_FILE nicht gefunden."
  echo "Bitte deploy/.env.example als deploy/.env kopieren und ausfüllen."
  exit 1
fi

# shellcheck source=.env
source "$ENV_FILE"

echo "→ Resource Group erstellen..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output none

echo "→ Storage Account erstellen..."
az storage account create \
  --name "$STORAGE_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku Standard_LRS \
  --output none

STORAGE_KEY=$(az storage account keys list \
  --account-name "$STORAGE_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --query "[0].value" -o tsv)

echo "→ File Shares erstellen..."
for SHARE in sqlserver-data keycloak-data keycloak-import data-protection-keys uploads; do
  az storage share create \
    --name "$SHARE" \
    --account-name "$STORAGE_ACCOUNT" \
    --account-key "$STORAGE_KEY" \
    --output none
  echo "   ✓ $SHARE"
done

echo "→ Keycloak Realm-Konfiguration hochladen..."
az storage file upload \
  --account-name "$STORAGE_ACCOUNT" \
  --account-key "$STORAGE_KEY" \
  --share-name keycloak-import \
  --source "$SCRIPT_DIR/../keycloak/realm-export.json" \
  --path realm-export.json \
  --output none

# STORAGE_KEY in .env speichern
if grep -q "^STORAGE_KEY=" "$ENV_FILE"; then
  sed -i "s|^STORAGE_KEY=.*|STORAGE_KEY=$STORAGE_KEY|" "$ENV_FILE"
else
  echo "STORAGE_KEY=$STORAGE_KEY" >> "$ENV_FILE"
fi

echo ""
echo "✓ Setup abgeschlossen!"
echo ""
echo "  Resource Group:   $RESOURCE_GROUP"
echo "  Storage Account:  $STORAGE_ACCOUNT"
echo "  STORAGE_KEY wurde in deploy/.env gespeichert."
echo ""
echo "Nächster Schritt: ./deploy/deploy.sh"
