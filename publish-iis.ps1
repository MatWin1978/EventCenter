# EventCenter — IIS Publish-Skript
# Ausführen: .\publish-iis.ps1
# Output: .\publish\EventCenter\  (bereit zum Kopieren auf den IIS-Server)

$ProjectPath = ".\EventCenter.Web\EventCenter.Web.csproj"
$OutputPath  = ".\publish\EventCenter"

Write-Host "Publiziere EventCenter fuer IIS..." -ForegroundColor Cyan

dotnet publish $ProjectPath `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=false `
    -o $OutputPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "Fehler beim Publish!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Fertig! Publish-Ausgabe: $OutputPath" -ForegroundColor Green
Write-Host ""
Write-Host "Naechste Schritte:" -ForegroundColor Yellow
Write-Host "  1. Ordner '$OutputPath' auf den IIS-Server kopieren"
Write-Host "  2. appsettings.Production.json anpassen (DB, Keycloak, SMTP)"
Write-Host "  3. IIS Application Pool: .NET CLR Version = 'Kein verwalteter Code'"
Write-Host "  4. ASP.NET Core Hosting Bundle auf dem Server installieren (falls noch nicht vorhanden)"
Write-Host "     -> https://dotnet.microsoft.com/download/dotnet/8.0 -> 'Hosting Bundle'"
Write-Host "  5. IIS-Site auf den Publish-Ordner zeigen lassen"
Write-Host "  6. App Pool Identity muss Lese-/Schreibrecht auf den Ordner haben"
