using System;
using System.Linq;
using System.Web.Mvc;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	public class ClientActionsController : ControlPanelController
	{
		/// <summary>
		/// Отображает форму для редактирования "Приватного сообщения"
		/// </summary>
		public ActionResult PrivateMessage(int id)
		{
			var client = DbSession.Get<Client>(id);
			ViewBag.Client = client;
			var message = DbSession.Query<PrivateMessage>().FirstOrDefault(pm => pm.Client.Id == id);
			ViewBag.PrivateMessage = message ?? new PrivateMessage {
				Client = client,
				EndDate = SystemTime.Today().AddDays(1)
			};
			return View();
		}

		/// <summary>
		/// Сохраняет данные по "Приватному сообщению" из формы
		/// </summary>
		[HttpPost]
		public ActionResult PrivateMessage([EntityBinder] PrivateMessage privateMessage)
		{
			if (privateMessage == null) {
				ErrorMessage("Ошибка! Невозможно сохранить сообщение.");
				return RedirectToAction("List", "Client");
			}

			// Чтобы не сохранялась и не выводилась на экран дата "01.01.0001"
			if (privateMessage.EndDate == DateTime.MinValue)
				privateMessage.EndDate = SystemTime.Today().AddDays(1);
			var errors = ValidationRunner.ValidateDeep(privateMessage);
			if (errors.Length > 0) {
				ViewBag.Client = privateMessage.Client;
				ViewBag.PrivateMessage = privateMessage;
				return View();
			}

			privateMessage.RegDate = SystemTime.Now();
			privateMessage.Registrator = GetCurrentEmployee();
			DbSession.SaveOrUpdate(privateMessage);
			SuccessMessage("Приватное сообщение успешно сохранено!");
			return RedirectToAction("InfoPhysical", "Client", new {id = privateMessage.Client.Id});
		}
	}
}
