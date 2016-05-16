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

		public ActionResult Error()
		{
			ViewBag.BreadCrumb = "";
			return View();
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



		/// <summary>
		/// Возвращение списка улиц по региону.
		/// </summary>
		/// <param name="regionId">Id региона</param>
		/// <param name="count">кол-во улиц</param>
		/// <returns>Изменение произошло</returns>
		[HttpPost]
		public JsonResult GetStreetNumberChangedFlag(int regionId, int count)
		{
			var equals = DbSession.Query<Street>()
				.Count(s => s.Region.Id == regionId || s.Houses.Any(a => a.Region.Id == regionId)) != count;
			return Json(equals, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Возвращение списка улиц по региону.
		/// </summary>
		/// <param name="regionId">Id региона</param>
		/// <param name="streetId">Id улицы</param>
		/// <param name="count">кол-во улиц</param>
		/// <returns>Изменение произошло</returns>
		[HttpPost]
		public JsonResult GetHouseNumberChangedFlag(int streetId, int count, int regionId = 0)
		{
			var equals = false;
			if (regionId != 0)
			{
				equals = DbSession.Query<House>().Count(s => (s.Region == null || regionId == s.Region.Id) &&
																										 ((s.Street.Region.Id == regionId && s.Street.Id == streetId) || (s.Street.Id == streetId && s.Region.Id == regionId)) &&
																										 (s.Street.Region.Id == regionId && s.Region == null || (s.Street.Id == streetId && s.Region.Id == regionId))
					) != count;
			}
			else
			{
				equals = DbSession.Query<House>().Count(s => s.Street.Id == streetId) != count;
			}
			return Json(equals, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Возвращение списка улиц по региону.
		/// </summary>
		/// <param name="regionId">Id региона</param>
		/// <returns>Json* Список в форме: Id, Name, Geomark, Confirmed, Region (Id), Houses (кол-во)</returns>
		[HttpPost]
		public JsonResult GetStreetList(int regionId)
		{
			var streets = DbSession.Query<Street>().
				Where(s => s.Region.Id == regionId || s.Houses.Any(a => a.Region.Id == regionId)).
				Select(s => new {
					Id = s.Id,
					Name = s.Name,
					Geomark = s.Geomark,
					Confirmed = s.Confirmed,
					Region = s.Region.Id,
					Houses = s.Houses.Count
				}).OrderBy(s => s.Name).ToList();
			return Json(streets, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Возвращение списка домов по улице.
		/// </summary>
		/// <param name="regionId">Id региона</param>
		/// <param name="streetId">Id улицы</param>
		/// <returns>Json* Список в форме: Id, Number, Geomark, Confirmed, Street (Id), EntranceAmount ,ApartmentAmount</returns>
		[HttpPost]
		public JsonResult GetHouseList(int streetId, int regionId = 0)
		{
			var query = DbSession.Query<House>();
			if (regionId != 0)
			{
				query = query.Where(s => (s.Region == null || regionId == s.Region.Id) &&
																 ((s.Street.Region.Id == regionId && s.Street.Id == streetId) || (s.Street.Id == streetId && s.Region.Id == regionId)) &&
																 (s.Street.Region.Id == regionId && s.Region == null || (s.Street.Id == streetId && s.Region.Id == regionId))
					);
			}
			else
			{
				query = query.Where(s => s.Street.Id == streetId);
			}
			var houses = query.Select(s => new {
				Id = s.Id,
				Number = s.Number,
				Geomark = s.Geomark,
				Confirmed = s.Confirmed,
				Street = s.Street.Id,
				EntranceAmount = s.EntranceAmount,
				ApartmentAmount = s.ApartmentAmount
			}).OrderBy(s => s.Number).ToList();
			return Json(houses, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Получение тарифов по региону
		/// </summary>
		/// <param name="regionId">Id региона</param>
		/// <returns>Json* Список в форме: Id, Name, Price</returns>
		[HttpPost]
		public JsonResult GetPlansListForRegion(int regionId)
		{
			var planList = DbSession.Query<Plan>()
				.Where(s => s.Disabled == false && s.AvailableForNewClients && s.RegionPlans.Any(d => d.Region.Id == regionId))
				.Select(d => new {
					Id = d.Id,
					Name = d.Name,
					Price = d.Price
				}).OrderBy(s => s.Name).ToList();
			return Json(planList, JsonRequestBehavior.AllowGet);
		}



	}
}