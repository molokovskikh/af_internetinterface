using System.Web.Mvc;

namespace Inforoom2.Controllers
{
	[Authorize]
	public class PersonalController : BaseController
	{
		public ActionResult Index()
		{
			ViewBag.Title = "Личный кабинет";
			return View();
		}

		public ActionResult ShowProfile()
		{
			ViewBag.Title = "Профиль";
			ViewBag.CurrentClient = CurrentClient;
			return PartialView();
		}

		public ActionResult Plans()
		{
			ViewBag.Title = "Тарифы";
			return PartialView();
		}

		public ActionResult Payment()
		{
			ViewBag.Title = "Платежи";
			return PartialView();
		}

		public ActionResult Credit()
		{
			ViewBag.Title = "Доверительный платеж";
			return PartialView();
		}

		public ActionResult UserDetails()
		{
			ViewBag.Title = "Данные пользователя";
			return PartialView();
		}

		public ActionResult Service()
		{
			ViewBag.Title = "Услуги";
			return PartialView();
		}

		public ActionResult Notifications()
		{
			ViewBag.Title = "Уведомления";
			return PartialView();
		}

		public ActionResult Bonus()
		{
			ViewBag.Title = "Бонусы";
			return PartialView();
		}
	}
}