using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Castle.ActiveRecord;
using InternetInterface.Models;
using log4net;

namespace InforoomInternet.Models
{
	//{action=login|logout} {subscriberId} {packageId} {monitor=1|0} {additive=true|false} [ip_or_mask=ip address|network]
	public class SceHelper
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(SceHelper));

		public static string SceHelperPath = @"U:\Apps\dhcp\com.sce.helper\com.sce.helper.jar";
		public static string JavaPath = @"java";

		public static int? Action(string action, string ip, string subscriberId, bool monitoring, bool IsMultilease, int packageId)
		{
			var noError = true;
			try {
				var command = String.Format("-jar \"{0}\" {1} {2} {3} {4} {5} {6}",
					SceHelperPath,
					action,
					subscriberId,
					packageId,
					monitoring ? 1 : 0,
					IsMultilease.ToString().ToLower(),
					ip);
				_log.DebugFormat("Применение настроек для sce {0}", command);
				RunCommand(command);
				_log.DebugFormat("Применены настройки для sce {0}", command);
			}
			catch (Exception e) {
				_log.Error(String.Format("ошибка при применении настроек для sce, ip {0}", ip), e);
				noError = false;
			}
			if (noError) {
				if (action == "login")
					return packageId;
				if (action == "logout")
					return null;
			}
			return null;
		}

		public static int? Action(string action, Lease lease, string ip)
		{
			var endpoint = lease.Endpoint;
			if (endpoint == null)
				return null;

			return Action(action, endpoint, ip);
		}

		public static int? Action(string action, ClientEndpoint endpoint, string ip)
		{
			var packageId = endpoint.PackageId;
			if (packageId == null)
				return null;

			return Action(action, ip, Convert.ToString(endpoint.Id), endpoint.Monitoring, endpoint.IsMultilease, packageId.Value);
		}

		public static int? Login(Lease lease, string ip)
		{
			return Action("login", lease, ip);
		}

		public static void Logout(Lease lease, string ip)
		{
			Action("logout", lease, ip);
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
#if !DEBUG
			var process = Process.Start(startInfo);
			process.OutputDataReceived += (sender, args) => { output += args.Data + "\r\n"; };
			process.ErrorDataReceived += (sender, args) => { error += args.Data + "\r\n"; };
			process.BeginErrorReadLine();
			process.BeginOutputReadLine();
			process.WaitForExit();
			if (process.ExitCode != 0) {
				_log.ErrorFormat("ошибка при применении настроек для sce, команда {2}, {0} {1}", error, output, command);
			}
#endif
		}
	}
}