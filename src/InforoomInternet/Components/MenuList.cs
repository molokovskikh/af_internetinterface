using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InforoomInternet.Controllers;
using InforoomInternet.Models;

namespace InforoomInternet.Components
{
	public class MenuList : ViewComponent
	{
		public override void Render()
		{
			var thisPath = EngineContext.UrlInfo.Controller + "/" + EngineContext.UrlInfo.Action;
			if (thisPath == @"Main/Index")
				thisPath = string.Empty;
			MenuField currentMenu = null;
			foreach (var menuField in MenuField.FindAll())
			{
				if (menuField.Link == thisPath)
					currentMenu = menuField;
			}
			if (currentMenu == null)
			{
				foreach (var subMenuField in SubMenuField.FindAll())
				{
					if (subMenuField.Link == thisPath)
						currentMenu = subMenuField.MenuField;
				}
			}
			/*if (ComponentParams["MenuListItems"] == null)
				throw new Exception("Элементы для заполнения легенды не заданы. Параметер MenuListItems пуст.");*/
			var menu = MenuField.FindAll();//(IList<MenuField>)ComponentParams["MenuListItems"];
			var blockMenu = string.Empty;
			var mitemFirst = true;
			foreach (var menuField in menu)
			{
				//var active = menuField.subMenu.Count > 0 ? "active" : string.Empty;
				var active = string.Empty;
				if (currentMenu != null)
				active = menuField.Link == currentMenu.Link ? "active" : string.Empty; 
				else
				active = mitemFirst ? "active" : string.Empty;
				var subMenDis = "none";
				if (!string.IsNullOrEmpty(active))
					subMenDis = "block";
				var id = Path.GetFileNameWithoutExtension(menuField.Link.Split(new[] { '\\' }).Last());
				blockMenu += string.Format("<li class=\"main {2}\">" +
						   "<a href=\"{0}\" id=\"{3}\" class=\"menu-item one-line {2}\">{1}</a><br />", EngineContext.ApplicationPath + "/" + menuField.Link,
						   menuField.Name, active, id);
				//if (menuField.subMenu.Count != 0 || mitemFirst)
				{
					blockMenu += mitemFirst ? "<ul  class=\"sub\">" : "<ul style=\"background:none\" class=\"sub\">";
					foreach (var subField in menuField.subMenu)
					{
						blockMenu += string.Format("<li><a href=\"{0}\" style=\"display:{2}\" class=\"submenu-item\">{1}</a></li>", EngineContext.ApplicationPath + "/" + subField.Link, subField.Name, subMenDis);
					}
					if (string.IsNullOrEmpty(active) && mitemFirst)
						blockMenu += "<br />" + "<br />"; else
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
		public string GenerateSelectList(string id, string selectedLink)
		{
			var select = string.Format("<select class=\"linkSelector\" name=\"tariff\" id=\"{0}\">", id);
			var selected = string.Empty;
			var selectedFlag = false;
			var nameIndex = 0;
			foreach (var item in MenuField.Queryable.Where(el => EditorController.SpecialLinks.Contains(el.Link)))
			{
				if (item.Link == selectedLink)
				{
					selected = "selected";
					selectedFlag = true;
				}
				else
					selected = string.Empty;
				select += string.Format("<option name=\"{3}\" value=\"{0}\" {2}>{1}</option>", item.Link, item.Name, selected, nameIndex);
				nameIndex++;
			}
			foreach (var item in EditorController.SpecialLinks.Where(t=>!MenuField.FindAll().Select(g => g.Link).Contains(t)))
			{
				select += string.Format("<option value=\"{0}\" >{1}</option>", item, item);
			}
			selected = string.IsNullOrEmpty(selected) && !selectedFlag ? "selected" : string.Empty;
			select += string.Format("<option  value=\"{0}\" {2}> {1} </option>", "Content", "Контентная сслыка", selected);
			select += "</select>";
			return select;
		}

		public override void Render()
		{
			var htmlCode = string.Empty;
			var menu = MenuField.FindAll();
			htmlCode += "<input type=text style=\"display:none\" class=\"subitem\"/>";
			foreach (var menuField in menu)
			{
				htmlCode += string.Format("<Div class=\"menuBlock\" id=\"{0}\">", "menuBlock_" + menuField.Id);
				var mainInput = "<input type=text name=\"fieldName\" id=\"{1}\" value=\"{0}\" class=\"mitem\"/>";
				htmlCode += string.Format(mainInput, menuField.Name, "n_" + menuField.Id);
				htmlCode += "<div class=\"addSubMenuItem\"> </div>";
				htmlCode += "<div class=\"delMenuItem\"> </div>";
				//htmlCode += string.Format(mainInput, menuField.Link, "l_" + menuField.Id);
				htmlCode += GenerateSelectList("l_" + menuField.Id, menuField.Link);
				htmlCode += "<br />";

				htmlCode += "<div class=\"subDiv\">";
				foreach (var field in menuField.subMenu)
				{
					htmlCode += "<div class=\"delSubMenu\">";
					var subIntem =
						"<input type=text name=\"fieldName\" id=\"{1}\" value=\"{0}\" class=\"subitem\"/>";
					htmlCode += string.Format(subIntem, field.Name, "sn_" + field.Id);
					htmlCode += GenerateSelectList("sl_" + field.Id, field.Link);
					//htmlCode += string.Format(subIntem, field.Link, "sl_" + field.Id);
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