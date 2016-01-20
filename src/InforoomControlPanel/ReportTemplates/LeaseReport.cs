﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Common.Tools;
using Common.Tools.Calendar;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Models;
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

			endpointsFromLog.Each(s =>
			{
				if (!endPointList.Any(d => d == s)) {
					endPointList.Add(s);
				}
			});
			endPointList.Each(s =>
			{
				objList.AddRange(
					dbSession.Query<Internetsessionslog>()
						.Where(d => d.EndpointId.HasValue && d.EndpointId.Value == s && d.LeaseBegin >= dateA && d.LeaseBegin <= dateB)
						.ToList());
			});

			var appeals =
				client.Appeals.Where(s => s.AppealType == AppealType.Statistic && s.Date >= dateA && s.Date <= dateB).ToList();
			if (appeals.Count > 0) {
				objList.AddEach(appeals.Select(s => new Internetsessionslog()
				{
					Id = s.Id,
					IP = "<b>Событие</b>",
					HwId = "<span style='color:#860202;'>" + s.Message + "</span>",
					LeaseBegin = s.Date
				}).ToList());
			}

			objList = objList.OrderByDescending(s => s.LeaseBegin).ToList();
			items = objList.Count;
			objList = objList.Skip(itemsPerPage*(pageNumber-1)).Take(itemsPerPage).ToList();
			pager.SetTotalItems(items);

			return objList;
		}
	}
}