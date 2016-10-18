using System.ComponentModel;
using System.Globalization;
using System.Web.Services.Description;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель города
	/// </summary>
	[Class(0, Table = "PublicDataContext", NameType = typeof (PublicDataContext))]
	public class PublicDataContext : BaseModel, IPositionIndex
	{
		[ManyToOne(Column = "ParentId", Cascade = "save-update"), NotNull]
		public virtual PublicData PublicData { get; set; }

		[Property(Column = "Content")]
		public virtual string Json { get; set; }

		[Property(Column = "RowIndex")]
		public virtual int? PositionIndex { get; set; }

		public virtual void ContextSet<T>(T obj)
		{
			Json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
		}

		public virtual T ContextGet<T>() where T : class
		{
			return string.IsNullOrEmpty(Json) ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Json);
		}
	}
	public class ViewModelPublicDataPriceList
	{
		public int Id { get; set; }
		[NotNullNotEmpty(Message = "не указано наименование")]
		public string Name { get; set; }
		public string Price { get; set; }
		public string Comment { get; set; }
	}
}