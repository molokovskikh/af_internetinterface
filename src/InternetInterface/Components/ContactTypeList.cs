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
			var componentId = Convert.ToUInt32(ComponentParams["componentId"].ToString());
			var contactId = Convert.ToUInt32(ComponentParams["cintactId"].ToString());
			var thisContact = Contact.Find(contactId);
			var htmlCode = string.Format("<select class=\"linkSelector\" name=\"contact[{0}].Type\" >", componentId);
			foreach (var type in Enum.GetValues(typeof (ContactType))) {
				htmlCode += string.Format("<option value=\"{0}\" {2}>{1}</option>", type,
				                          Contact.GetReadbleCategorie((ContactType) type),
				                          thisContact.Type == (ContactType) type ? "selected" : string.Empty);
			}
			htmlCode += "</select>";
			RenderText(htmlCode);
		}
	}
}