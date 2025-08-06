using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace TestRunner;

class TestProgram
{
	static async Task Main(string[] args)
	{
		Console.WriteLine("IP Lock Screen Background Generator - Test Mode");
		Console.WriteLine("================================================");

		try
		{
			// Get IP configuration
			Console.WriteLine("Getting IP configuration...");
			var ipInfo = await GetIPConfigurationInfo();

			// Create test image
			Console.WriteLine("Creating background image...");
			var imagePath = await CreateBackgroundImage(ipInfo);

			Console.WriteLine($"Background image created at: {imagePath}");
			Console.WriteLine("Opening image...");

			// Open the image with default application
			Process.Start(new ProcessStartInfo(imagePath) { UseShellExecute = true });

			Console.WriteLine("Test completed successfully!");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error: {ex.Message}");
		}

		Console.WriteLine("Press any key to exit...");
		Console.ReadKey();
	}


	private static async Task<string> GetIPConfigurationInfo()
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

		return $"Network Configuration - {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n{output}";
	}

	private static async Task<string> CreateBackgroundImage(string ipInfo)
	{
		const int width = 1920;
		const int height = 1080;

		var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
			$"ip_background_test_{DateTime.Now:yyyyMMdd_HHmmss}.png");

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

			// Add decorative elements
			using var accentBrush = new SolidBrush(Color.FromArgb(100, 120, 200, 255));
			graphics.FillEllipse(accentBrush, width - 300, -100, 400, 400);
			graphics.FillEllipse(accentBrush, -100, height - 300, 400, 400);

			// Draw text
			using var titleFont = new Font("Segoe UI", 24, FontStyle.Bold);
			using var textFont = new Font("Consolas", 12, FontStyle.Regular);
			using var textBrush = new SolidBrush(Color.White);
			using var shadowBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0));

			var lines = ipInfo.Split('\n');
			var y = 50f;
			var x = 50f;
			var lineHeight = 18f;

			// Draw title
			graphics.DrawString("Network Configuration", titleFont, shadowBrush, x + 2, y + 2);
			graphics.DrawString("Network Configuration", titleFont, textBrush, x, y);
			y += 50;

			// Draw IP information
			foreach (var line in lines)
			{
				if (!string.IsNullOrWhiteSpace(line.Trim()))
				{
					graphics.DrawString(line, textFont, shadowBrush, x + 1, y + 1);
					graphics.DrawString(line, textFont, textBrush, x, y);
				}
				y += lineHeight;

				if (y > height - 100) break;
			}

			bitmap.Save(outputPath, ImageFormat.Png);
		});

		return outputPath;
	}
}