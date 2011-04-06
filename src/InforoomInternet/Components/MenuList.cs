using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InforoomInternet.Models;

namespace InforoomInternet.Components
{
	public class MenuList : ViewComponent
	{
		public override void Render()
		{
			/*if (ComponentParams["MenuListItems"] == null)
				throw new Exception("Элементы для заполнения легенды не заданы. Параметер MenuListItems пуст.");*/
			var menu = MenuField.FindAll();//(IList<MenuField>)ComponentParams["MenuListItems"];
			var blockMenu = string.Empty;
			var mitemFirst = true;
			foreach (var menuField in menu)
			{
				//var active = menuField.subMenu.Count > 0 ? "active" : string.Empty;
				var active = mitemFirst ? "active" : string.Empty;
				var id = Path.GetFileNameWithoutExtension(menuField.Link.Split(new[] { '\\' }).Last());
				blockMenu += string.Format("<li class=\"main {2}\">" +
						   "<a href=\"{0}\" id=\"{3}\" class=\"menu-item one-line {2}\">{1}</a><br />", EngineContext.UrlInfo.AppVirtualDir + "/" + menuField.Link,
						   menuField.Name, active, id);
				if (menuField.subMenu.Count != 0 || mitemFirst)
				{
					blockMenu += mitemFirst ? "<ul class=\"sub\">" : "<ul style=\"background:none\" class=\"sub\">";
					foreach (var subField in menuField.subMenu)
					{
						blockMenu += string.Format("<li><a href=\"{0}\" class=\"submenu-item\">{1}</a></li>", subField.Link, subField.Name);
					}
					for (int i = 0; i < 2 - menuField.subMenu.Count; i++)
					{
						//blockMenu += "<li><a href=\"#\" class=\"submenu-item\">&nbsp;</a></li>";
						blockMenu += "<br />";
					}
					blockMenu += "</ul>";
				}
				blockMenu += "</li>";
				mitemFirst = false;
			}
			RenderText(blockMenu);
		}
	}

	public class EditMenuList : ViewComponent
	{
		public override void Render()
		{
			var htmlCode = string.Empty;
			var menu = MenuField.FindAll();
			foreach (var menuField in menu)
			{
				htmlCode += string.Format("<Div class=\"menuBlock\" id=\"{0}\">", "menuBlock_" + menuField.Id);
				var mainInput = "<input type=text name=\"fieldName\" id=\"{1}\" value=\"{0}\" class=\"mitem\"/>";
				htmlCode += string.Format(mainInput, menuField.Name, "n_" + menuField.Id);
				htmlCode += "<div class=\"addSubMenuItem\"> </div>";
				htmlCode += "<div class=\"delMenuItem\"> </div>";
				htmlCode += string.Format(mainInput, menuField.Link, "l_" + menuField.Id);
				htmlCode += "<br />";
				/*htmlCode += menuField.subMenu.Count > 0
				            	? string.Format("<div class=\"subDiv\">")
				            	: string.Empty;*/
				htmlCode += "<div class=\"subDiv\">";
				foreach (var field in menuField.subMenu)
				{
					htmlCode += "<div class=\"delSubMenu\">";
					var subIntem =
						"<input type=text name=\"fieldName\" id=\"{1}\" value=\"{0}\" class=\"subitem\"/>";
					htmlCode += string.Format(subIntem, field.Name, "sn_" + field.Id);
					htmlCode += string.Format(subIntem, field.Link, "sl_" + field.Id);
					htmlCode += "<div class=\"delSubMenuItem\"> </div>";
					htmlCode += " <br />";
					htmlCode += "</div>";
				}
				htmlCode += menuField.subMenu.Count > 0
								? string.Format(
								"<div class=\"{0}\"></div>", "appendDiv" + menuField.Id) : string.Empty;
				//htmlCode += menuField.subMenu.Count > 0 ? "</div>" : string.Empty;
				htmlCode += "</div>";
				htmlCode += "</div>";
			}
			RenderText(htmlCode);
		}
	}
}