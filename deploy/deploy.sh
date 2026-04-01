#!/usr/bin/env bash
# deploy.sh — Container Group deployen oder updaten
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.env"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Fehler: $ENV_FILE nicht gefunden. Zuerst setup.sh ausführen."
  exit 1
fi

# shellcheck source=.env
source "$ENV_FILE"

if [[ -z "${STORAGE_KEY:-}" ]]; then
  echo "→ STORAGE_KEY aus Azure laden..."
  STORAGE_KEY=$(az storage account keys list \
    --account-name "$STORAGE_ACCOUNT" \
    --resource-group "$RESOURCE_GROUP" \
    --query "[0].value" -o tsv)
  export STORAGE_KEY
fi

echo "→ ACI-Konfiguration generieren..."
export RESOURCE_GROUP LOCATION STORAGE_ACCOUNT STORAGE_KEY DNS_LABEL \
       KC_ADMIN_PASSWORD KC_CLIENT_SECRET MSSQL_SA_PASSWORD

envsubst < "$SCRIPT_DIR/aci-template.yaml" > /tmp/aci-deploy.yaml

echo "→ Container Group deployen..."
az container create \
  --resource-group "$RESOURCE_GROUP" \
  --file /tmp/aci-deploy.yaml \
  --output none

echo "→ Status prüfen..."
az container show \
  --resource-group "$RESOURCE_GROUP" \
  --name eventcenter \
  --query "{Status:instanceView.state, FQDN:ipAddress.fqdn}" \
  --output table

FQDN=$(az container show \
  --resource-group "$RESOURCE_GROUP" \
  --name eventcenter \
  --query "ipAddress.fqdn" -o tsv)

echo ""
echo "✓ Deployment abgeschlossen!"
echo ""
echo "  App:      http://$FQDN:5270"
echo "  Keycloak: http://$FQDN:8080"
echo "  Mailpit:  http://$FQDN:8025"
echo ""
echo "Hinweis: SQL Server + Keycloak brauchen ~60 Sekunden zum Hochfahren."
