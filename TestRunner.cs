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

			// Log the ipInfo to the console
			Console.WriteLine($"IP Configuration Information: {ipInfo}");

			// Create test image
			Console.WriteLine("Creating background image...");
			var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
				$"ip_background_test_{DateTime.Now:yyyyMMdd_HHmmss}.png");
			var imagePath = await IPLockScreenService.NetworkImageRenderer.CreateBackgroundImage(ipInfo, outputPath);

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

	// ...existing code...
}