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

		private uint _region;

		public int LawyerRegister { get; set; }

		public int AllBlocked
		{
			get { return BlockedLawyer + BlockedPhysical; }
		}

		public int BlockedPhysical { get; set; }
		public int BlockedLawyer { get; set; }
		public int BlockedOnLine { get; set; }
		public int Dissolved { get; set; }

		public Statistic(ISession session, uint region)
		{
			_session = session;
			_region = region;
		}

		private string SetRegion()
		{
			if (_region > 0)
				return string.Format(" and h.RegionId = {0} or lp.RegionId = {0}", _region);
			return string.Empty;
		}

		public Statistic GetStatistic()
		{
			var region = SetRegion();

			OnLineCount = _session.CreateSQLQuery(string.Format(@"
SELECT count(*) FROM internet.leases l
join internet.ClientEndPoints cp on cp.id = l.Endpoint
left join internet.Clients c on c.id = cp.client
left join internet.PhysicalClients pc on pc.id = c.PhysicalClient
left join internet.Houses h on h.id = pc.HouseObj
left join internet.LawyerPerson lp on lp.id = c.LawyerPerson
where 1 = 1 {0}
group by cp.Client;", region))
				.List<object>().Count;

			UniqueClient = _session.CreateSQLQuery(string.Format(@"
SELECT count(*) FROM logs.internetsessionslogs l
join internet.ClientEndPoints cp on cp.id = l.EndpointId
left join internet.Clients c on c.id = cp.client
left join internet.PhysicalClients pc on pc.id = c.PhysicalClient
left join internet.Houses h on h.id = pc.HouseObj
left join internet.LawyerPerson lp on lp.id = c.LawyerPerson
where l.LeaseBegin >= :beginInterval and l.LeaseBegin <= :endInterval {0}
group by cp.Client;", region))
				.SetParameter("beginInterval", DateTime.Now.AddDays(-1))
				.SetParameter("endInterval", DateTime.Now)
				.List<object>().Count;

			PhysicalRegister = Convert.ToInt32(_session.CreateSQLQuery(string.Format(@"
select count(*) from internet.Clients c
join internet.PhysicalClients pc on pc.id = c.PhysicalClient
left join internet.Houses h on h.id = pc.HouseObj
left join internet.LawyerPerson lp on lp.id = c.LawyerPerson
where 1 = 1 {0};
", region))
				.UniqueResult());

			LawyerRegister = Convert.ToInt32(_session.CreateSQLQuery(string.Format(@"
select count(*) from internet.Clients c
left join internet.PhysicalClients pc on pc.id = c.PhysicalClient
left join internet.Houses h on h.id = pc.HouseObj
join internet.LawyerPerson lp on lp.id = c.LawyerPerson
where 1 = 1 {0};
", region))
				.UniqueResult());

			AllRegister = Convert.ToInt32(_session.CreateSQLQuery(string.Format(@"
select count(*) from internet.Clients c
left join internet.PhysicalClients pc on pc.id = c.PhysicalClient
left join internet.Houses h on h.id = pc.HouseObj
left join internet.LawyerPerson lp on lp.id = c.LawyerPerson
where 1 = 1 {0}
", region))
				.UniqueResult());

			BlockedPhysical = Convert.ToInt32(_session.CreateSQLQuery(string.Format(@"
select count(*) from internet.Clients c
join internet.PhysicalClients pc on pc.id = c.PhysicalClient
left join internet.Houses h on h.id = pc.HouseObj
left join internet.LawyerPerson lp on lp.id = c.LawyerPerson
where c.Disabled and c.Status <> 10 {0};
", region))
				.UniqueResult());

			BlockedLawyer = Convert.ToInt32(_session.CreateSQLQuery(string.Format(@"
select count(*) from internet.Clients c
left join internet.PhysicalClients pc on pc.id = c.PhysicalClient
left join internet.Houses h on h.id = pc.HouseObj
join internet.LawyerPerson lp on lp.id = c.LawyerPerson
where c.Disabled and c.Status <> 10 {0};
", region))
				.UniqueResult());

			Dissolved = Convert.ToInt32(_session.CreateSQLQuery(string.Format(@"
select count(*) from internet.Clients c
left join internet.PhysicalClients pc on pc.id = c.PhysicalClient
left join internet.Houses h on h.id = pc.HouseObj
left join internet.LawyerPerson lp on lp.id = c.LawyerPerson
where c.Status = 10 {0};
", region))
				.UniqueResult());

			BlockedOnLine = _session.CreateSQLQuery(string.Format(@"
SELECT count(*) FROM internet.leases l
join internet.ClientEndPoints cp on cp.id = l.Endpoint
join internet.Clients c on c.id = cp.Client
left join internet.PhysicalClients pc on pc.id = c.PhysicalClient
left join internet.Houses h on h.id = pc.HouseObj
left join internet.LawyerPerson lp on lp.id = c.LawyerPerson
where c.Disabled {0}
group by cp.Client;", region))
				.List<object>().Count;
			return this;
		}
	}
}