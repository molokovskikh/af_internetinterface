using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InforoomInternet.Components;
using InforoomInternet.Logic;
using InforoomInternet.Models;
using InternetInterface.Helpers;

namespace InforoomInternet.Controllers
{
	[Layout("Main")]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class EditorController : SmartDispatcherController
	{
		public void Menu()
		{
			if (!LoginLogic.IsAccessiblePartner(Session["LoginPartner"]))
				RedirectToSiteRoot();
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
					if (type[i] == "main")
					{
						mainMenu = new MenuField
						           	{
						           		Name = name[i],
						           		Link = link[i]
						           	};
						mainMenu.SaveAndFlush();
					}
					if (type[i] == "sub")
					{
						new SubMenuField
							{
								Link = link[i],
								Name = name[i],
								MenuField = mainMenu //MenuField.Find(Convert.ToUInt32(id[i]))
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