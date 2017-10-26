$projectsToPublish = @("Protacon.RxMq.Abstractions","Protacon.RxMq.AzureServiceBus")
$projectsToTest = @("Protacon.RxMq.AzureServiceBus.Tests")

$projectsToPublish | foreach {
    $path = "$PsScriptRoot\$_\artifacts";

    if(Test-Path $path) {
        Remove-Item $path -Force -Recurse
    }
}

dotnet restore
dotnet build

$projectsToTest | foreach {
    dotnet test $PSScriptRoot\$_\$_.csproj
}

$version = if($env:APPVEYOR_REPO_TAG) {
    "$env:APPVEYOR_REPO_TAG_NAME"
} else {
    "0.0.1-beta$env:APPVEYOR_BUILD_NUMBER"
}

$projectsToPublish | foreach {
    dotnet pack $PSScriptRoot\$_\($_).csproj -c Release -o $PSScriptRoot\$_\artifacts /p:Version=$version
}