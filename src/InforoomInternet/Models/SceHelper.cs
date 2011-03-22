using System;
using System.Diagnostics;
using System.IO;
using log4net;

namespace InforoomInternet.Models
{
	public class SceHelper
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (SceHelper));

		public static string SceHelperPath = @"U:\Apps\dhcp\SceHelper\com.sce.helper.jar";
		public static string JavaPath = @"C:\Program Files (x86)\Java\j2re1.4.2_19\bin\java";

		public static void Login(Lease lease, ClientEndpoint endpoint, string ip)
		{
			try
			{
				var packageId = endpoint.PackageId;

				if (endpoint.Id == 0
					|| packageId == null)
					return;

				var output = "";
				var error = "";
				Console.WriteLine("package id = {0}", packageId);
				var command = String.Format("-jar \"{0}\" {1} {2} {3} {4}",
					SceHelperPath,
					endpoint.Id,
					ip,
					packageId,
					endpoint.Monitoring ? 1 : 0);
				var startInfo = new ProcessStartInfo(JavaPath, command) {
					WorkingDirectory = Path.GetDirectoryName(SceHelperPath),
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardError = true,
					RedirectStandardOutput = true
				};

				var process = Process.Start(startInfo);
				process.OutputDataReceived += (sender, args) => {
					output += args.Data + "\r\n";
				};
				process.ErrorDataReceived += (sender, args) => {
					error += args.Data + "\r\n";
				};
				process.BeginErrorReadLine();
				process.BeginOutputReadLine();
				process.WaitForExit();
				if (process.ExitCode != 0)
				{
					_log.ErrorFormat("ошибка при применении настрок для sce, {0} {1}", error, output);
				}
			}
			catch (Exception e)
			{
				_log.Error(String.Format("ошибка при применении настрок для sce, номер аренды {0}", lease.Id), e);
			}
		}
	}
}