using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Inforoom2.Components;
using Inforoom2.Controllers;

namespace InforoomControlPanel.ReportTemplates
{
	public class FilterReport<T> : InforoomModelFilter<T>
	{
		public FilterReport(BaseController controller)
			: base(controller)
		{
		}

		public void SetTotalItems(int number)
		{
			_totalItems = number;
		}
		public new void SetItemsPerPage(int number)
		{
			ItemsPerPage = number;
		}
		public void SetCurrentPage(int number)
		{
			Page = number;
		}
	}
}