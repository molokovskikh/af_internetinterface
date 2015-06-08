using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Inforoom2.Helpers;
using Inforoom2.Intefaces;
using Inforoom2.validators;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "address", NameType = typeof(Address)), Description("Адрес")]
	public class Address : BaseModel, ILogAppeal
	{
		[ManyToOne(Column = "house", Cascade = "save-update"), ValidatorNotEmpty, Description("Дом")]
		public virtual House House { get; set; }

		[Property, Description("Подъезд")]
		public virtual string Entrance { get; set; }

		[Property, Description("Квартира")]
		public virtual string Apartment { get; set; }

		[Property, Description("Этаж")]
		public virtual int Floor { get; set; }

		//true если яндекс api нашел адрес
		[Property, Description("Проверен Яндексом")]
		public virtual bool IsCorrectAddress { get; set; }

		public virtual Region Region
		{
			get { return House.Region ?? House.Street.Region; }
		}

		public virtual string FullAddress
		{
			get
			{
				return (House.Region == null ? House.Street.Region.City.Name : House.Region.City.Name) + ". "
				       + House.Street.Name + ". "
				       + House.Number;
			}
		}

		public virtual string AddressAsString { get; set; }

		public virtual Client GetAppealClient(ISession session)
		{
			return session.Query<Client>().FirstOrDefault(s => s.PhysicalClient.Address == this);
		}

		public virtual List<string> GetAppealFields()
		{
			return new List<string>() {
				"House",
				"Entrance",
				"Apartment",
				"Floor",
				"IsCorrectAddress"
			};
		}

		public virtual string GetAdditionalAppealInfo(string property, object oldPropertyValue, ISession session)
		{
			string message = "";
			// для свойства House
			if (property == "House") {
				// получаем псевдоним из описания
				property = this.House.GetDescription();
				var oldHouse = oldPropertyValue == null ? null : ((House)oldPropertyValue);
				var currentHouse = this.House;
				if (oldHouse == null) {
					message += property + " было: " + "значение отсуствует <br/> ";
				}
				else {
					// получаем недостающие значения моделей (без них ошибка у NHibernate во Flush)
					oldHouse.Street = session.Query<Street>().FirstOrDefault(s => s == oldHouse.Street);
					oldHouse.Street.Houses = session.Query<Street>().FirstOrDefault(s => s == oldHouse.Street).Houses;
					message += property + " было: " + oldHouse.Street.Name + ", д." + oldHouse.Number + "<br/>";
				}
				if (currentHouse == null) {
					message += property + " стало: " + "значение отсуствует <br/> ";
				}
				else {
					// получаем недостающие значения моделей (без них ошибка у NHibernate во Flush)
					this.House = session.Query<House>().FirstOrDefault(s => s == this.House);
					this.House.Street = session.Query<Street>().FirstOrDefault(s => s == this.House.Street);
					this.House.Street.Houses = session.Query<Street>().FirstOrDefault(s => s == this.House.Street).Houses;
					message += property + " стало: " + currentHouse.Street.Name + ", д." + currentHouse.Number + "<br/>";
				}
			}
			return message;
		}
	}
}