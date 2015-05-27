using System;
using System.ComponentModel;
using System.Globalization;
using System.Globalization;
using System.Web.Services.Description;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	///		Слайды (на главной странице сайта ivrn )
	/// </summary>
	[Class(0, Table = "slide", NameType = typeof(Slide))]
	public class Slide : PriorityModel
	{
		[Description("Регион, в котором действует тариф")]
		[ManyToOne(Column = "Region", Cascade = "save-update")]
		public virtual Region Region { get; set; }

		[Property, Description("Ссылка слайда")]
		public virtual string Url { get; set; }

		[Property, Description("Путь к изображению слайда")]
		public virtual string ImagePath { get; set; }

		[Property, Description("Время последнего изменения слайда")]
		public virtual DateTime LastEdit { get; set; }

		[ManyToOne(Column = "Partner", Cascade = "save-update"), Description("Пользователь, вносивший изменения")]
		public virtual Employee Partner { get; set; }

		[Property, Description("Маркер, отражающий, показывается ли слайд пользователям")]
		public virtual bool Enabled { get; set; }
	}
}