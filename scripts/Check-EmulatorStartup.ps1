[CmdletBinding()]
param
(
    [string]
    $IpAddress = "localhost",

    [System.TimeSpan]
    $Timeout = $(New-TimeSpan -Minutes 2),

    [int]
    $Port = 8081
)

$ErrorActionPreference = "Stop"

$StartTime = $(Get-Date)
$EndTime = $StartTime + $Timeout
$StatusCode = -1
do
{
    $StatusCode = try
    {
        $(Invoke-WebRequest -Uri "https://$($IpAddress):$($Port)/_explorer/emulator.pem" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop).BaseResponse.StatusCode
    } catch [System.Net.WebException] {
        $_.Exception.Response.StatusCode
    }

    if ($StatusCode -eq 200)
    {
        Write-Host "Emulator startup completed"
        return
    }

    Start-Sleep -Seconds 2
}
while ($(Get-Date) -lt $EndTime)

throw "Emulator not reachable within timeout $Timeout! Last retrieved status code: $StatusCode."