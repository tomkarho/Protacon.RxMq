param(
    [Parameter()][string]$SettingsFile = 'developer-settings.json',
    [Parameter()][Switch]$CreateServicePrincipal
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "Reading settings from file $SettingsFile"
$settingsJson = Get-Content -Raw -Path $SettingsFile | ConvertFrom-Json

$context = Get-AzContext

if ($CreateServicePrincipal -eq $true) {
    $credentials = New-Object `
        -TypeName Microsoft.Azure.Commands.ActiveDirectory.PSADPasswordCredential `
        -Property @{ `
            StartDate = Get-Date; `
            EndDate   = '2099-01-01'; `
            Password  = 'ExamplePassword!1'; `
    
    }`

    New-AzAdServicePrincipal `
        -DisplayName 'TestingServicePrincipal' `
        -PasswordCredential $credentials `
        -Role Reader `
        -Scope "/subscriptions/$($context.Subscription.Id)/resourceGroups/$($settingsJson.ResourceGroupName)"
}

$busKey = Get-AzServiceBusKey -ResourceGroupName $settingsJson.ResourceGroupName -Namespace $settingsJson.ResourceGroupName -Name 'RootManageSharedAccessKey'

$output = @{
    ConnectionString         = $busKey.PrimaryConnectionString
    AzureResourceGroup       = $settingsJson.ResourceGroupName
    AzureSubscriptionId      = $context.Subscription.Id
    AzureSpAppId             = $settingsJson.ServicePrincipal.AppId
    AzureSpPassword          = $settingsJson.ServicePrincipal.Password
    AzureSpTenantId          = $settingsJson.ServicePrincipal.TenantId
    AzureNamespace           = $settingsJson.ResourceGroupName
    AzureRetryMinimumBackoff = 5
    AzureRetryMaximumBackoff = 10
    AzureMaximumRetryCount   = 10
}

$output | ConvertTo-Json -depth 100 | Out-File "Protacon.RxMq.AzureServiceBus.Tests\client-secrets.json"