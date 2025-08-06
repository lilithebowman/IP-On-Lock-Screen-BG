# Security Notice for IP Lock Screen Background Service

## Why Windows Shows Security Warnings

When you download and run the IP Lock Screen Background Service executables, Windows may display security warnings such as:

- "Windows protected your PC"
- "This app can't run on your PC"
- "This file is not commonly downloaded"

This is a **false positive** and happens because:

1. **The executables are not code-signed** - Code signing certificates are expensive (~$300+/year)
2. **Self-contained .NET executables** often trigger antivirus heuristics
3. **Low download frequency** - New releases don't have enough downloads to build reputation

## This Software is Safe

### Open Source Transparency

- **Full source code** is available at: <https://github.com/lilithebowman/IP-On-Lock-Screen-BG>
- **Built automatically** using GitHub Actions (publicly visible build process)
- **No hidden code** - everything is reviewable

### What the Software Does

- Reads local network configuration using standard Windows commands (`ipconfig`)
- Creates a background image with this information
- Updates the Windows lock screen background
- **No network communication** beyond local system queries
- **No data collection** or transmission

### Build Verification

- Check the GitHub Actions build logs to see exactly how the executables were built
- Compare file hashes if desired (available in GitHub release)
- All dependencies are standard Microsoft .NET libraries

## How to Safely Use the Software

### Option 1: Allow through Windows Security

1. When you see "Windows protected your PC":
   - Click "More info"
   - Click "Run anyway"

### Option 2: Manually Unblock Files

1. Right-click the downloaded ZIP file
2. Select "Properties"
3. Check "Unblock" at the bottom
4. Click "OK"
5. Extract the files

### Option 3: Add Exclusion (if you trust the software)

1. Open Windows Security
2. Go to Virus & threat protection
3. Manage settings under "Virus & threat protection settings"
4. Add an exclusion for the folder containing the executables

### Option 4: Build from Source

If you're still concerned, you can build the software yourself:

```cmd
git clone https://github.com/lilithebowman/IP-On-Lock-Screen-BG.git
cd IP-On-Lock-Screen-BG
dotnet publish IPLockScreenService.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## For Developers: Code Signing

If you fork this project and want to avoid these warnings:

1. Purchase a code signing certificate from a trusted CA
2. Add these secrets to your GitHub repository:
   - `CERTIFICATE_BASE64`: Base64-encoded certificate file
   - `CERTIFICATE_PASSWORD`: Certificate password
3. The GitHub Actions workflow will automatically sign the executables

## Reporting False Positives

You can help improve detection by reporting false positives to:

- **Microsoft Defender**: <https://www.microsoft.com/en-us/wdsi/filesubmission>
- **VirusTotal**: Upload the file to check multiple antivirus engines

## Contact

If you have security concerns or questions, please:

- Open an issue on the GitHub repository
- Review the source code yourself
- Contact the maintainer through GitHub

---

**Remember**: Always be cautious with software from unknown sources. The transparency of this open-source project and public build process should provide confidence in its safety.
