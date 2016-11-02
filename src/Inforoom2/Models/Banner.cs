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
	///		Банер (на главной странице сайта ivrn )
	/// </summary>
	[Class(0, Table = "banner", NameType = typeof(Banner))]
	public class Banner : BaseModel
	{
		[Description("Регион, в котором действует тариф")]
		[ManyToOne(Column = "Region", Cascade = "save-update")]
		public virtual Region Region { get; set; }

		[Property, Description("Ссылка слайда")]
		public virtual string Url { get; set; }

		[Property, Description("Путь к изображению слайда")]
		public virtual string ImagePath { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property, Description("Время последнего изменения слайда")]
		public virtual DateTime LastEdit { get; set; }

		[Property(Column = "Target")]
		public virtual BannerType Type { get; set; }

		[ManyToOne(Column = "Partner", Cascade = "save-update"), Description("Пользователь, вносивший изменения")]
		public virtual Employee Partner { get; set; }

		[Property, Description("Маркер, отражающий, показывается ли слайд пользователям")]
		public virtual bool Enabled { get; set; }
	}

	public enum BannerType
	{
		[Description("Главная страница")]
		ForMainPage = 0,
		[Description("Страница клиента")]
		ForClientPage = 1
	}
}