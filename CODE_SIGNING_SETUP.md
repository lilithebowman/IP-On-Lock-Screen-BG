# Code Signing Setup Guide

This guide will help you set up code signing for the IP Lock Screen Background Service to eliminate Windows security warnings.

## Quick Setup Options

### Option 1: Self-Signed (Free, for testing)

1. Go to your GitHub repository
2. Settings → Secrets and variables → Actions → Variables tab
3. Click "New repository variable"
4. Name: `USE_SELF_SIGNED`
5. Value: `true`
6. Click "Add variable"

**Note**: Self-signed certificates still trigger some Windows warnings, but they're useful for testing the signing process.

### Option 2: Free SSL.com Certificate (90 days)

1. Apply for a free certificate at: <https://www.ssl.com/certificates/free/>
2. Follow their verification process
3. Download your certificate (.p12 file)
4. Follow the "Purchased Certificate" steps below

### Option 3: Purchased Certificate (Recommended for production)

## Detailed Setup for Purchased Certificate

### Step 1: Purchase a Code Signing Certificate

Choose a trusted Certificate Authority:

- **Sectigo**: ~$315/year (good value)
- **GlobalSign**: ~$299/year
- **SSL.com**: ~$239/year (cheapest)
- **DigiCert**: ~$474/year (premium)

### Step 2: Prepare the Certificate

1. **Download your certificate** (should be a .p12 or .pfx file)

2. **Convert to Base64** using PowerShell:

   ```powershell
   # Replace with your actual certificate path
   $certPath = "C:\path\to\your\certificate.p12"
   $certBytes = [System.IO.File]::ReadAllBytes($certPath)
   $certBase64 = [System.Convert]::ToBase64String($certBytes)
   
   # Save to file for easy copying
   $certBase64 | Out-File "certificate_base64.txt"
   Write-Host "Certificate converted to Base64 and saved to certificate_base64.txt"
   ```

### Step 3: Add GitHub Secrets

1. **Go to your GitHub repository**
2. **Click Settings** (top right of repo page)
3. **Go to Secrets and variables → Actions**
4. **Click "New repository secret"**

5. **Add first secret**:
   - Name: `CERTIFICATE_BASE64`
   - Value: Open `certificate_base64.txt` and copy the entire content
   - Click "Add secret"

6. **Add second secret**:
   - Name: `CERTIFICATE_PASSWORD`
   - Value: Your certificate password
   - Click "Add secret"

### Step 4: Test the Setup

1. **Create a test tag**:

   ```bash
   git tag v0.1.0-test
   git push origin v0.1.0-test
   ```

2. **Check the GitHub Actions**:
   - Go to Actions tab in your repository
   - Watch the "Build and Release" workflow
   - Look for "Sign executables" step
   - Should see "✅ Successfully signed" messages

3. **Download and test**:
   - Download the release ZIP
   - Extract and run the executable
   - Should have fewer/no Windows warnings

## Troubleshooting

### Common Issues

**"Failed to sign" error**:

- Check that certificate password is correct
- Verify the certificate file is valid
- Make sure it's a code signing certificate (not SSL)

**Certificate not found error**:

- Ensure `CERTIFICATE_BASE64` secret contains the full Base64 string
- No extra spaces or line breaks
- The certificate file was properly converted

**Timestamp server errors**:

- This is usually temporary - retry the build
- The workflow uses DigiCert's timestamp server

### Verification

To verify your certificate is working:

1. **Check file properties**:
   - Right-click the executable
   - Properties → Digital Signatures tab
   - Should show your certificate details

2. **Use signtool**:

   ```cmd
   signtool verify /pa IPLockScreenService.exe
   ```

## Certificate Management

### Renewal

- Code signing certificates typically last 1-3 years
- Set a calendar reminder to renew before expiration
- Update the GitHub secrets with the new certificate

### Security

- **Never commit certificates to code**
- Keep certificate files secure
- Use strong passwords
- Consider using hardware security modules (HSM) for high-value certificates

### Cost Optimization

- **Multi-year purchases** often have discounts
- **OV certificates** are cheaper than EV but work fine for executables
- **Compare providers** - prices vary significantly

## Next Steps

After setting up code signing:

1. **Update documentation** to mention signed executables
2. **Remove self-signed fallback** once you have a real certificate
3. **Consider automating certificate renewal** for long-term projects

## Support

If you need help:

- Check GitHub Actions logs for specific error messages
- Open an issue in the repository
- Consult your Certificate Authority's documentation
