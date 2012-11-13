using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Queries
{
	public enum ClientState
	{
		[Description("Состоявшееся")]
		Connected,
		[Description("Несостоявшееся")]
		NoConnected,
		[Description("Все")]
		All
	}

	public class BrigadFilter : PaginableSortable
	{
		public string SearchText { get; set; }
		public Brigad Brigad { get; set; }
		public DatePeriod Period { get; set; }
		public ClientState State { get; set; }

		public BrigadFilter()
		{
			Period = new DatePeriod(DateTime.Now.AddDays(-7), DateTime.Now);
			State = ClientState.All;
		}

		public IList<ClientInfo> Find(ISession session)
		{
			var query = session.Query<ConnectGraph>();
			if (Brigad != null && Brigad.Id > 0)
				query = query.Where(c => c.Brigad.Id == Brigad.Id);
			if (Period != null)
				query = query.Where(c => c.Day >= Period.Begin && c.Day <= Period.End);
			if (State == ClientState.Connected)
				query = query.Where(c => c.Client.BeginWork != null);
			if (State == ClientState.NoConnected)
				query = query.Where(c => c.Client.BeginWork == null);
			query = query.Where(c => c.Client != null);

			RowsCount = query.Count();

			if (CurrentPage > 0) {
				var skiped = query.Skip(CurrentPage * PageSize);
				return skiped.Take(PageSize).ToList().Select(c => new ClientInfo(c.Client)).ToList();
			}
			else {
				return query.Take(PageSize).ToList().Select(c => new ClientInfo(c.Client)).ToList();
			}
		}
	}
}