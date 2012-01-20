using System.Collections.Generic;
using System.Linq;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using InforoomInternet.Logic;
using InforoomInternet.Models;
using InternetInterface.Helpers;

namespace InforoomInternet.Controllers
{
	[Layout("Main")]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof (BeforeFilter))]
	[Filter(ExecuteWhen.BeforeAction, typeof (NHibernateFilter))]
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
					"Main/Index",
					"Main/Feedback"
				};
			}
		}
	}
}