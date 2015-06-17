using System;
using System.ComponentModel;
using System.Globalization;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Services.Description;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{ 
	/// <summary>
	///		Содержание в html
	/// </summary>
	[Class(0, Table = "PlanHtmlContent", NameType = typeof(PlanHtmlContent))]
	public class PlanHtmlContent : BaseModel
	{
		[Description("Регион, в котором будет отображаться HTML")]
		[ManyToOne(Column = "Region", Cascade = "save-update"),NotNull]
		public virtual Region Region { get; set; }

		[Property, Description("Содержание")]
		public virtual string Content { get; set; } 
	}
}