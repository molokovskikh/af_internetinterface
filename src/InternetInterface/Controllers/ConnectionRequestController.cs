using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MonoRail.Framework;
using Common.MySql;
using Common.Web.Ui.Helpers;
using InternetInterface.AllLogic;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Queries;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	[Helper(typeof(TextHelper))]
	[Helper(typeof(PaginatorHelper))]
	public class ConnectionRequestController : InternetInterfaceController
	{
		public void Index([DataBind("filter")] RequestFilter filter)
		{
			var requests = filter.Find(DbSession);
			PropertyBag["Clients"] = InitializeContent.Partner.Role.ReductionName == "Agent"
				? requests.Where(r => r.Registrator == InitializeContent.Partner).ToList()
				: requests.ToList();
			PropertyBag["filter"] = filter;
			PropertyBag["Direction"] = filter.Direction;
			PropertyBag["SortBy"] = filter.SortBy;
			SendRequestEditParameter();
		}

		public void RequestOne(uint id)
		{
			PropertyBag["Request"] = DbSession.Load<Request>(id);
			PropertyBag["Messages"] = DbSession.Query<RequestMessage>().Where(r => r.Request.Id == id).ToList();
		}

		public void CreateRequestComment(uint requestId, string comment)
		{
			if (!string.IsNullOrEmpty(comment)) {
				var message = new RequestMessage {
					Date = DateTime.Now,
					Registrator = InitializeContent.Partner,
					Comment = comment,
					Request = DbSession.Load<Request>(requestId)
				};
				DbSession.Save(message);
			}
			RedirectToReferrer();
		}

		private void SendRequestEditParameter()
		{
			PropertyBag["labelColors"] = ColorWork.GetColorSet();
			PropertyBag["LabelName"] = string.Empty;
			PropertyBag["Labels"] = DbSession.Query<Label>().OrderBy(l => l.Name).ToList();
		}

		public void EditLabel(uint deletelabelch, string labelName, string labelcolor)
		{
			var labelForEdit = DbSession.Load<Label>(deletelabelch);
			if (labelForEdit != null && labelForEdit.Deleted) {
				if (labelName != null)
					labelForEdit.Name = labelName;
				if (labelcolor != "#000000") {
					labelForEdit.Color = labelcolor;
				}
				DbSession.Save(labelForEdit);
			}
			RedirectToAction("Index");
		}

		public void DeleteLabel(uint deletelabelch)
		{
			var labelForDel = DbSession.Load<Label>(deletelabelch);
			if (labelForDel != null && labelForDel.Deleted) {
				labelForDel.DeleteAndFlush();
				DbSession.CreateSQLQuery(
					@"update internet.Requests R
set r.`Label` = null,
r.`ActionDate` = :ActDate,
r.`Operator` = :Oper
where r.`Label`= :LabelIndex;")
					.SetParameter("LabelIndex", deletelabelch)
					.SetParameter("ActDate", DateTime.Now)
					.SetParameter("Oper", InitializeContent.Partner.Id)
					.ExecuteUpdate();
			}
			RedirectToAction("Index");
		}

		public void RequestInArchive(uint id, bool action)
		{
			var request = DbSession.Load<Request>(id);
			request.Archive = action;
			DbSession.Save(request);
			RedirectToReferrer();
		}

		/// <summary>
		/// Создать новую метку
		/// </summary>
		public void CreateLabel(string labelName, string labelcolor)
		{
			if (!string.IsNullOrEmpty(labelName)) {
				DbSession.Save(new Label {
					Color = labelcolor,
					Name = labelName,
					Deleted = true
				});
			}
			Error("Нельзя создать метку без имени");
			RedirectToAction("Index");
		}

		/// <summary>
		/// Устанавливает метки на клиентов
		/// </summary>
		[AccessibleThrough(Verb.Post)]
		public void SetLabel([DataBind("LabelList")] List<uint> labelList, uint labelch)
		{
			var currentLabel = DbSession.Get<Label>(labelch);
			foreach (var label in labelList) {
				var request = DbSession.Load<Request>(label);
				if ((request.Label == null) ||
					(request.Label.ShortComment != "Refused" && request.Label.ShortComment != "Registered")) {
					request.Label = currentLabel;
					request.ActionDate = DateTime.Now;
					request.Operator = InitializeContent.Partner;
					if (currentLabel != null)
						if (currentLabel.ShortComment == "Refused" || currentLabel.ShortComment == "Deleted" || currentLabel.ShortComment == "Registered") {
							request.Archive = true;
						}
					DbSession.Save(request);
				}
			}
			RedirectToReferrer();
		}
	}
}