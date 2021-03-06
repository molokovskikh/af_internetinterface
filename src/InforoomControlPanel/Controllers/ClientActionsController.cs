﻿using System;
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
			var message = client.Message;
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
			var errors = ValidationRunner.Validate(privateMessage);
			if (errors.Length > 0) {
				ViewBag.Client = privateMessage.Client;
				ViewBag.PrivateMessage = privateMessage;
				return View();
			}
			var client = privateMessage.Client;
            privateMessage.RegDate = SystemTime.Now();
			privateMessage.Registrator = GetCurrentEmployee();
			if (client.Message == null || privateMessage.Client.Message.Id == 0 || client.Message.Id == privateMessage.Id)
			{
				client.Message = privateMessage;
				DbSession.SaveOrUpdate(privateMessage);
			}
			else {
				if (client.Message.Id != privateMessage.Id) {
					client.Message.Enabled = privateMessage.Enabled;
					client.Message.EndDate = privateMessage.EndDate;
					client.Message.RegDate = privateMessage.RegDate;
					client.Message.Registrator = privateMessage.Registrator;
					client.Message.Text = privateMessage.Text;
					DbSession.SaveOrUpdate(client.Message);
				}
            }
			SuccessMessage("Приватное сообщение успешно сохранено!");
			return RedirectToAction("InfoPhysical", "Client", new {id = privateMessage.Client.Id});
		}
	}
}
