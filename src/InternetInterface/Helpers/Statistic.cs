using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Models;
using NHibernate;

namespace InternetInterface.Helpers
{
	public class Statistic
	{
		private ISession _session;

		public int OnLineCount { get; set; }
		public int UniqueClient { get; set; }
		public int AllRegister { get; set; }
		public int PhysicalRegister { get; set; }

		public int LawyerRegister
		{
			get { return AllRegister - PhysicalRegister; }
		}

		public int AllBlocked
		{
			get { return BlockedLawyer + BlockedPhysical; }
		}

		public int BlockedPhysical { get; set; }
		public int BlockedLawyer { get; set; }
		public int BlockedOnLine { get; set; }
		public int Dissolved { get; set; }

		public Statistic(ISession session)
		{
			_session = session;
		}

		public Statistic GetStatistic()
		{
			OnLineCount = _session.CreateSQLQuery(@"
SELECT count(*) FROM internet.leases l
join internet.ClientEndPoints cp on cp.id = l.Endpoint
group by cp.Client;")
				.List<object>().Count;

			UniqueClient = _session.CreateSQLQuery(@"
SELECT count(*) FROM logs.internetsessionslogs l
join internet.ClientEndPoints cp on cp.id = l.EndpointId
where l.LeaseBegin >= :beginInterval and l.LeaseBegin <= :endInterval
group by cp.Client;")
				.SetParameter("beginInterval", DateTime.Now.AddDays(-1))
				.SetParameter("endInterval", DateTime.Now)
				.List<object>().Count;

			PhysicalRegister = Convert.ToInt32(_session.CreateSQLQuery(@"
select count(*) from internet.Clients c
where c.PhysicalClient is not null;
")
				.UniqueResult());

			AllRegister = Convert.ToInt32(_session.CreateSQLQuery(@"
select count(*) from internet.Clients c;
")
				.UniqueResult());

			BlockedPhysical = Convert.ToInt32(_session.CreateSQLQuery(@"
select count(*) from internet.Clients c
where c.Disabled and c.PhysicalClient is not null and c.Status <> 10;
")
				.UniqueResult());

			BlockedLawyer = Convert.ToInt32(_session.CreateSQLQuery(@"
select count(*) from internet.Clients c
where c.Disabled and c.LawyerPerson is not null and c.Status <> 10;
")
				.UniqueResult());

			Dissolved = Convert.ToInt32(_session.CreateSQLQuery(@"
select count(*) from internet.Clients c
where c.Status = 10;
")
				.UniqueResult());

			BlockedOnLine = _session.CreateSQLQuery(@"
SELECT count(*) FROM internet.leases l
join internet.ClientEndPoints cp on cp.id = l.Endpoint
join internet.Clients c on c.id = cp.Client
where c.Disabled
group by cp.Client;")
				.List<object>().Count;
			return this;
		}
	}
}