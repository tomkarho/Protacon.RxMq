param(
    [Parameter()][string]$SettingsFile = 'developer-settings.json'
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "Reading settings from file $SettingsFile"
$settingsJson = Get-Content -Raw -Path $SettingsFile | ConvertFrom-Json

$busKey = Get-AzServiceBusKey -ResourceGroupName $settingsJson.ResourceGroupName -Namespace $settingsJson.ResourceGroupName -Name 'RootManageSharedAccessKey'

$output = @{
    ConnectionString   = $busKey.PrimaryConnectionString
    AzureResourceGroup = $settingsJson.ResourceGroupName
}

$output | ConvertTo-Json -depth 100 | Out-File "Protacon.RxMq.AzureServiceBus.Tests\client-secrets.json"