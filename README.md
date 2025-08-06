# IP Lock Screen Background Service

A Windows service that automatically updates the Windows lock screen background with current network configuration information obtained from `ipconfig`.

## Features

- Runs as a Windows service that starts automatically on boot
- Gathers network information using `ipconfig /all` and WMI queries
- Generates an attractive PNG image with gradient background showing:
  - Network adapter information
  - IP addresses
  - Subnet masks
  - Default gateways
  - DNS servers
  - DHCP status
- Updates the lock screen background every 5 minutes
- Professional-looking output with anti-aliased text and gradient backgrounds

## Requirements

- Windows 10/11
- .NET 8.0 Runtime (included in self-contained deployment)
- Administrator privileges for service installation and registry modifications

## Installation

1. **Build and Install Service:**
   - Right-click on `install-service.bat` and select "Run as administrator"
   - The script will build the project and install it as a Windows service

2. **Manual Installation:**

   ```cmd
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   sc create "IPLockScreenService" binPath="[full-path-to-exe]" start=auto DisplayName="IP Lock Screen Background Service"
   sc start "IPLockScreenService"
   ```

## Uninstallation

Run `uninstall-service.bat` as administrator to remove the service.

## How It Works

1. **Service Startup:** The service starts automatically with Windows
2. **Data Collection:** Every 5 minutes, it runs `ipconfig /all` and queries WMI for network adapter information
3. **Image Generation:** Creates a 1920x1080 PNG image with:
   - Gradient blue background
   - Decorative accent elements
   - Network configuration text in a readable font
   - Current timestamp
4. **Background Update:** Sets the generated image as the Windows lock screen background via registry modification

## Configuration

The service logs its activities to the Windows Event Log. You can modify the update interval by changing the delay in the `Worker.cs` file (currently set to 5 minutes).

## Permissions

The service requires:

- Registry write access to `HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization`
- File system write access to `%WINDOWS%\System32\oobe\info\backgrounds\`
- Network adapter enumeration permissions

## Troubleshooting

1. **Service won't start:** Check Windows Event Log for error messages
2. **Background not updating:** Ensure the service has proper permissions
3. **Build errors:** Verify .NET 8.0 SDK is installed

## Files

- `Program.cs` - Service host configuration
- `Worker.cs` - Main service logic
- `IPLockScreenService.csproj` - Project file with dependencies
- `install-service.bat` - Service installation script
- `uninstall-service.bat` - Service removal script
