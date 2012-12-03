using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;

namespace InforoomInternet.Models
{
	[ActiveRecord("ViewTexts", Schema = "internet")]
	public class ViewText
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public string Description { get; set; }

		[Property]
		public string Text { get; set; }
	}
}