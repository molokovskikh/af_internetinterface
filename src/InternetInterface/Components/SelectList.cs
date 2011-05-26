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
		    var client = Clients.Find(Convert.ToUInt32(ComponentParams["clientCode"].ToString()));
			var htmlCode = string.Format("<select class=\"linkSelector\" name=\"speed\" id=\"{0}\">", "Speed");
			foreach (var packageSpeed in PackageSpeed.FindAll().OrderBy(o => o.Speed))
			{
                if (packageSpeed != client.LawyerPerson.Speed)
				    htmlCode += string.Format("<option value=\"{0}\">{1}</option>", packageSpeed.Id, packageSpeed.GetNormalizeSpeed());
                else
                    htmlCode += string.Format("<option value=\"{0}\" selected>{1}</option>", packageSpeed.Id, packageSpeed.GetNormalizeSpeed());
			}
			htmlCode += "</select>";
			RenderText(htmlCode);
		}
	}
}