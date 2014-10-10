using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Helpers.IpGeoBaseNET;
using Inforoom2.Models;
using Newtonsoft.Json;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class HomeController : BaseController
	{
		public ActionResult Index()
		{
			ViewBag.Message = "This can be viewed by anonymous";
			
			return View();
		}

		[Authorize(Roles = "admin")]
		public ActionResult AdminIndex()
		{
			ViewBag.Message = "This can be viewed only by users in Admin role only";
			return View();
		}
	}
}