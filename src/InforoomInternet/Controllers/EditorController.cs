using System.Collections.Generic;
using System.Linq;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using InforoomInternet.Models;
using InternetInterface.Helpers;

namespace InforoomInternet.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	[Filter(ExecuteWhen.BeforeAction, typeof(EditAccessFilter))]
	public class EditorController : BaseEditorController
	{
		public override IEnumerable<string> SpecialLinks
		{
			get
			{
				return new List<string> {
					"",
					"Main/zayavka",
					"Main/OfferContract",
					"PrivateOffice/IndexOffice",
					"PrivateOffice/Services",
					"PrivateOffice/SmsNotification",
					"PrivateOffice/AboutSale",
					"Main/Index",
					"Main/Feedback"
				};
			}
		}
	}
}