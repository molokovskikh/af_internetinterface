using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;
using System.Security.Cryptography;
using System.Text;
using InforoomControlPanel.Helpers;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления аутентификацией
	/// </summary>
	public class AdminOpenController : BaseController
	{
		/// <summary>
		/// Права доступа
		/// </summary>
		public JsonResult RenewActionPermissionsJs()
		{
#if DEBUG
			EmployeePermissionViewHelper.GeneratePermissions(DbSession, this);
#endif
			return null;
		}

		/// <summary>
		/// Асинхронная функция (JSON).
		/// Определяет не упал ли коммутатор, к которому подключен клиент.
		/// </summary>
		/// <param name="id">Идентификатор точки подключения клиента</param>
		public JsonResult PingEndpoint(int id)
		{
			string result = "";
			try
			{
				var endpoint = DbSession.Query<ClientEndpoint>().First(i => i.Id == id);
				var ip = endpoint.Switch.Ip;
				Ping pingSender = new Ping();
				PingOptions options = new PingOptions();
				// Use the default Ttl value which is 128,
				// but change the fragmentation behavior.
				options.DontFragment = true;

				// Create a buffer of 32 bytes of data to be transmitted.
				string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
				byte[] buffer = Encoding.ASCII.GetBytes(data);
				int timeout = 120;

				// отправлять 4 пакета
				var replyArray = new PingReply[4];
				for (int i = 0; i < replyArray.Length; i++) replyArray[i] = pingSender.Send(ip, timeout, buffer, options);

				//Отобразить пользователю:
				//-Минимальное время,
				Int64 minRoundtripTime = 0;
				//-Максимальное время,
				Int64 maxRoundtripTime = 0;
				//-Количество пакетов, которое вернулось.
				Int64 returnedPackagesNumber = 0;
				// проверка вернувшихся пакетов
				for (int i = 0; i < replyArray.Length; i++)
				{
					if (i == 0)
					{
						minRoundtripTime = replyArray[i].RoundtripTime;
						maxRoundtripTime = replyArray[i].RoundtripTime;
					}
					minRoundtripTime = minRoundtripTime > replyArray[i].RoundtripTime && replyArray[i].Status == IPStatus.Success
						? replyArray[i].RoundtripTime
						: minRoundtripTime;
					maxRoundtripTime = maxRoundtripTime < replyArray[i].RoundtripTime && replyArray[i].RoundtripTime != 0 &&
									   replyArray[i].Status == IPStatus.Success
						? replyArray[i].RoundtripTime
						: maxRoundtripTime;
					returnedPackagesNumber += replyArray[i].Status == IPStatus.Success ? 1 : 0;
				}
				//если вернулся хотя бы один пакет
				if (returnedPackagesNumber > 0)
				{
					result =
						string.Format(
							"<b style='color:{0}'>Статус: Онлайн,<br/> Пришло пакетов: {1},<br/> Скорость ответа минимальная: {2} мс.<br/> Скорость ответамаксимальная: {3} мс.</b>",
							returnedPackagesNumber > 1 ? "green" : "red",
							returnedPackagesNumber + " / " + replyArray.Length,
							minRoundtripTime,
							maxRoundtripTime);
					// если ни один пакет не вернулся
				}
				else
				{
					result = string.Format("<b style='color:red'>Коммутатор ничего не ответил</b>");
				}
			}
			catch (Exception)
			{
				result = string.Format("<b style='color:red'>Коммутатор ничего не ответил</b>");
			}

			return Json(result, JsonRequestBehavior.AllowGet);
		}






	 /// <summary>
	 /// Основная информация о коммутаторе
	 /// </summary>
	 /// <returns></returns>
	 public JsonResult ClientEndpointGetInfo(int id)
	 {
		 var cl = DbSession.Query<ClientEndpoint>().FirstOrDefault(s => s.Id == id);
		 var rawData = HardwareHelper.GetPortInformator(cl);
		 var data = new Dictionary<string, object>();
		 if (rawData != null) {
			 rawData.GetPortInfo(DbSession, data, id);
		 }
		 return Json(data, JsonRequestBehavior.AllowGet);
	 }

		/// <summary>
		/// Основная информация о коммутаторе
		/// </summary>
		/// <returns></returns>
		public JsonResult ClientEndpointGetInfoShort(int id)
		{
			var cl = DbSession.Query<ClientEndpoint>().FirstOrDefault(s => s.Id == id);
			var rawData = HardwareHelper.GetPortInformator(cl);
			var data = new Dictionary<string, object>();
			if (rawData != null) {
				rawData.GetStateOfPort(DbSession, data, id);
			}
			return Json(data, JsonRequestBehavior.AllowGet);
		}






	}
}