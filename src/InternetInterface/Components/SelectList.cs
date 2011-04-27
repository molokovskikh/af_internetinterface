using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InternetInterface.Models;

namespace InternetInterface.Components
{
	public class SelectList : ViewComponent
	{
		public override void Render()
		{
			var htmlCode = string.Format("<select class=\"linkSelector\" name=\"speed\" id=\"{0}\">", "Speed");
			foreach (var packageSpeed in PackageSpeed.FindAll().OrderBy(o => o.Speed))
			{
				float mb = packageSpeed.Speed / 1000000.00f;
				string opName = mb >= 1 ? mb.ToString("#.00") + " Mb" : (mb*1000).ToString("#.00") + " Kb";
				htmlCode += string.Format("<option value=\"{0}\">{1}</option>", packageSpeed.Id, opName);
			}
			htmlCode += "</select>";
			RenderText(htmlCode);
		}
	}
}