using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Отображает страницы оферты
	/// </summary>
	public class AboutController : BaseController
	{
		public ActionResult Index()
		{
			return View();
		}

		public ActionResult Details()
		{
			return View();
		}

		public ActionResult Payment()
		{
			return View();
		}
	}
}