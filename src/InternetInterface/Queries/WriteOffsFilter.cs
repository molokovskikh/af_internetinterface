using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Queries
{
	public class WriteOffsFilter : PaginableSortable
	{
		public ISession Session { get; set; }
		public RegionHouse Region { get; set; }
		public DateTime BeginDate { get; set; }
		public DateTime EndDate { get; set; }

		public IList<BaseItemForTable> Find()
		{
			var firstDataQuery = Session.Query<WriteOff>().Where(w => w.WriteOffDate >= BeginDate.Date && w.WriteOffDate <= EndDate.Date);
		}
	}
}