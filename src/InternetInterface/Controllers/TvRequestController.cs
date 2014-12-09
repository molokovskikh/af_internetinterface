using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NPOI.SS.Formula.Functions;

namespace InternetInterface.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class TvRequestController : InternetInterfaceController
	{
		public void New(uint clientId)
		{
			var client = DbSession.Get<Client>(clientId);
			var request = new TvRequest(client);
			PropertyBag["client"] = client;
			PropertyBag["request"] = request;
			var contactTypes = new List<ContactType>();
			contactTypes.Add(ContactType.HeadPhone);
			contactTypes.Add(ContactType.MobilePhone);
			contactTypes.Add(ContactType.HousePhone);
			contactTypes.Add(ContactType.ConnectedPhone);
			PropertyBag["contacts"] = client.Contacts.Where(c => contactTypes.Contains(c.Type)).ToList();
		}

		public void New(uint clientId, [ARDataBind("request", AutoLoad = AutoLoadBehavior.NewRootInstanceIfInvalidKey)] TvRequest request)
		{
			request.Partner = Partner;

			var flag = request.Hdmi ? "да" : "нет";
			var contact = request.Contact != null ? request.Contact.HumanableNumber : request.AdditionalContact;
			var body = "Соотрудник, создавший заявку: " + request.Partner.Name + "\n";
			body += "HDMI: " + flag + "\n";
			body += "Контакт: " + contact + "\n";
			body += "Комментарий: \n" + request.Comment + "\n";
			var title = " Заявка на ТВ. ЛС: " + request.Client.Id + ", " + request.Client.Name;
			var issue = new RedmineIssue(title, body);
			issue.assigned_to_id = 279; //Группа координаторы
			issue.project_id =  67; //Координация
			issue.due_date = request.Date.AddDays(3);

			var errors = ValidateDeep(request);
			errors.RegisterErrorsFrom(ValidateDeep(issue));
			if (errors.ErrorsCount == 0) {
				DbSession.Save(request);
				//Отправляем заявку в redmine
				DbSession.Save(issue);
				issue.root_id = (int)issue.Id;
				DbSession.Save(issue);
				//Пишем коммент пользователю
				var appeal = request.Client.CreareAppeal("Создана заявка на подключение ТВ №"+request.Id+", задача #"+issue.Id);
				DbSession.Save(appeal);
				

				Notify("Заявка успешно создана");
				RedirectTo(request.Client);
				return;
			}
			New(clientId);
			PropertyBag["request"] = request;
		}
	}
}