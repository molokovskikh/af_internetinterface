using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Common.Tools.Calendar;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Queries
{
	public class WriteOffsItem : BaseItemForTable
	{
		public WriteOffsItem(WriteOff writeOff)
		{
			ClientId = writeOff.Client.Id.ToString();
			Name = writeOff.Client.Name;
			_sum = writeOff.WriteOffSum;
			_date = writeOff.WriteOffDate;
			Comment = writeOff.Comment;
		}

		[Display(Name = "Код клиента", Order = 0)]
		public string ClientId { get; set; }

		[Display(Name = "Клиент", Order = 1)]
		public string Name { get; set; }

		private decimal _sum;

		[Display(Name = "Сумма", Order = 2)]
		public string Sum {
			get { return _sum.ToString("0:C"); }
		}

		private DateTime _date;

		[Display(Name = "Дата", Order = 3)]
		public string Date {
			get { return _date.ToShortDateString(); }
		}

		[Display(Name = "Комментарий", Order = 4)]
		public string Comment { get; set; }
	}

	public class WriteOffsFilter : PaginableSortable
	{
		public ISession Session { get; set; }
		public RegionHouse Region { get; set; }
		public DateTime BeginDate { get; set; }
		public DateTime EndDate { get; set; }
		public ForSearchClientType ClientType { get; set; }
		public string Name { get; set; }

		public WriteOffsFilter()
		{
			BeginDate = DateTime.Now.FirstDayOfMonth();
			EndDate = DateTime.Now;
			Region = null;
		}

		public IList<BaseItemForTable> Find()
		{
			var firstDataQuery = Session.Query<WriteOff>().Where(w => w.WriteOffDate >= BeginDate.Date && w.WriteOffDate <= EndDate.Date);
			if (Region != null)
				firstDataQuery = firstDataQuery.Where(w => w.Client.PhysicalClient.HouseObj.Region == Region || w.Client.LawyerPerson.Region == Region.Id);

			if (ClientType == ForSearchClientType.Lawyer)
				firstDataQuery = firstDataQuery.Where(w => w.Client.LawyerPerson != null);

			if (ClientType == ForSearchClientType.Physical)
				firstDataQuery = firstDataQuery.Where(w => w.Client.PhysicalClient != null);

			if (!string.IsNullOrEmpty(Name.Trim()))
				firstDataQuery = firstDataQuery.Where(w => w.Client.Name.Contains(Name));

			return firstDataQuery.ToList().Skip(CurrentPage * PageSize).Take(PageSize).Select(w => new WriteOffsItem(w)).Cast<BaseItemForTable>().ToList();
		}
	}
}