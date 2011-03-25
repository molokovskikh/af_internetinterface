using System;
using System.IO;
using System.Net;
using Castle.MonoRail.Framework;
using InforoomInternet.Models;
using InternetInterface.Models;
//using Tariff = InforoomInternet.Models.Tariff;

namespace InforoomInternet.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class MainController : SmartDispatcherController
	{
		public void Index()
		{
			var all = Tariff.FindAll();
			PropertyBag["tariffs"] = all;
		}

		public void Zayavka()
		{
			var all = Tariff.FindAll();
			PropertyBag["tariffs"] = all;
		}

		public void Main()
		{}

		public void HowPay()
		{}

		public void Ok()
		{}

		public void OfferContract()
		{}

		public void requisite()
		{}
		
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
			var lease = PhisicalClients.FindByIP(hostAdress);
#if DEBUG
			lease = new Lease {
				Endpoint = new ClientEndpoints {
					Client = new Clients {
						Disabled = false,
						PhisicalClient = new PhisicalClients {
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
				RedirectToSiteRoot();
				return;
			}
			if (lease.Endpoint.Client.PhisicalClient == null)
			{
				RedirectToSiteRoot();
				return;
			}
			var pclient = lease.Endpoint.Client.PhisicalClient;
			var client = lease.Endpoint.Client;

			if (IsPost)
			{
				SceHelper.Login(lease, lease.Endpoint, Request.UserHostAddress);
				var url = Request["referer"];
				if (String.IsNullOrEmpty(url))
					RedirectToSiteRoot();
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
	}
}