using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Inforoom2.Models;

namespace InforoomControlPanel.Models
{
	public class ViewModelConnectedHouse
	{
		public virtual int Id { get; set; }
		public virtual int Street { get; set; }
		public virtual string House { get; set; }
		public virtual string Comment { get; set; }
		public virtual bool Disabled { get; set; }
	}

	public class ViewModelConnectedHouses
	{
		public Street Street { get; set; }
		public List<ConnectedHouse> Houses { get; set; }
	}
}