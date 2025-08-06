using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Management;
using Microsoft.Win32;

namespace IPLockScreenService;

public class Worker : BackgroundService
{
	private readonly ILogger<Worker> _logger;
	private readonly string _backgroundImagePath;

	public Worker(ILogger<Worker> logger)
	{
		_logger = logger;
		_backgroundImagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
			"System32", "oobe", "info", "backgrounds", "backgroundDefault.jpg");
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("IP Lock Screen Background Service started at: {time}", DateTimeOffset.Now);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await UpdateLockScreenBackground();
				_logger.LogInformation("Lock screen background updated at: {time}", DateTimeOffset.Now);

				// Wait for 5 minutes before next update
				await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating lock screen background");
				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
			}
		}
	}

	private async Task UpdateLockScreenBackground()
	{
		try
		{
			// Get IP configuration information
			var ipInfo = await GetIPConfigurationInfo();

			// Create background image with IP information
			var imagePath = await CreateBackgroundImage(ipInfo);

			// Set as lock screen background
			await SetLockScreenBackground(imagePath);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to update lock screen background");
			throw;
		}
	}

	private async Task<string> GetIPConfigurationInfo()
	{
		try
		{
			var processInfo = new ProcessStartInfo
			{
				FileName = "ipconfig",
				Arguments = "/all",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var process = Process.Start(processInfo);
			if (process == null)
				throw new InvalidOperationException("Failed to start ipconfig process");

			var output = await process.StandardOutput.ReadToEndAsync();
			await process.WaitForExitAsync();

			// Also get additional network information
			var additionalInfo = GetNetworkAdapterInfo();

			return $"Network Configuration - {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n{output}\n\n{additionalInfo}";
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to get IP configuration");
			return $"Error retrieving network information: {ex.Message}";
		}
	}

	private string GetNetworkAdapterInfo()
	{
		try
		{
			var info = new List<string>();

			using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = true");
			foreach (ManagementObject obj in searcher.Get())
			{
				var description = obj["Description"]?.ToString() ?? "Unknown";
				var ipAddresses = obj["IPAddress"] as string[];
				var subnetMasks = obj["IPSubnet"] as string[];
				var gateways = obj["DefaultIPGateway"] as string[];
				var dhcpEnabled = obj["DHCPEnabled"]?.ToString() ?? "Unknown";

				info.Add($"Adapter: {description}");
				if (ipAddresses != null)
					info.Add($"  IP Addresses: {string.Join(", ", ipAddresses)}");
				if (subnetMasks != null)
					info.Add($"  Subnet Masks: {string.Join(", ", subnetMasks)}");
				if (gateways != null)
					info.Add($"  Gateways: {string.Join(", ", gateways)}");
				info.Add($"  DHCP Enabled: {dhcpEnabled}");
				info.Add("");
			}

			return string.Join("\n", info);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to get network adapter information");
			return "Error retrieving adapter information";
		}
	}

	private async Task<string> CreateBackgroundImage(string ipInfo)
	{
		const int width = 1920;
		const int height = 1080;

		var tempPath = Path.Combine(Path.GetTempPath(), "ip_lockscreen_bg.png");

		await Task.Run(() =>
		{
			using var bitmap = new Bitmap(width, height);
			using var graphics = Graphics.FromImage(bitmap);

			// Set high quality rendering
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

			// Create gradient background
			using var brush = new LinearGradientBrush(
				new Rectangle(0, 0, width, height),
				Color.FromArgb(20, 30, 50),
				Color.FromArgb(40, 60, 90),
				LinearGradientMode.Vertical);

			graphics.FillRectangle(brush, 0, 0, width, height);

			// Add some decorative elements
			using var accentBrush = new SolidBrush(Color.FromArgb(100, 120, 200, 255));
			graphics.FillEllipse(accentBrush, width - 300, -100, 400, 400);
			graphics.FillEllipse(accentBrush, -100, height - 300, 400, 400);

			// Prepare text
			var lines = ipInfo.Split('\n');
			var displayLines = new List<string>();

			// Filter and format the most important information
			foreach (var line in lines)
			{
				var trimmedLine = line.Trim();
				if (string.IsNullOrWhiteSpace(trimmedLine) ||
					trimmedLine.Contains("Media State") ||
					trimmedLine.Contains("Connection-specific DNS Suffix") ||
					trimmedLine.Contains("Description") ||
					trimmedLine.Contains("Physical Address") ||
					trimmedLine.Contains("DHCP Enabled") ||
					trimmedLine.Contains("Autoconfiguration Enabled") ||
					trimmedLine.Contains("Link-local IPv6 Address") ||
					trimmedLine.Contains("IPv4 Address") ||
					trimmedLine.Contains("Subnet Mask") ||
					trimmedLine.Contains("Default Gateway") ||
					trimmedLine.Contains("DNS Servers") ||
					trimmedLine.Contains("Network Configuration") ||
					trimmedLine.Contains("Adapter:") ||
					trimmedLine.Contains("IP Addresses:") ||
					trimmedLine.Contains("Gateways:"))
				{
					displayLines.Add(trimmedLine);
				}

				// Limit the number of lines to prevent overflow
				if (displayLines.Count > 30)
					break;
			}

			// Draw text
			using var titleFont = new Font("Segoe UI", 24, FontStyle.Bold);
			using var textFont = new Font("Consolas", 12, FontStyle.Regular);
			using var textBrush = new SolidBrush(Color.White);
			using var shadowBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0));

			var y = 50f;
			var x = 50f;
			var lineHeight = 18f;

			// Draw title with shadow
			graphics.DrawString("Network Configuration", titleFont, shadowBrush, x + 2, y + 2);
			graphics.DrawString("Network Configuration", titleFont, textBrush, x, y);
			y += 50;

			// Draw IP information with shadow
			foreach (var line in displayLines.Take(25))
			{
				if (!string.IsNullOrWhiteSpace(line))
				{
					graphics.DrawString(line, textFont, shadowBrush, x + 1, y + 1);
					graphics.DrawString(line, textFont, textBrush, x, y);
				}
				y += lineHeight;

				if (y > height - 100) break; // Prevent overflow
			}

			// Save the image
			bitmap.Save(tempPath, ImageFormat.Png);
		});

		return tempPath;
	}

	private async Task SetLockScreenBackground(string imagePath)
	{
		try
		{
			await Task.Run(() =>
			{
				// Copy image to Windows backgrounds directory
				var targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
					"System32", "oobe", "info", "backgrounds");

				Directory.CreateDirectory(targetPath);

				var targetFile = Path.Combine(targetPath, "backgroundDefault.jpg");
				File.Copy(imagePath, targetFile, true);

				// Update registry to use custom lock screen
				using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Personalization", true);
				if (key != null)
				{
					key.SetValue("LockScreenImage", targetFile);
					key.SetValue("NoChangingLockScreen", 1);
				}
				else
				{
					// Create the registry key if it doesn't exist
					using var parentKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows", true) ??
										 Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows");
					using var newKey = parentKey.CreateSubKey("Personalization");
					newKey.SetValue("LockScreenImage", targetFile);
					newKey.SetValue("NoChangingLockScreen", 1);
				}
			});

			_logger.LogInformation("Lock screen background updated successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to set lock screen background");
			throw;
		}
	}
}
