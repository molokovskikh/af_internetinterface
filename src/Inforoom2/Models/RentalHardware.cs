using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Inforoom2.validators;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Тип арендуемого оборудования
	/// </summary>
	[Class(0, Table = "RentalHardware", NameType = typeof(RentalHardware))]
	public class RentalHardware : BaseModel
	{
		public RentalHardware()
		{
			HardwareParts = new List<HardwarePart>();
		}

		[Property(NotNull = true), Description("Название"), NotNullNotEmpty(Message = "Поле не может быть пустым")]
		public virtual string Name { get; set; }

		[Property(NotNull = true), Description("Стоимость"), DecimalMin(0.0, Message = "Введите число >= 0")]
		public virtual decimal Price { get; set; }

		[Property(NotNull = true), Description("Количество бесплатных дней"), Digits(3, 0, Message = "Введите число от 0 до 999")]
		public virtual uint FreeDays { get; set; }

		[Bag(0, Table = "hardwareparts", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "RentalHardware")]
		[OneToMany(2, ClassType = typeof(HardwarePart))]
		public virtual IList<HardwarePart> HardwareParts { get; set; }
	}
}