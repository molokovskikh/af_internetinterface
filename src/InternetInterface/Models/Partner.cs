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

		[Property, ValidateEmail("Ошибка формата Email"), ValidateNonEmpty("Введите EMail")]
		public virtual string Email { get; set; }

		[Property, ValidateRegExp(@"^((\d{4,5})-(\d{5,6}))|((\d{1})-(\d{3})-(\d{3})-(\d{2})-(\d{2}))", "Ошибка фотмата телефонного номера (Код города (4-5 цифр) + местный номер (5-6 цифр) или мобильный телефн (8-***-***-**-**))")
		, ValidateNonEmpty("Введите номер телефона")]
		public virtual string TelNum { get; set; }

		[Property, ValidateNonEmpty("Введите адрес")]
		public virtual string Adress { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		/*[Property, ValidateNonEmpty("Введите пароль")]
		public virtual string Pass { get; set; }*/

		[Property, ValidateNonEmpty("Введите логин")]
		public virtual string Login { get; set; }

		public static Partner GetPartnerForLogin(string login)
		{
			return FindAllByProperty("Login", login)[0];
		}

		private static bool IsBrigadir(Partner partner)
		{
			var result = PartnerAccessSet.FindAll(DetachedCriteria.For(typeof(PartnerAccessSet))
						.CreateAlias("AccessCat", "AC", JoinType.InnerJoin)
						.Add(Restrictions.Eq("PartnerId", partner))
						.Add(Restrictions.Eq("AC.ReduceName", "CD")));
			return result.Length != 0 ? true : false;
		}

		public static bool RegistrLogicPartner(Partner _Partner, List<int> _Rights, ValidatorRunner validator)
		{
				//var newPartner = new Partner();
				if (validator.IsValid(_Partner))
				{
					_Partner.RegDate = DateTime.Now;
					_Partner.SaveAndFlush();
					foreach (var right in _Rights)
					{
						/*if ((right == 4) && (!_Rights.Contains(1)))
						{
							_Rights.Add(1);
						}*/
						var newAccess = new PartnerAccessSet
						                	{
						                		AccessCat = AccessCategories.Find(right),
												PartnerId = _Partner
						                	};
						newAccess.SaveAndFlush();
					}
					AccessDependence.SetCrossAccessForRegister(_Rights,_Partner);
					//AccessDependence.SetCrossAccess(ChRights, rights, partner)

					var newPartnerID = Partner.FindAllByProperty("Login", _Partner.Login);
					if (IsBrigadir(_Partner))
					{
						var newBrigad = new Brigad
						                	{
												Name = _Partner.Name,
						                		PartnerID = newPartnerID[0],
												Adress = _Partner.Adress,
						                		BrigadCount = 1
						                	};
						newBrigad.SaveAndFlush();
					}
					return true;
				}
				else
				{
					return false;
				}
		}
	}

}