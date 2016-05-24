using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.Mvc;
using Common.Tools;
using Common.Tools.Calendar;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Models;
using InforoomControlPanel.Controllers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Transform;
using Remotion.Linq.Clauses;

namespace InforoomControlPanel.ReportTemplates
{
	public class LeaseReport
	{
		public static List<Internetsessionslog> GetGeneralReport(BaseController controller,
			FilterReport<Internetsessionslog> pager,
			ISession dbSession, Client client)
		{
			var objList = new List<Internetsessionslog>();

			var items = 0;

			var dateA = DateTime.MinValue;
			var dateB = DateTime.MaxValue;
			var itemsPerPage = 100;
			var pageNumber = 0;

			if (string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.LeaseBegin")) &&
			    string.IsNullOrEmpty(pager.GetParam("filter.LowerOrEqual.LeaseBegin"))) {
				pager.ParamDelete("filter.GreaterOrEqueal.LeaseBegin");
				pager.ParamDelete("filter.LowerOrEqual.LeaseBegin");
				pager.ParamSet("filter.GreaterOrEqueal.LeaseBegin", SystemTime.Now().Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
				pager.ParamSet("filter.LowerOrEqual.LeaseBegin", SystemTime.Now().Date.ToString("dd.MM.yyyy"));
			}
			int.TryParse(pager.GetParam("itemsPerPage"), out itemsPerPage);
			int.TryParse(pager.GetParam("page"), out pageNumber);
			DateTime.TryParse(pager.GetParam("filter.GreaterOrEqueal.LeaseBegin"), out dateA);
			DateTime.TryParse(pager.GetParam("filter.LowerOrEqual.LeaseBegin"), out dateB);
			dateB = dateB.AddHours(23).AddMinutes(59).AddSeconds(59);
			var endPointList = client.Endpoints.Select(s => s.Id).ToList();
			pager.ParamSet("Id", client.Id.ToString());
			var endpointsFromLogRaw =
				dbSession.Query<ClientEndpointLog>()
					.Where(
						s => s.Client != null && s.Client.Value == client.Id && s.ClientendpointId != null && s.ClientendpointId != 0)
					.ToList();
			var endpointsFromLog = endpointsFromLogRaw.Select(s => s.ClientendpointId.Value).ToList();

			endpointsFromLog.Each(s => {
				if (!endPointList.Any(d => d == s)) {
					endPointList.Add(s);
				}
			});
			endPointList.Each(s => {
				objList.AddRange(
					dbSession.Query<Internetsessionslog>()
						.Where(d => d.EndpointId != null && d.EndpointId.Value == s && d.LeaseBegin >= dateA && d.LeaseBegin <= dateB)
						.ToList());
			});


			var appeals =
				client.Appeals.Where(s => s.AppealType == AppealType.Statistic && s.Date >= dateA && s.Date <= dateB).ToList();
			if (appeals.Count > 0) {
				objList.AddEach(appeals.Select(s => new Internetsessionslog() {
					Id = s.Id,
					IP = "<b>Событие</b>",
					HwId = "<span style='color:#860202;'>" + s.Message + "</span>",
					LeaseBegin = s.Date
				}).ToList());
			}
			var currenrLeaseListRaw = client.Endpoints.SelectMany(s => s.LeaseList.Select(l => l).ToList()).ToList();

			foreach (var lease in currenrLeaseListRaw) {
				var currentLease = objList.FirstOrDefault(s => s.EndpointId!=null && s.GetIpString() !=null
				&& s.GetIpString().ToString() == lease.Ip.ToString() && s.HwId.IndexOf(lease.Mac) != -1 && s.EndpointId == lease.Endpoint.Id && s.LeaseBegin == lease.LeaseBegin);
				if (currentLease != null) {
					currentLease.IP = $"<span class='blue bold'>{lease.Ip.ToString()}</span>";
					currentLease.HwId = $"<span class='blue bold'>{lease.Mac}</span>";
					currentLease.LeaseBegin = lease.LeaseBegin;
					currentLease.LeaseEnd = lease.LeaseEnd;
				}
				else {
					objList.Add(new Internetsessionslog() {
						Id = lease.Id,
						EndpointId = lease.Endpoint.Id,
						IP = $"<span class='blue bold'>{lease.Ip.ToString()}</span>",
						HwId = $"<span class='blue bold'>{lease.Mac}</span>",
						LeaseBegin = lease.LeaseBegin,
						LeaseEnd = lease.LeaseEnd
					});
				}
			}
			var controllerBase = (ControlPanelController)controller;
			objList = objList.OrderByDescending(s => s.LeaseBegin).ThenByDescending(s => s.Id).ThenByDescending(s=>s.LeaseEnd).ToList();
			items = objList.Count;
			objList = objList.Skip(itemsPerPage * (pageNumber - 1)).Take(itemsPerPage).ToList();
			pager.SetTotalItems(items);
			controllerBase.PreventSessionUpdate();
			return objList;
		}
	}
}