using System.Linq;
using System.Web.Mvc;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	///     Класс-контроллер для управления операциями по "Аренде оборудования" клиента
	/// </summary>
	public class HardwareRentController : ControlPanelController
	{
		/// <summary>
		///     Отображает список оборудования для аренды
		/// </summary>
		public ActionResult HardwareList(int id)
		{
			var client = DbSession.Get<Client>(id);
			var rentalHardwareList = DbSession.Query<RentalHardware>().ToList();
			ViewBag.Client = client;
			ViewBag.RentalHardwareList = rentalHardwareList;
			return View();
		}

		/// <summary>
		///     Отображает список оборудования, когда либо арендованного клиентом
		/// </summary>
		public ActionResult HardwareArchiveList(int id)
		{
			var client = DbSession.Get<Client>(id);
			var rentalHardwareList = DbSession.Query<RentalHardware>().ToList();
			ViewBag.Client = client;
			ViewBag.RentalHardwareList = rentalHardwareList;
			return View();
		}

		/// <summary>
		///     Создание новой аренды оборудования
		/// </summary>
		public ActionResult CreateHardwareRent(int id, uint hardware)
		{
			var rentalHardware = DbSession.Query<RentalHardware>().FirstOrDefault(i => i.Id == hardware);
			var client = DbSession.Get<Client>(id);

			if (rentalHardware == null) {
				ErrorMessage("Невозможно арендовать данное оборудование!");
				return RedirectToAction("HardwareList", new {id});
			}
			var clientHardware =
				new ClientRentalHardware
				{
					Hardware = rentalHardware,
					Client = client,
					GiveDate = SystemTime.Now()
				};
			//формирование комплектации
			clientHardware.GetClientHardwareParts();

			ViewBag.ClientHardware = clientHardware;
			return View();
		}

		/// <summary>
		///     Создание новой аренды оборудования
		/// </summary>
		[HttpPost]
		public ActionResult CreateHardwareRent([EntityBinder] ClientRentalHardware clientRentalHardware)
		{
			var errors = ValidationRunner.Validate(clientRentalHardware);

			if (errors.Length > 0 || clientRentalHardware.Client == null || clientRentalHardware.Hardware == null) {
				if (errors.Length == 0)
					ErrorMessage("Невозможно активировать услугу!");
				ViewBag.ClientHardware = clientRentalHardware;
				return View("CreateHardwareRent");
			}
			var currentEmployee = GetCurrentEmployee();
			clientRentalHardware.Activate(DbSession, currentEmployee);

			clientRentalHardware.GetClientHardwareParts();
			DbSession.Save(clientRentalHardware);
			SuccessMessage("Услуга успешно активирована!");

			ViewBag.ClientHardware = clientRentalHardware;
			return RedirectToAction("HardwareList", new {@id = clientRentalHardware.Client.Id});
		}

		/// <summary>
		///     Обновление аренды оборудования
		/// </summary>
		public ActionResult UpdateHardwareRent(int id)
		{
			var clientRentalHardware = DbSession.Query<ClientRentalHardware>().FirstOrDefault(i => i.Id == id);
			if (clientRentalHardware == null) {
				return RedirectToAction("ClientList", "HardwareTypes");
			}
			clientRentalHardware.GetClientHardwareParts();
			ViewBag.ClientHardware = clientRentalHardware;
			return View();
		}

		/// <summary>
		///     Обновление аренды оборудования
		/// </summary>
		[HttpPost]
		public ActionResult UpdateHardwareRent([EntityBinder] ClientRentalHardware clientRentalHardware)
		{
			var errors = ValidationRunner.Validate(clientRentalHardware);
			if (errors.Count == 0) {
				clientRentalHardware.GetClientHardwareParts();
				DbSession.Update(clientRentalHardware);
				SuccessMessage("Услуга успешно обновлена!");
				ViewBag.ClientHardware = clientRentalHardware;
				return View();
			}
			ViewBag.ClientHardware = clientRentalHardware;
			return View();
		}

		/// <summary>
		///     Деактивирует последнюю аренду клиента
		/// </summary>
		[HttpPost]
		public ActionResult DiactivateHardwareRent(int id, uint hardware, string deactivateReason)
		{
			var rentalHardware = DbSession.Query<RentalHardware>().First(i => i.Id == hardware);
			var client = DbSession.Get<Client>(id);

			// Обработка случая с деактивацией аренды
			if (client.HardwareIsRented(rentalHardware)) {
				var thisHardware = client.GetActiveRentalHardware(rentalHardware);

				if (thisHardware == null || thisHardware.Client == null)
					ErrorMessage("Невозможно деактивировать услугу!");
				else {
					//сохранение комментария
					thisHardware.DeactivateComment = deactivateReason;
					//валидация
					var errors = ValidationRunner.Validate(thisHardware);
					// если нет ошибок
					if (errors.Count == 0) {
						//деактивация аренды
						var currentEmployee = GetCurrentEmployee();
						thisHardware.Deactivate(DbSession, currentEmployee);
						thisHardware.DeactivateCommentSend(DbSession, currentEmployee);
						SuccessMessage("Услуга успешно деактивирована!");
					}
					else {
						ViewBag.ClientHardware = thisHardware;
						return View("UpdateHardwareRent");
					}
				}
			}
			return RedirectToAction("HardwareList", new {@id = client.Id});
		}

		/// <summary>
		///     Обновление аренды оборудования клиента после деактивации
		/// </summary>
		[HttpPost]
		public ActionResult DiactivatedHardwareUpdate([EntityBinder] ClientRentalHardware clientRentalHardware)
		{
			var errors = ValidationRunner.Validate(clientRentalHardware, clientRentalHardware.GetErrors());
			if (errors.Count == 0) {
				clientRentalHardware.GetClientHardwareParts();
				DbSession.Update(clientRentalHardware);
				clientRentalHardware.DeactivateRentUpdateAppeal(DbSession, GetCurrentEmployee());
			}
			ViewBag.ClientHardware = clientRentalHardware;
			return RedirectToAction("HardwareArchiveList", new {@id = clientRentalHardware.Client.Id});
		}
	}
}