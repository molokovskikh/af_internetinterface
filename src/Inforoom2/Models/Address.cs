using System.Collections.Generic;
using System.Linq;
using Inforoom2.Intefaces;
using Inforoom2.validators;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "address", NameType = typeof(Address))]
	public class Address : BaseModel, ILogAppeal
	{
		[ManyToOne(Column = "house", Lazy = Laziness.False, Cascade = "save-update"), ValidatorNotEmpty]
		public virtual House House { get; set; }

		[Property]
		public virtual string Entrance { get; set; }

		[Property]
		public virtual string Apartment { get; set; }

		[Property]
		public virtual int Floor { get; set; }

		//true если яндекс api нашел адрес
		[Property]
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
			var physicalClient = session.Query<PhysicalClient>().FirstOrDefault(s => s.Address == this);
			if (physicalClient != null) {
				return physicalClient.Client;
			}
			return null;
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

		public virtual string GetRelationChanges(string property, object oldPropertyValue)
		{
			string message = "";
			// для свойства House
			if (property == "House") {
				var oldHouse = oldPropertyValue == null ? null : ((House)oldPropertyValue);
				var currentHouse = this.House;
				if (oldHouse != null) {
					message += property + " было: " + oldHouse.Street.Name + ", д." + oldHouse.Number + "<br/>";
				}
				else {
					message += property + " было: " + "значение отсуствует <br/> ";
				}
				if (currentHouse != null) {
					message += property + " стало: " + currentHouse.Street.Name + ", д." + currentHouse.Number + "<br/>";
				}
				else {
					message += property + " стало: " + "значение отсуствует <br/> ";
				}
			}
			return message;
		}
	}
}