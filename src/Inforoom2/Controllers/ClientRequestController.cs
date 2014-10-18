using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class ClientRequestController : BaseController
	{
		/// <summary>
		/// Отображает  форму новой заявки
		/// </summary>
		public ActionResult Index()
		{
			var clientRequest = new ClientRequest();
			ViewBag.ClientRequest = clientRequest;
			GetTariffs();
			return View();
		}

		private List<Tariff> GetTariffs()
		{
			var tariffs = DbSession.Query<Tariff>();
			List<SelectListItem> selectListItems =tariffs.Select(k => new SelectListItem {
				Value = k.Name,
				Text = k.Name
			}).ToList();
			ViewBag.Tariffs = selectListItems;
			return tariffs.ToList();
		}

		[HttpPost]
		public ActionResult Create(ClientRequest clientRequest)
		{
			var tariff = GetTariffs().FirstOrDefault(k => k.Name == clientRequest.Tariff.Name);
			clientRequest.Tariff = tariff; 
			var errors = ValidationRunner.ValidateDeep(clientRequest);
			if (errors.Length == 0) {
				DbSession.Save(clientRequest);
				SuccessMessage("Ваша заявка успешно зарегестрирована");
				return RedirectToAction("Index", "Home");
			}
			ViewBag.ClientRequest = clientRequest;
			return View("Index");
		}
	}
}