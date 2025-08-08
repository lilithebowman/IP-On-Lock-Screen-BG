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

			// Log the ipInfo to the console
			_logger.LogInformation("IP Configuration Information: {ipInfo}", ipInfo);

			// Create background image with IP information
			var tempPath = Path.Combine(Path.GetTempPath(), "ip_lockscreen_bg.png");
			var imagePath = await NetworkImageRenderer.CreateBackgroundImage(ipInfo, tempPath);

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
			// Run ipconfig /all > ipconfig-output.txt
			var ipconfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ipconfig-output.txt");
			var processInfo = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = $"/c ipconfig /all > \"{ipconfigPath}\"",
				RedirectStandardOutput = false,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			using (var process = Process.Start(processInfo))
			{
				await process.WaitForExitAsync();
			}

			string output = await File.ReadAllTextAsync(ipconfigPath);

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

			// Get all network adapters with IPEnabled = true
			using var configSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = true");
			var configs = configSearcher.Get().Cast<ManagementObject>().ToList();

			// Get all network adapters for connection status
			using var adapterSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter");
			var adapters = adapterSearcher.Get().Cast<ManagementObject>().ToList();

			foreach (var obj in configs)
			{
				var description = obj["Description"]?.ToString() ?? "Unknown";
				var ipAddresses = obj["IPAddress"] as string[];
				var subnetMasks = obj["IPSubnet"] as string[];
				var gateways = obj["DefaultIPGateway"] as string[];
				var dhcpEnabled = obj["DHCPEnabled"]?.ToString() ?? "Unknown";
				var settingId = obj["SettingID"]?.ToString();

				// Find the corresponding adapter for connection status
				var adapter = adapters.FirstOrDefault(a => a["GUID"]?.ToString() == settingId);
				var netConnectionStatus = adapter?["NetConnectionStatus"] as ushort?;
				// NetConnectionStatus == 2 means 'Connected'
				bool isConnected = netConnectionStatus == 2;

				// Validate at least one valid IPv4 address
				bool hasValidIp = false;
				if (ipAddresses != null)
				{
					foreach (var ip in ipAddresses)
					{
						if (System.Net.IPAddress.TryParse(ip, out var addr))
						{
							if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
								!ip.StartsWith("169.254.") && // Exclude APIPA
								!ip.StartsWith("127.") &&      // Exclude loopback
								!string.IsNullOrWhiteSpace(ip))
							{
								hasValidIp = true;
								break;
							}
						}
					}
				}

				if (isConnected && hasValidIp)
				{
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
			}

			if (info.Count == 0)
			{
				info.Add("No connected adapters with a valid IP address found.");
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

			// Prepare text: show all lines
			var lines = ipInfo.Split('\n');
			var displayLines = lines.Select(l => l.TrimEnd('\r')).ToList();

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

			// Draw IP information in two columns, filling the first column vertically, then continuing in the second column
			float col1x = x;
			float col2x = width / 2f + 20f;
			float coly = y;
			bool inSecondCol = false;
			int linesDrawn = 0;
			for (int i = 0; i < displayLines.Count && linesDrawn < 60; i++)
			{
				var line = displayLines[i];
				if (!string.IsNullOrWhiteSpace(line))
				{
					if (!inSecondCol)
					{
						graphics.DrawString(line, textFont, shadowBrush, col1x + 1, coly + 1);
						graphics.DrawString(line, textFont, textBrush, col1x, coly);
					}
					else
					{
						graphics.DrawString(line, textFont, shadowBrush, col2x + 1, coly + 1);
						graphics.DrawString(line, textFont, textBrush, col2x, coly);
					}
				}
				coly += lineHeight;
				linesDrawn++;
				if (coly > height - 100)
				{
					if (!inSecondCol)
					{
						// Move to second column
						inSecondCol = true;
						coly = y;
					}
					else
					{
						// No more space
						break;
					}
				}
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
