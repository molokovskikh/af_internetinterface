using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель не предназначена для бизнес-логики, служит только для фильтрации сотрудников, начисляющих платежи
	/// </summary>
	[Class(0, Table = "paymentsystems", NameType = typeof (PaymentSystem))]
	public class PaymentSystem : BaseModel
	{
		[ManyToOne(Column = "Employee", Cascade = "save-update"), NotNull]
		public virtual Employee Employee { get; set; }
	}
}