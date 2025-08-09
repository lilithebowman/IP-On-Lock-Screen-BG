# IP Lock Screen Service Management Script
param(
	[Parameter(Mandatory = $true)]
	[ValidateSet("install", "uninstall", "start", "stop", "status", "build")]
	[string]$Action
)

$ServiceName = "IPLockScreenService"
$DisplayName = "IP Lock Screen Background Service"
$Description = "Updates Windows lock screen background with current IP configuration information"

function Test-Administrator {
	$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
	$principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
	return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Build-Project {
	Write-Host "Building project..." -ForegroundColor Green
	dotnet publish IPLockScreenService.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
	if ($LASTEXITCODE -eq 0) {
		Write-Host "Build completed successfully." -ForegroundColor Green
	}
 else {
		Write-Host "Build failed." -ForegroundColor Red
		exit 1
	}
}

function Install-Service {
	if (-not (Test-Administrator)) {
		Write-Host "Administrator privileges required. Please run as administrator." -ForegroundColor Red
		exit 1
	}

	Build-Project
    
	$ExePath = Join-Path $PSScriptRoot "bin\Release\net6.0-windows\win-x64\publish\IPLockScreenService.exe"
    
	if (-not (Test-Path $ExePath)) {
		Write-Host "Executable not found at: $ExePath" -ForegroundColor Red
		exit 1
	}

	# Stop and remove existing service if it exists
	$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
	if ($existingService) {
		Write-Host "Stopping existing service..." -ForegroundColor Yellow
		Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
		sc.exe delete $ServiceName
		Start-Sleep -Seconds 2
	}

	Write-Host "Installing service..." -ForegroundColor Green
	sc.exe create $ServiceName binPath= $ExePath start= auto DisplayName= $DisplayName depend= Tcpip
	sc.exe description $ServiceName $Description
    
	Write-Host "Starting service..." -ForegroundColor Green
	Start-Service -Name $ServiceName
    
	Write-Host "Service installed and started successfully." -ForegroundColor Green
}

function Uninstall-Service {
	if (-not (Test-Administrator)) {
		Write-Host "Administrator privileges required. Please run as administrator." -ForegroundColor Red
		exit 1
	}

	Write-Host "Stopping service..." -ForegroundColor Yellow
	Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    
	Write-Host "Uninstalling service..." -ForegroundColor Green
	sc.exe delete $ServiceName
    
	Write-Host "Service uninstalled successfully." -ForegroundColor Green
}

function Start-ServiceCustom {
	Write-Host "Starting service..." -ForegroundColor Green
	Start-Service -Name $ServiceName
	Write-Host "Service started." -ForegroundColor Green
}

function Stop-ServiceCustom {
	Write-Host "Stopping service..." -ForegroundColor Yellow
	Stop-Service -Name $ServiceName -Force
	Write-Host "Service stopped." -ForegroundColor Yellow
}

function Get-ServiceStatus {
	$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
	if ($service) {
		Write-Host "Service Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq 'Running') { 'Green' } else { 'Yellow' })
		Write-Host "Service Display Name: $($service.DisplayName)"
		Write-Host "Service Start Type: $($service.StartType)"
	}
 else {
		Write-Host "Service not found." -ForegroundColor Red
	}
}

# Main execution
switch ($Action) {
	"install" { Install-Service }
	"uninstall" { Uninstall-Service }
	"start" { Start-ServiceCustom }
	"stop" { Stop-ServiceCustom }
	"status" { Get-ServiceStatus }
	"build" { Build-Project }
}

Write-Host "`nDone." -ForegroundColor Cyan
