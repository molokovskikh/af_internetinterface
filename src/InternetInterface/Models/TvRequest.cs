using System;
using Castle.ActiveRecord;
using Castle.Components.Validator;

namespace InternetInterface.Models
{
	/// <summary>
	/// Заявка на подключение ТВ пользователю
	/// </summary>
	[ActiveRecord("TvRequests", Schema = "Internet", Lazy = true)]
	public class TvRequest : ChildActiveRecordLinqBase<TvRequest>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string AdditionalContact { get; set; }

		[Property, ValidateNonEmpty]
		public virtual bool Hdmi { get; set; }

		[Property, ValidateNonEmpty]
		public virtual DateTime Date { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[BelongsTo, ValidateNonEmpty]
		public virtual Client Client { get; set; }

		[BelongsTo, ValidateNonEmpty]
		public virtual Partner Partner { get; set; }

		[BelongsTo]
		public virtual Contact Contact { get; set; }

		public TvRequest()
		{
			Date = DateTime.Now;
		}

		public TvRequest(Client client) : this()
		{
		}
	}
}