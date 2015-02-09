using System.Linq;
using System.Web.Mvc;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Отображает страницы оферты
	/// </summary>
	public class AboutController : Inforoom2Controller
	{
		/// <summary>
		/// Страница о компании
		/// </summary>
		public ActionResult Index()
		{
			return View();
		}

		/// <summary>
		/// Реквизиты компании
		/// </summary>
		public ActionResult Details()
		{
			return View();
		}

		/// <summary>
		/// Страница оплаты, если пользователь не авторизован
		/// </summary>
		public ActionResult Payment()
		{
			return View();
		}

		/// <summary>
		/// Списки подключенных домов
		/// </summary>
		public ActionResult ConnectedHousesLists()
		{
			var addresses = DbSession.Query<SwitchAddress>().Where(i=>i.House != null).OrderBy(i=>i.House.Street.Name).ToList();

			ViewBag.Addresses = addresses;
			return View();
		}
	}
}