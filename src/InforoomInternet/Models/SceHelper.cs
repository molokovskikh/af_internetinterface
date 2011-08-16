using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using InternetInterface.Models;
using log4net;

namespace InforoomInternet.Models
{
	//{action=login|logout} {subscriberId} {packageId} {monitor=1|0} {additive=true|false} [ip_or_mask=ip address|network]
	public class SceHelper
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (SceHelper));

		public static string SceHelperPath = @"SceHelper\com.sce.helper.jar";
		public static string JavaPath = @"java";

		public static void Action(string action, Lease lease)
		{
			try
			{
				var endpoint = lease.Endpoint;
				if (endpoint == null)
					return;

				var packageId = endpoint.PackageId;

				if (packageId == null)
					return;

				var command = String.Format("-jar \"{0}\" {1} {2} {3} {4} {5} {6}",
					SceHelperPath,
					action,
					endpoint.Id,
					packageId,
					endpoint.Monitoring ? 1 : 0,
					endpoint.IsMultilease.ToString().ToLower(),
					lease.Ip);
				RunCommand(command);
			}
			catch (Exception e)
			{
				_log.Error(String.Format("ошибка при применении настрок для sce, номер аренды {0}", lease.Id), e);
			}
		}

		public static void Login(Lease lease)
		{
			Action("login", lease);
		}

		public static void Logout(Lease lease)
		{
			Action("logout", lease);
		}

		public static void RunCommand(string command)
		{
			var output = "";
			var error = "";
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
	}
}