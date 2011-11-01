using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Models.Editor;
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
		{
			if (Flash["application"] == null)
				RedirectToSiteRoot();
		}

		public void OfferContract(bool edit)
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
			CancelLayout();
			CancelView();
		}

		[AccessibleThrough(Verb.Post)]
		public void Send([DataBind("application")] Requests application )
		{
			if (Validator.IsValid(application))
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
			var lease = Client.FindByIP(hostAdress);
#if DEBUG
			lease = new Lease {
				Endpoint = new ClientEndpoints {
					Client = new Client {
						Disabled = false,
						ShowBalanceWarningPage = true,
						PhysicalClient = new PhysicalClients {
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

			if (IsPost)
			{
				SceHelper.Login(lease, Request.UserHostAddress);
				var client_w = lease.Endpoint.Client;
				client_w.ShowBalanceWarningPage = false;
				client_w.Update();
				var url = Request.Form["referer"];
				if (String.IsNullOrEmpty(url))
					Redirecter.RedirectRoot(Context, this);
					//RedirectToSiteRoot();
				else
					RedirectToUrl(string.Format("http://{0}", url));
				return;
			}

			var host = Request["host"];
			var rUrl = Request["url"];
			if (!string.IsNullOrEmpty(host))
				PropertyBag["referer"] = host + rUrl;
			else PropertyBag["referer"] = string.Empty;

			var pclient = lease.Endpoint.Client.PhysicalClient;
			var client = lease.Endpoint.Client;

			PropertyBag["Client"] = client;

			if (lease.Endpoint.Client.PhysicalClient == null)
			{
				PropertyBag["LClient"] = client.LawyerPerson;
				RenderView("WarningLawyer");
				return;
			}

			PropertyBag["PClient"] = pclient;
		}


		/*public void Save()
		{
			var localPath = Request.Form["LocalPath"];
			if (LoginLogic.IsAccessiblePartner(Session["LoginPartner"]))
			{
				var htmlcode = Request.Form["htmlcode"];
				var views = SiteContent.FindAllByProperty("ViewName", localPath);
				if (views.Length == 0)
					new SiteContent
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
		}*/

		private void SetEdatableAttribute(bool edit, string viewName)
		{
			PropertyBag["Content"] = SiteContent.FindAllByProperty("ViewName", viewName).First().Content;
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

		public void WarningPackageId()
		{
			var hostAdress = Request.UserHostAddress;
			var mailToAdress = "internet@ivrn.net";
			var messageText = new StringBuilder();
#if DEBUG
			hostAdress = NetworkSwitches.GetNormalIp(Lease.FindFirst().Ip);
			mailToAdress = "a.zolotarev@analit.net";
#endif
			var lease = Client.FindByIP(hostAdress);
			if (lease == null)
			{
				messageText.AppendLine(string.Format("Пришел запрос на страницу WarningPackageId от стороннего клиента (IP: {0})", hostAdress));
			}
			else
			{
				messageText.AppendLine(string.Format("Пришел запрос на страницу WarningPackageId от клиента {0}", lease.Endpoint.Client.Id.ToString("00000")));
				if (lease.Endpoint.Switch != null)
				{
					messageText.AppendLine("Свич: " + lease.Endpoint.Switch.Name);
				}
				else
				{
					messageText.AppendLine("Свич неопределен");
				}
				if (lease.Endpoint.Port != null)
				{
					messageText.AppendLine("Порт: " + lease.Endpoint.Port);
				}
				else
				{
					messageText.AppendLine("Порт неопределен");
				}
			}
			var message = new MailMessage();
			message.To.Add(mailToAdress);
			message.Subject = "Преадресация на страницу WarningPackageId";
			message.From = new MailAddress("service@analit.net");
			message.Body = messageText.ToString();
			var smtp = new SmtpClient("box.analit.net");
			smtp.Send(message);
		}
	}
}