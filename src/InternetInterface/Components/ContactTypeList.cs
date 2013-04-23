using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InternetInterface.Models;

namespace InternetInterface.Components
{
	public class ContactTypeList : ViewComponent
	{
		public override void Render()
		{
			var id = ComponentParams["id"];
			var idAttr = "";
			if (id != null)
				idAttr = String.Format("id=\"{0}\"", id);

			var name = ComponentParams["name"].ToString();
			var thisContact = ComponentParams["contact"] as Contact;
			var htmlCode = string.Format("<select {1} class=\"linkSelector\" name=\"{0}.Type\" >", name, idAttr);
			foreach (var type in Enum.GetValues(typeof(ContactType))) {
				htmlCode += string.Format("<option class={3} value=\"{0}\" {2}>{1}</option>", type,
					Contact.GetReadbleCategorie((ContactType)type),
					thisContact != null && thisContact.Type == (ContactType)type ? "selected" : string.Empty,
					(int)type == (int)ContactType.Email ? "emailOption" : "telephoneOption");
			}
			htmlCode += "</select>";
			RenderText(htmlCode);
		}
	}
}