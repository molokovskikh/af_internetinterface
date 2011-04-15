using System;
using System.IO;
using System.Linq;
using System.Net;
using Castle.MonoRail.Framework;
using InforoomInternet.Logic;
using InforoomInternet.Models;
using InternetInterface.Models;
//using Tariff = InforoomInternet.Models.Tariff;

namespace InforoomInternet.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	[Filter(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class MainController : SmartDispatcherController
	{
		public void Index()
		{
			PropertyBag["tariffs"] = Tariff.FindAll();
		}

		public void Zayavka()
		{
			PropertyBag["tariffs"] = Tariff.FindAll();
		}

		public void Main()
		{}

		public void HowPay(bool edit)
		{
			SetEdatableAttribute(edit, "HowPay");
		}

		public void Ok()
		{}

		public void OfferContract(bool edit)
		{}

		/*public void requisite(bool edit)
		{
			//PropertyBag["Content"] = IVRNContent.FindAllByProperty("ViewName", "requisite").First().Content;
			SetEdatableAttribute(edit, "requisite");
			/*if (edit)
				LayoutName = "TinyMCE";*/
		//}

		public void PrivateOffice()
		{}


		public void Assist()
		{
			if (Request != null)
			if (Request.Headers["X-Requested-With"] != null)
			if (Request.Headers["X-Requested-With"].Equals("XMLHttpRequest"))
			{
				var queryString = Request.QueryString.Get("q");
				if ( !String.IsNullOrEmpty(queryString) )
				{
					Street.GetStreetList(queryString, Response.Output);
				}
				else
				{
					return;
				}
			}
			else
			{
				return;
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void Send([DataBind("application")] Request application )
		{
			if (application.IsValid())
			{
				application.ActionDate = DateTime.Now;
				application.Save();
				Flash["application"] = application;
				RedirectToAction("Ok");
			}
			else
			{
				var all = Tariff.FindAll();
				PropertyBag["tariffs"] = all;
				PropertyBag["application"] = application;
				RenderView("Zayavka");
			}
		}

		public void Warning()
		{
			var hostAdress = Request.UserHostAddress;
/*#if DEBUG
			hostAdress = "91.219.6.6";
#endif*/
			var lease = PhysicalClients.FindByIP(hostAdress);
#if DEBUG
			lease = new Lease {
				Endpoint = new ClientEndpoints {
					Client = new Clients {
						Disabled = false,
						PhisicalClient = new PhysicalClients {
							Balance = 100,
							Tariff = new Tariff {
								Price = 500
							}
						}
					}
				}
			};
#endif
			if (lease == null)
			{
				Redirecter.RedirectRoot(Context, this);
				//RedirectToSiteRoot();
				return;
			}
			if (lease.Endpoint.Client.PhisicalClient == null)
			{
				Redirecter.RedirectRoot(Context, this);
				//RedirectToSiteRoot();
				return;
			}
			var pclient = lease.Endpoint.Client.PhisicalClient;
			var client = lease.Endpoint.Client;

			if (IsPost)
			{
				SceHelper.Login(lease, lease.Endpoint, Request.UserHostAddress);
				var url = Request["referer"];
				if (String.IsNullOrEmpty(url))
					Redirecter.RedirectRoot(Context, this);
					//RedirectToSiteRoot();
				else
					RedirectToUrl(url);
			}
			var host = Request["host"];
			var rUrl = Request["url"];
			if (!string.IsNullOrEmpty(host))
				PropertyBag["referer"] = host + rUrl;
			else PropertyBag["referer"] = string.Empty;
			PropertyBag["PClient"] = pclient;
			PropertyBag["Client"] = client;
		}


		public void Save()
		{
			var localPath = Request.Form["LocalPath"];
			if (LoginLogic.IsAccessiblePartner(Session["LoginPartner"]))
			{
				var htmlcode = Request.Form["htmlcode"];
				var views = IVRNContent.FindAllByProperty("ViewName", localPath);
				if (views.Length == 0)
					new IVRNContent
						{
							Content = htmlcode,
							ViewName = localPath
						}.SaveAndFlush();
				else
				{
					var forEdit = views.First();
					forEdit.Content = htmlcode;
					forEdit.ViewName = localPath;
					forEdit.UpdateAndFlush();
				}
			}
			var url = localPath == "HowPay" ? string.Empty :  Context.ApplicationPath + "/Content/";
			RedirectToUrl(url + localPath);
		}

		private void SetEdatableAttribute(bool edit, string viewName)
		{
			PropertyBag["Content"] = IVRNContent.FindAllByProperty("ViewName", viewName).First().Content;
			if (edit)
			{
				LayoutName = "TinyMCE";
				PropertyBag["ShowEditLink"] = false;
			}
			else
			{
				PropertyBag["ShowEditLink"] = true;
			}
		}
	}
}