﻿using System.Collections.Generic;
using System.Linq;
using Castle.MonoRail.Framework;
using InforoomInternet.Logic;
using InforoomInternet.Models;
using InternetInterface.Helpers;

namespace InforoomInternet.Controllers
{
	[Layout("Main")]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	public class EditorController : SmartDispatcherController
	{
		public static IEnumerable<string> SpecialLinks
		{
			get
			{
				return new List<string>{
					"",
					"Main/zayavka",
					"Main/OfferContract",
					"PrivateOffice/IndexOffice",
					"Main/Index"
					//"Main/requisite"
				};
			}
		}

		public void Menu()
		{
			if (!LoginLogic.IsAccessiblePartner(Session["LoginPartner"]))
				//RedirectToSiteRoot();
				Redirecter.RedirectRoot(Context, this);
		}

		[return: JSONReturnBinder]
		public string FreeLinks(object obj)
		{
			if (LoginLogic.IsAccessiblePartner(Session["LoginPartner"]))
			{
				var link = Request.Form["newLink"];
				new IVRNContent
				{
					Content = string.Empty,
					ViewName = link
				}.Save();
				return  string.Format("Ссылка {0} добавлена", link);
			}
			return "Не достаточно прав доступа";
		}

		[return: JSONReturnBinder]
		public string DelLink(object obj)
		{
			if (LoginLogic.IsAccessiblePartner(Session["LoginPartner"]))
			{
				var link = Request.Form["delLink"];
				var items = IVRNContent.Queryable.Where(i => i.ViewName == link).ToList();
				foreach (var ivrnContent in items)
				{
					ivrnContent.Delete();
				}
				return string.Format("Ссылка {0} удалена", link);
			}
			return "Не достаточно прав доступа";
		}

		[return: JSONReturnBinder]
		public string Save(object obj)
		{
			if (LoginLogic.IsAccessiblePartner(Session["LoginPartner"]))
			{
				ARSesssionHelper<MenuField>.QueryWithSession(session =>
																{
																	var query =
																		session.CreateSQLQuery(
																			@"delete from internet.menufield; delete from internet.submenufield;")
																			.AddEntity(
																				typeof (MenuField));
																	query.ExecuteUpdate();
																	return new List<MenuField>();
																});
				var id = Request.Form["id[]"].Split(new[] {','});
				var name = Request.Form["name[]"].Split(new[] {','});
				var link = Request.Form["link[]"].Split(new[] {','});
				var type = Request.Form["fieldType[]"].Split(new[] {','});
				var elCount = name.Length;
				var mainMenu = new MenuField();
				for (int i = 0; i < elCount; i++)
				{
					//var subLink = IVRNContent.Queryable.Where(p => p.ViewName == Path.GetFileNameWithoutExtension(link[i])).Count() > 0  
					//var subLink =  SpecialLinks.Where(t => t == link[i]).Count() > 0 || link[i].Contains("Content/") ? string.Empty : "Content/";
					var subLink = link[i] == "Content" ? "/" + name[i] : string.Empty;
					if (link[i] == "Content" && IVRNContent.Queryable.Count(iv => iv.ViewName == name[i]) == 0)
						new IVRNContent {
						                	Content = string.Empty,
						                	ViewName = name[i]
						                }.SaveAndFlush();
					if (type[i] == "main")
					{
						mainMenu = new MenuField
									{
										Name = name[i],
										Link = string.Format(link[i] + "{0}", subLink)
									};
						mainMenu.SaveAndFlush();
					}
					if (type[i] == "sub")
					{
						new SubMenuField
							{
								//Link = link[i],
								Link = string.Format(link[i] + "{0}", subLink),
								Name = name[i],
								MenuField = mainMenu
							}.SaveAndFlush();
					}
				}
				//Response.Write("Закончено");
				return "Сохранено";
			}
			//RedirectToUrl("../Editor/Menu");
			return "Не достаточно прав доступа";
		}
	}
}