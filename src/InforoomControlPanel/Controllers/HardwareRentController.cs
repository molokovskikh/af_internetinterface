using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Класс-контроллер для управления операциями по "Аренде оборудования" клиента
	/// </summary>
	public class HardwareRentController : ControlPanelController
	{
		/// <summary>
		/// Отображает список оборудования для аренды
		/// </summary>
		public ActionResult HardwareList(int id)
		{
			var client = DbSession.Get<Client>(id);
			ViewBag.Client = client;
			var rentIsActive = new bool[(int)HardwareType.Count];
			rentIsActive[(int)HardwareType.TvBox] = client.HardwareIsRented(HardwareType.TvBox);
			for (var i = HardwareType.Switch; i < HardwareType.Count; i++) {
				rentIsActive[(int)i] = client.HardwareIsRented(i);
			}
			ViewBag.ActiveRent = rentIsActive;
			return View();
		}

		/// <summary>
		/// Отображает инф-цию об арендуемом оборудовании либо деактивирует данную аренду
		/// </summary>
		public ActionResult UpdateHardwareRent(int id, HardwareType hardware)
		{
			var client = DbSession.Get<Client>(id);
			// Обработка случая с деактивацией аренды
			if (client.HardwareIsRented(hardware)) {
				var thisHardware = client.GetActiveRentalHardware(hardware);
				if (thisHardware == null || thisHardware.Client == null)
					ErrorMessage("Невозможно деактивировать услугу!");
				else {
					var msg = thisHardware.Deactivate();
					DbSession.Update(thisHardware);
					var appeal = new Appeal(msg, thisHardware.Client, AppealType.User) {
						Employee = GetCurrentEmployee()
					};
					DbSession.Save(appeal);
					SuccessMessage("Услуга успешно деактивирована!");
				}
				return RedirectToAction("HardwareList", new {@id = client.Id});
			}

			// Обработка случая с активацией аренды
			var rentHardware = DbSession.Query<RentalHardware>().ToList()
					.Where(rh => rh.Type == hardware).ToList().FirstOrDefault();
			if (rentHardware == null) {
				ErrorMessage("Невозможно арендовать данное оборудование!");
				return RedirectToAction("HardwareList", new {@id = client.Id});
			}
			var clientHardware = new ClientRentalHardware {
				Hardware = rentHardware,
				Client = client
			};
			ViewBag.ClientHardware = clientHardware;
			return View();
		}

		/// <summary>
		/// Создает новую аренду оборудования для клиента
		/// </summary>
		[HttpPost]
		public ActionResult SaveHwRentService([EntityBinder] ClientRentalHardware clientRentalHardware)
		{
			var errors = ValidationRunner.Validate(clientRentalHardware);
			if (errors.Length > 0 || clientRentalHardware.Client == null || clientRentalHardware.Hardware == null) {
				if (errors.Length == 0)
					ErrorMessage("Невозможно активировать услугу!");
				ViewBag.ClientHardware = clientRentalHardware;
				return View("UpdateHardwareRent");
			}

			var msg = clientRentalHardware.Activate(DbSession, GetCurrentEmployee());
			DbSession.Save(clientRentalHardware);
			// 1-ое обращение - об активации услуги
			var appeal = new Appeal(msg, clientRentalHardware.Client, AppealType.User) {
				Employee = GetCurrentEmployee()
			};
			DbSession.Save(appeal);
			// 2-ое обращение - об арендуемом оборудовании
			appeal = new Appeal(string.Format("Клиент арендовал \"{0}\", модель \"{1}\", S/N {2}", clientRentalHardware.Hardware.Name, 
				clientRentalHardware.Model.Name, clientRentalHardware.Model.SerialNumber), clientRentalHardware.Client, AppealType.User) {
				Employee = GetCurrentEmployee()
			};
			DbSession.Save(appeal);
			SuccessMessage("Услуга успешно активирована!");
			return RedirectToAction("HardwareList", new {@id = clientRentalHardware.Client.Id});
		}
	}
}
