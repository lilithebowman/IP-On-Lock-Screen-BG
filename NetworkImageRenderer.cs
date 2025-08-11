using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace IPLockScreenService
{
	public static class NetworkImageRenderer
	{
		public static async Task<string> CreateBackgroundImage(string ipInfo, string outputPath, int width = 1920, int height = 1080)
		{
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
				for (int i = 0; i < displayLines.Count; i++)
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

				bitmap.Save(outputPath, ImageFormat.Png);
			});

			return outputPath;
		}
	}
}
