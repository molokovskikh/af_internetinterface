using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;

namespace InternetInterface.Models
{
	public class Printer
	{
		public static void Execute(string arguments)
		{
			var printerPath = Global.Config.PrinterPath;
			var info = new ProcessStartInfo(printerPath, arguments) {
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardInput = true,
				RedirectStandardOutput = true
			};
			var process = Process.Start(info);
			process.OutputDataReceived += (sender, args) => { };
			process.ErrorDataReceived += (sender, args) => { };
			process.BeginErrorReadLine();
			process.BeginOutputReadLine();
		}

		public static List<string> All()
		{
			return PrinterSettings.InstalledPrinters
				.Cast<string>()
#if !DEBUG
				.Where(p => p.Contains("Бух"))
				.OrderBy(p => p)
#endif
				.ToList();
		}
	}

}