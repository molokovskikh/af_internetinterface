using System;
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

		/// <summary>
		/// Получение форматированного адреса
		/// </summary>
		/// <param name="city">Город</param>
		/// <param name="street">Улица</param>
		/// <param name="house">Дом</param>
		/// <param name="entrance">Подъезд</param>
		/// <param name="floor">Этаж</param>
		/// <param name="apartment">Квартира</param>
		/// <returns>Форматированный адрес</returns>
		public virtual string GetStringForPrint(bool city = true, bool street = true, bool house = true, bool entrance = true, bool floor = true, bool apartment = true)
		{
			var cityReg = (House.Region == null ? House.Street.Region.City.Name : House.Region.City.Name);

			return GetAddressString(street ? House.Street.Name : "", house ? House.Number : "",
				city ? cityReg : "", apartment ? Apartment ?? "" : "", entrance ? Entrance ?? "" : "", floor ? Floor == 0 ? "" : Floor.ToString() : "");
		}

		/// <summary>
		/// Формирование адреса
		/// </summary>
		/// <param name="street">Улица</param>
		/// <param name="house">Дом</param>
		/// <param name="city">Город</param>
		/// <param name="apartment">Квартира</param>
		/// <param name="entrance">Подъезд</param>
		/// <param name="floor">Этаж</param>
		/// <param name="mask">форматирующая маска</param>
		/// <returns></returns>
		protected static string GetAddressString(string street, string house, string city = "", string apartment = "", string entrance = "", string floor = "", string mask = "")
		{
			string address;
			if (mask != String.Empty) {
				address = string.Format(mask, GetPrintStreet(street), house, city, apartment, entrance, floor);
			}
			else {
				address = (city != "" ? "г. " + city : "")
				          + (street != "" ? ", " + GetPrintStreet(street) : "")
				          + (house != "" ? ", д. " + house : "")
				          + (apartment != "" ? ", кв. " + apartment : "")
				          + (entrance != "" ? ", подъезд " + entrance : "")
				          + (floor != "" ? ", этаж " + floor : "");
				address = address[0] == ',' ? address.Substring(1) : address;
				address = address.Replace(",,", ",");
			}
			return address;
		}

		/// <summary>
		/// Форматирование названия улицы
		/// </summary>
		/// <param name="street">Улица</param>
		/// <returns>Форматированное название улицы</returns>
		protected static string GetPrintStreet(string street)
		{
			var shortCut = new Dictionary<string, string>() {
				{ "улица", "ул." },
				{ "проезд", "пр-д." },
				{ "проспект", "просп." },
				{ "переулок", "пер." },
				{ "бульвар", "бул." }
			};
			street = street.Trim();
			bool withoutCut = true;
			for (int i = 0; i < shortCut.Count; i++) {
				var indexOfCut = street.ToLower().IndexOf(shortCut.ElementAt(i).Key);
				if (indexOfCut != -1) {
					var streetSubStings = street.Split(' ');
					var newStreet = "";
					for (int j = 0; j < streetSubStings.Length; j++) {
						if (!shortCut.Any(s => s.Key == streetSubStings[j].ToLower() || s.Value == streetSubStings[j].ToLower())) {
							newStreet += (" " + streetSubStings[j][0].ToString().ToUpper() + streetSubStings[j].Substring(1));
						}
					}
					street = shortCut.ElementAt(i).Value + " " + newStreet;
					withoutCut = false;
				}
				else if (street.ToLower().IndexOf(shortCut.ElementAt(i).Value) != -1) {
					var streetSubStings = street.Split(' ');
					var newStreet = "";
					for (int j = 0; j < streetSubStings.Length; j++) {
						if (!shortCut.Any(s => s.Key == streetSubStings[j].ToLower() || s.Value == streetSubStings[j].ToLower())) {
							newStreet += newStreet.Length == 0 ? streetSubStings[j][0].ToString().ToUpper() + streetSubStings[j].Substring(1)
								: " " + streetSubStings[j][0].ToString().ToUpper() + streetSubStings[j].Substring(1);
						}
						else {
							newStreet += newStreet.Length == 0 ? streetSubStings[j] : " " + streetSubStings[j];
						}
					}
					street = newStreet;
					withoutCut = false;
				}
			}
			if (withoutCut) {
				street = "ул. " + street;
			}
			return street.Trim();
		}
	}
}