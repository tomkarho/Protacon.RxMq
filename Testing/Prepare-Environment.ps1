<#
    .SYNOPSIS
    Creates testing environment in Azure from given settings file

    .DESCRIPTION
    Creates and prepares and environment for development and testing.
    SettingsFile (default developer-settings.json) should contain all
    related settings

    .PARAMETER SettinsFile
    Settings file that contains environment settings.
    Defaults to 'developer-settings.json'
#>
param(
    [Parameter()][string]$SettingsFile = 'developer-settings.json',
    [Parameter()][string]$EnvironmentName = $SettingsFile.ResourceGroupName
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "Reading settings from file $SettingsFile"
$settingsJson = Get-Content -Raw -Path $SettingsFile | ConvertFrom-Json

$tagsHashtable = @{ }
if ($settingsJson.Tags) {
    $settingsJson.Tags.psobject.properties | ForEach-Object { $tagsHashtable[$_.Name] = $_.Value }
}

Write-Host "Creating resource group $($EnvironmentName) to location $($settingsJson.Location)..."
New-AzResourceGroup -Name $EnvironmentName -Location $settingsJson.Location -Tag $tagsHashtable -Force

Write-Host 'Creating environment...'
New-AzResourceGroupDeployment `
    -Name 'test-deployment' `
    -TemplateFile 'Testing/azuredeploy.json' `
    -ResourceGroupName $EnvironmentName `
    -serviceBusName $EnvironmentName `
