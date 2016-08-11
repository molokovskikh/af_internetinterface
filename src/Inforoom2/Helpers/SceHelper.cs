using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Linq;

namespace Inforoom2.Helpers
{
	
	public class SceHelper
	{
		public static string SceHelperPath = @"U:\Apps\dhcp\com.sce.helper\com.sce.helper.jar";
		public static string JavaPath = @"java";

		public static int? Action(string action, string subscriberId, bool monitoring, bool additive, int packageId, params string[] ips)
		{
			var noError = true;
			var command = String.Format("-jar \"{0}\" {1} {2} {3} {4} {5} {6}",
				SceHelperPath,
				action,
				subscriberId,
				packageId,
				monitoring ? 1 : 0,
				additive.ToString().ToLower(),
				ips.Implode(" "));
			try {
				RunCommand(command);
			}
			catch (Exception e) {
				
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

		public static int? Action(string action, ClientEndpoint endpoint, params string[] ips)
		{
			var packageId = endpoint.PackageId;
			if (packageId == null)
				return null;
			if (endpoint.Disabled || endpoint.Client.Disabled) {
				packageId = 100;
			}
			if (endpoint.Client.ShowBalanceWarningPage) {
				packageId = 10;
			}

			return Action(action, endpoint.Id.ToString(), endpoint.Monitoring, true, packageId.Value, ips);
		}

		public static int? Login(ClientEndpoint endpoint, params string[] ip)
		{
			return Action("login", endpoint, ip);
		}

		public static void Logout(Lease lease, params string[] ip)
		{
			if (lease.Endpoint == null)
				return;
			Action("logout", lease.Endpoint, ip);
		}

		private static void RunCommand(string command)
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
				//_log.ErrorFormat("ошибка при применении настроек для sce, команда {2}, {0} {1}", error, output, command);
			}
#endif
			
		}

		public static void UpdatePackageId(ISession session, ClientEndpoint endpoint)
		{
			var addresses = session.Query<Lease>().Where(l => l.Endpoint == endpoint).ToArray()
				.Select(l => l.Ip.ToString())
				.Concat(session.Query<StaticIp>().Where(s => s.EndPoint == endpoint).ToArray()
					.Select(s => s.Address()))
				.ToArray();
			if (addresses.Length == 0)
				return;
			endpoint.UpdateActualPackageId(Login(endpoint, addresses));
			session.Save(endpoint);
			//Кажется это должно решить проблему с дедлоком базы на странице warning
			session.Flush();
		}

		public static void UpdatePackageId(ISession session, Client client)
		{
			foreach (var endpoint in client.Endpoints.Where(s=>!s.Disabled).ToList())
				UpdatePackageId(session, endpoint);
		}
	}
}