using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Inforoom2.Models;

namespace InforoomControlPanel.Models
{
	public class ViewModelEmployeeGroupPaymentsReport
	{
		public EmployeeGroup EmployeeGroup { get; set; }
		public Employee Employee { get; set; }
		public decimal Sum { get; set; }
		public decimal VirtualSum { get; set; }
	}
}