using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using Common.MySql;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Models;
using log4net;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Util;

namespace Inforoom2.Helpers
{
	public class SceHelper
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(SceHelper));
		public static string SceHelperPath = @"U:\Apps\dhcp\com.sce.helper\com.sce.helper.jar";
		public static string JavaPath = @"java";
		private static readonly BlockingCollection<int> ClientIdList = new BlockingCollection<int>();
		private static CancellationTokenSource StopRunning;
		private static Task CurrentTask;
		private static string _elementToAdd;

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
			try
			{
				RunCommand(command);
			}
			catch (Exception e)
			{
				noError = false;
			}
			if (noError)
			{
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
			if (endpoint.Disabled || endpoint.Client.Disabled)
			{
				packageId = 100;
			}
			if (endpoint.Client.ShowBalanceWarningPage)
			{
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
			var startInfo = new ProcessStartInfo(JavaPath, command)
			{
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
			foreach (var endpoint in client.Endpoints)
			{
				UpdatePackageId(session, endpoint);
			}
		}

		private static int? Action(string action, int endpointId, int? endpointPackageId, bool endpointDisabled, bool endpointMonitoring, bool clientDisabled, bool clientShowWarningPage, params string[] ips)
		{
			if (endpointPackageId == null)
				return null;
			if (endpointDisabled || clientDisabled)
			{
				endpointPackageId = 100;
			}
			if (clientShowWarningPage)
			{
				endpointPackageId = 10;
			}

			return Action(action, endpointId.ToString(), endpointMonitoring, true, endpointPackageId.Value, ips);
		}


		private static void UpdatePackageId(MySqlConnection db, int endpointId)
		{
			var dataSetIpList = db.FillDataSet(@"
				SELECT ls.Ip as 'leasesIp', st.Ip as 'staticIp'
				 FROM internet.clientendpoints as en
				LEFT JOIN  internet.StaticIps as st ON st.EndPoint = en.Id
				LEFT JOIN  internet.leases as ls ON ls.EndPoint = en.Id 
				WHERE en.Id = @endpointId
				", new { @endpointId = endpointId });

			var dataSetEndpointInfo = db.FillDataSet(@"
				SELECT en.Id, en.PackageId, en.Disabled as EndpointDisabled, en.Monitoring, cl.Disabled as ClientDisabled, cl.ShowBalanceWarningPage
				 FROM internet.clientendpoints as en
				LEFT JOIN  internet.clients as cl ON cl.Id = en.`Client` 
				WHERE en.Id = @endpointId
				", new { @endpointId = endpointId });

			var ipList = new List<string>();
			for (int i = 0; i < (dataSetIpList.Tables.Count > 0 && dataSetIpList.Tables[0].Rows.Count > 0 ? dataSetIpList.Tables[0].Rows.Count : 0); i++)
			{
				var leasesIp = new IPAddress(Convert.ToInt64(!(dataSetIpList.Tables[0].Rows[i]["leasesIp"] is DBNull) ? (dataSetIpList.Tables[0].Rows[i]["leasesIp"] ?? 0) : 0)).ToString();
				var staticIp = (dataSetIpList.Tables[0].Rows[i]["staticIp"] ?? "").ToString();
				_elementToAdd = string.IsNullOrEmpty(staticIp) || staticIp == "0.0.0.0" ? (string.IsNullOrEmpty(leasesIp) || leasesIp == "0.0.0.0" ? "" : leasesIp) : staticIp;
				var elementToAdd = _elementToAdd;
				if (!string.IsNullOrEmpty(elementToAdd))
				{
					ipList.Add(elementToAdd);
				}
			}
			ipList = ipList.OrderBy(s => s).ToList();

			if (ipList.Count == 0)
				return;

			if (dataSetEndpointInfo.Tables.Count > 0 && dataSetEndpointInfo.Tables[0].Rows.Count > 0)
			{
				var packageId = (!(dataSetEndpointInfo.Tables[0].Rows[0]["PackageId"] is DBNull) ? (int?)Convert.ToInt32(dataSetEndpointInfo.Tables[0].Rows[0]["PackageId"]) : null);
				var endpointDisabled = (!(dataSetEndpointInfo.Tables[0].Rows[0]["EndpointDisabled"] is DBNull) ? (int?)Convert.ToInt32(dataSetEndpointInfo.Tables[0].Rows[0]["EndpointDisabled"]) : null);
				var monitoring = (!(dataSetEndpointInfo.Tables[0].Rows[0]["Monitoring"] is DBNull) ? (int?)Convert.ToInt32(dataSetEndpointInfo.Tables[0].Rows[0]["Monitoring"]) : null);
				var clientDisabled = (!(dataSetEndpointInfo.Tables[0].Rows[0]["ClientDisabled"] is DBNull) ? (int?)Convert.ToInt32(dataSetEndpointInfo.Tables[0].Rows[0]["ClientDisabled"]) : null);
				var showBalanceWarningPage = (!(dataSetEndpointInfo.Tables[0].Rows[0]["ShowBalanceWarningPage"] is DBNull) ? (int?)Convert.ToInt32(dataSetEndpointInfo.Tables[0].Rows[0]["ShowBalanceWarningPage"]) : null);

				var actualPackageId = Action("login", endpointId, packageId,
					endpointDisabled.HasValue && endpointDisabled.Value == 1,
					monitoring.HasValue && monitoring.Value == 1,
					clientDisabled.HasValue && clientDisabled.Value == 1,
					showBalanceWarningPage.HasValue && showBalanceWarningPage.Value == 1,
					ipList.ToArray());

				db.Execute(@"
						UPDATE internet.clientendpoints 
						SET ActualPackageId = @actualPackageId
						WHERE id = @currentId;
						", new { @currentId = endpointId, @actualPackageId = actualPackageId.HasValue ? actualPackageId.Value.ToString() : "'Null'" });
			}
		}

		private static void QueueBuilder()
		{
			//открываем соединение с БД
			var connectionString = System.Configuration.ConfigurationManager.AppSettings["nhibernateConnectionString"];
			var currentClientId = 0;
			var currentEndpointId = 0;
			while (!StopRunning.IsCancellationRequested)
			{
				try
				{
					if (ClientIdList.Count == 0)
					{
						CurrentTask.Wait(4000, StopRunning.Token);
						continue;
					}
					currentClientId = ClientIdList.Take();
					using (MySqlConnection db = new MySqlConnection(connectionString))
					{
						db.Open();

						//логиним клиентов в SCE, обновляя PackageId по одному
						var endpointIdList = db.FillDataSet(@"
						SELECT en.Id
						 FROM internet.clientendpoints as en 
						WHERE en.IsEnabled = 1 AND en.Disabled = 0 AND en.client = @currentClientId
						", new { @currentClientId = currentClientId.ToString() });

						for (int i = 0; i < (endpointIdList.Tables.Count > 0 && endpointIdList.Tables[0].Rows.Count > 0 ? endpointIdList.Tables[0].Rows.Count : 0); i++)
						{
							currentEndpointId = Convert.ToInt32(endpointIdList.Tables[0].Rows[i]["Id"] ?? 0);
							if (currentEndpointId != 0)
							{
								UpdatePackageId(db, currentEndpointId);
							}
						}
					}
				}
				catch
					(Exception
						ex)
				{
					//обрабатываем исключение
					var errorMessage =
						string.Format(@"[internet] Ошибка при обновлении PackageId на SceHelper:
							Id клиента: {0},
							Id точки подключения: {1}", currentClientId, currentEndpointId);
					log.Error(errorMessage, ex);
				}
			}
		}

		/// <summary>
		/// Запуск /в Global.asax
		/// </summary>
		public static void StartRun()
		{
			StopRunning = new CancellationTokenSource();
			CurrentTask = Task.Factory.StartNew(() => QueueBuilder());
		}

		/// <summary>
		/// Остановка /в Global.asax
		/// </summary>
		public static void StopRun()
		{
			StopRunning.Cancel();
			ClientIdList.CompleteAdding();
			CurrentTask.Wait();
		}

		public static void UpdatePackageId(int clientId)
		{
			try
			{
				//если клиента нет в списке, добавляем его в очередь
				if (
					ClientIdList.All(s => s != clientId))
				{
					ClientIdList.TryAdd(clientId, 2000, StopRunning.Token);
				}
			}
			catch (OperationCanceledException ex)
			{
				//обрабатываем исключение
				var errorMessage = string.Format(@"[internet] Ошибка при завершении приложения на SceHelper:
							Не обработано Id клиента: {0}, : {1}", clientId);

				log.Error(errorMessage, ex);
			}
		}
	}
}