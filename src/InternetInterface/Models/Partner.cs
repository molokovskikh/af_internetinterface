using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

namespace InternetInterface.Models
{
	[ActiveRecord("Partners", Schema = "internet", Lazy = true)]
	public class Partner : ValidActiveRecordLinqBase<Partner>
	{

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите имя")]
		public virtual string Name { get; set; }

		[Property, ValidateEmail("Ошибка формата Email")]
		public virtual string Email { get; set; }

		[Property, ValidateRegExp(@"^((\d{4,5})-(\d{5,6}))|((\d{1})-(\d{3})-(\d{3})-(\d{2})-(\d{2}))", "Ошибка фотмата телефонного номера (Код города (4-5 цифр) + местный номер (5-6 цифр) или мобильный телефн (8-***-***-**-**))")]
		public virtual string TelNum { get; set; }

		[Property]
		public virtual string Adress { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[Property, ValidateNonEmpty("Введите логин"), ValidateIsUnique("Логин должен быть уникальный")]
		public virtual string Login { get; set; }

		public static Partner GetPartnerForLogin(string login)
		{
			return FindAllByProperty("Login", login)[0];
		}

		/*private static bool IsBrigadir(Partner partner)
		{
			var result = PartnerAccessSet.FindAll(DetachedCriteria.For(typeof(PartnerAccessSet))
						.CreateAlias("AccessCat", "AC", JoinType.InnerJoin)
						.Add(Restrictions.Eq("PartnerId", partner))
						.Add(Restrictions.Eq("AC.ReduceName", "CD")));
			return result.Length != 0 ? true : false;
		}*/

		public static bool RegistrLogicPartner(Partner _Partner, List<int> _Rights, ValidatorRunner validator)
		{
				if (validator.IsValid(_Partner))
				{
					_Partner.RegDate = DateTime.Now;
					_Partner.SaveAndFlush();
					foreach (var right in _Rights)
					{
						var newAccess = new PartnerAccessSet
						                	{
						                		AccessCat = AccessCategories.Find(right),
												PartnerId = _Partner
						                	};
						newAccess.SaveAndFlush();
					}
					AccessDependence.SetCrossAccessForRegister(_Rights,_Partner);
					return true;
				}
				else
				{
					return false;
				}
		}
	}

}