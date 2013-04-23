using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Common.Tools.Calendar;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Queries
{
	public class WriteOffsItem : BaseItemForTable
	{
		[Display(Name = "Код клиента", Order = 0)]
		public string ClientId { get; set; }

		[Display(Name = "Клиент", Order = 1)]
		public string Name { get; set; }

		[Display(Name = "Регион", Order = 2)]
		public string Region { get; set; }

		[Display(Name = "Сумма", Order = 3)]
		public string Sum { get; set; }

		[Display(Name = "Дата", Order = 4)]
		public string Date { get; set; }

		[Display(Name = "Комментарий", Order = 5)]
		public string Comment { get; set; }
	}

	public class WriteOffsFilter : PaginableSortable
	{
		public ISession Session { get; set; }
		public uint Region { get; set; }
		public DateTime BeginDate { get; set; }
		public DateTime EndDate { get; set; }
		public ForSearchClientType ClientType { get; set; }
		public string Name { get; set; }

		public bool ExportInExcel { get; set; }

		public WriteOffsFilter()
		{
			BeginDate = DateTime.Now.FirstDayOfMonth();
			EndDate = DateTime.Now;
			SortDirection = "asc";

			SortKeyMap = new Dictionary<string, string> {
				{ "ClientId", "c.Id" },
				{ "Region", "r.Id" },
				{ "Date", "w.WriteOffDate" },
				{ "Comment", "w.Comment" },
				{ "Sum", "w.WriteOffSum" },
				{ "Name", "c.Name" },
				{ "Init", "c.id, w.WriteOffDate" }
			};
		}

		public IList<BaseItemForTable> Find()
		{
			var countPart = "count(w.Id)";

			var selectPart = @"c.Id as ClientId,
c.Name as Name,
r.Region as Region,
w.WriteOffSum as `Sum`,
w.WriteOffDate as `Date`,
w.`Comment` as `Comment`";

			var sql = @"
select
{0}
from internet.WriteOff w
join internet.Clients c on c.Id = w.Client
left join internet.PhysicalClients pc on pc.id = c.PhysicalClient
left join internet.LawyerPerson lp on lp.id = c.LawyerPerson
left join internet.Houses h on h.id = pc.HouseObj
left join internet.regions r on h.RegionId = r.id or lp.RegionId = r.id
where
w.WriteOffDate >= :BeginDate
and w.WriteOffDate <= :EndDate";
			if (Region > 0)
				sql += " and (h.RegionId = :Region or lp.RegionId = :Region)";

			if (ClientType == ForSearchClientType.Physical)
				sql += " and pc.Id is not null";

			if (ClientType == ForSearchClientType.Lawyer)
				sql += " and lp.Id is not null";

			if (Name != null && !string.IsNullOrEmpty(Name.Trim()))
				sql += " and c.Name like :Name";

			if (string.IsNullOrEmpty(SortBy))
				SortBy = "Init";

			RowsCount = (int)SetParameters(Session.CreateSQLQuery(string.Format(sql, countPart))).UniqueResult<long>();

			sql += string.Format(@" order by {0} {1}", SortKeyMap[SortBy], SortDirection);

			if (!ExportInExcel)
				sql += string.Format(" limit {0}, {1};", CurrentPage * PageSize, PageSize);

			var query = SetParameters(Session.CreateSQLQuery(string.Format(sql, selectPart)));

			return query.ToList<WriteOffsItem>().Cast<BaseItemForTable>().ToList();
		}

		private IQuery SetParameters(IQuery query)
		{
			query.SetParameter("BeginDate", BeginDate.Date).SetParameter("EndDate", EndDate.Date);

			if (Region > 0)
				query.SetParameter("Region", Region);
			if (Name != null && !string.IsNullOrEmpty(Name.Trim()))
				query.SetParameter("Name", "%" + Name + "%");

			return query;
		}
	}
}