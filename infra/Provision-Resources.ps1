param(
    [Parameter(Mandatory=$true)]
    [string]
    $ResourceGroup,
    [Parameter(Mandatory=$true)]
    [string]
    $Location,
    [Parameter(Mandatory=$true)]
    [GUID]
    $ServicePrincipal
)

Write-Output "Creating Resource Group $ResourceGroup in $Location"
az group create --name $ResourceGroup --location $Location

Write-Output "Deploying to Resource Group $ResourceGroup"
$result = az deployment group create --name "Provision-$(Get-Random)" --resource-group $ResourceGroup --template-file $PSScriptRoot/main.bicep --parameters @$PSScriptRoot/main.parameters.json --parameters principalId=$ServicePrincipal | ConvertFrom-Json

Write-Output "OK"
Write-Output ""

$sentinelWorkspaceName = $result.properties.outputs.sentinelWorkspaceName.value
$appInsightsName = $result.properties.outputs.appInsightsName.value

Write-Output "Provisioned sentinel workspace $sentinelWorkspaceName"
Write-Output "Provisioned Application Insights $appInsightsName"
Write-Output ""

#TODO: Get Instrumentation Key and API Key for Application Insights
#az monitor app-insights api-key show --app demoApp -g demoRg --api-key demo-key

Write-Output "Copy these values to config.toml:"
Write-Output ""

$dcrImmutableId = $result.properties.outputs.dcrImmutableId.value
$endpointUri = $result.properties.outputs.endpointUri.value
$stream = $result.properties.outputs.stream.value

Write-Output "[LogIngestion]"
Write-Output "EndpointUri = ""$endpointUri"""
Write-Output "DcrImmutableId = ""$dcrImmutableId"""
Write-Output "Stream = ""$stream"""

Write-Output "When finished, run:"
Write-Output "az group delete --name $ResourceGroup"
