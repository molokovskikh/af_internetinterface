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
			var listId = ComponentParams["listId"].ToString();
			var componentId = Convert.ToUInt32(ComponentParams["componentId"].ToString());
			var contactId = Convert.ToUInt32(ComponentParams["cintactId"].ToString());
			var thisContact = Contact.Queryable.Where(c=>c.Id == contactId).FirstOrDefault(); //Contact.Find(contactId);
			var htmlCode = string.Format("<select id={1} class=\"linkSelector\" name=\"contact[{0}].Type\" >", componentId, listId);
			foreach (var type in Enum.GetValues(typeof (ContactType))) {
				htmlCode += string.Format("<option class={3} value=\"{0}\" {2}>{1}</option>", type,
				                          Contact.GetReadbleCategorie((ContactType) type),
				                          thisContact != null && thisContact.Type == (ContactType) type ? "selected" : string.Empty,
				                          (int) type == (int) ContactType.Email ? "emailOption" : "telephoneOption");
			}
			htmlCode += "</select>";
			RenderText(htmlCode);
		}
	}
}