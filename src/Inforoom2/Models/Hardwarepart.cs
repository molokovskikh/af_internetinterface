using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Inforoom2.Components;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;
using NHibernate.Validator.Engine;

namespace Inforoom2.Models
{
	/// <summary>
	/// Комплектация арендуемого оборудования (по умолчанию)
	/// </summary>
	[Class(0, Table = "hardwareparts", NameType = typeof(HardwarePart))]
	public class HardwarePart : BaseModel
	{
		[Property(NotNull = true), Description("Название"), NotNullNotEmpty(Message = "Поле не может быть пустым")]
		public virtual string Name { get; set; }

		[ManyToOne(Column = "RentalHardware", Cascade = "save-update"), Description("Тип оборудования")]
		public virtual RentalHardware RentalHardware { get; set; }

		public override ValidationErrors Validate(ISession session)
		{
			var invalidValue = new ValidationErrors();
			//Проверка на наличие существующего элемента, при добавлении в комплектацию  
			if (RentalHardware != null )
			{
				var hardwarePart = this;
				if (hardwarePart.RentalHardware == null)
				{
					invalidValue.Add(new InvalidValue("Элемент комплектации не закреплен за арендуемым оборудованием!", this.GetType(), "RentalHardware", RentalHardware, RentalHardware, new Collection<object>())); 
				}
				var hardware = hardwarePart.RentalHardware.HardwareParts;
				// если найдены совпадения
				if (hardware.Any(s => s.Name == hardwarePart.Name))
				{
					invalidValue.Add(new InvalidValue("Комплектация уже содержит данный элемент!", this.GetType(), "Name", Name, Name, new Collection<object>()));  
				}
			}
			return invalidValue;
		}
	}
}