using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.NHibernateExtentions;
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

		public void New()
		{
			var request = new Request();
			PropertyBag["request"] = request;
			var partners = DbSession.Query<Partner>().ToList();
			var myPartner = partners.Where(p => p.Login == AuthenticationFilter.GetLoginFromCookie(Context)).ElementAt(0);
			PropertyBag["myPartner"] = (myPartner == null) ? 0 : myPartner.Id;
			PropertyBag["partnersList"] = partners;
			if (IsPost) {
				SetARDataBinder(AutoLoadBehavior.NullIfInvalidKey);
				BindObjectInstance(request, "request");
				if (IsValid(request)) {
					request.PreInsert();
					DbSession.Save(request);
					Notify("Сохранено");
					RedirectToAction("RequestOne", new { request.Id });
				}
			}
		}

		public void RequestOne(uint id)
		{
			var request = DbSession.Load<Request>(id);
			PropertyBag["Request"] = request;
			if (request.RequestSource == RequestType.FromClient || request.RequestSource == RequestType.FromOperator)
				PropertyBag["reqSourceDesc"] = request.RequestSource.GetDescription();
			else
				PropertyBag["reqSourceDesc"] = "";
			PropertyBag["Messages"] = DbSession.Query<RequestMessage>().Where(r => r.Request.Id == id).ToList();
		}

		[return: JSONReturnBinder]
		public object StreetAutoComplete(string term)
		{
			if (string.IsNullOrEmpty(term))
				return new object[0];

			if (Request == null || Request.Headers["X-Requested-With"] != "XMLHttpRequest")
				return new object[0];

			var subs = term.Split(' ');

			return subs.SelectMany(s => DbSession.Query<Street>().Where(x => x.Name.Contains(s)))
				.Distinct()
				.Select(s => s.Name)
				.ToList();
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
			PropertyBag["labelColors"] = Label.GetColors();
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
				DbSession.Delete(labelForDel);
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