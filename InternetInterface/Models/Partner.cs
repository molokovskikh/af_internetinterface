using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;
using Castle.MonoRail.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord("Partners", Schema = "internet", Lazy = true)]
	public class Partner : ChildActiveRecordLinqBase<Partner>
	{
		public Partner()
		{
			ValidationErrors = null;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите имя")]
		public virtual string Name { get; set; }

		[Property, ValidateEmail("Ошибка формата Email"), ValidateNonEmpty("Введите EMail")]
		public virtual string Email { get; set; }

		[Property, ValidateRegExp(@"^((\d{4,5})-(\d{5,6}))|((\d{1})-(\d{3})-(\d{3})-(\d{2})-(\d{2}))", "Ошибка фотмата телефонного номера (Код города (4-5 цифр) + местный номер (5-6 цифр) или мобильный телефн (8-***-***-**-**))")
		, ValidateNonEmpty("Введите номер телефона")]
		public virtual string TelNum { get; set; }

		[Property, ValidateNonEmpty("Введите имя")]
		public virtual string Adress { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[Property, ValidateNonEmpty("Введите имя")]
		public virtual string Pass { get; set; }

		[Property, ValidateNonEmpty("Введите имя")]
		public virtual string Login { get; set; }

		private static ErrorSummary ValidationErrors;

		public virtual void SetValidationErrors(ErrorSummary _ValidationErrors)
		{
			ValidationErrors = _ValidationErrors;
		}

		public virtual ErrorSummary GetValidationErrors()
		{
			return ValidationErrors;
		}

		public static Partner GetPartnerForLogin(string login)
		{
			return FindAllByProperty("Login", login)[0];
		}

		public virtual string GetErrorText(string field)
		{
			if (ValidationErrors != null)
			{
				for (int i = 0; i < ValidationErrors.ErrorsCount; i++)
				{
					if (ValidationErrors.ErrorMessages != null)
					{
						if (ValidationErrors.InvalidProperties[i] == field)
						{
							return ValidationErrors.ErrorMessages[i];
						}
					}
				}
			}
			return "";
		}

		public static bool RegistrLogicPartner(Partner _Partner, List<uint> _Rights, ValidatorRunner validator)
		{
			if (PartnerAccessSet.AccesPartner(AccessCategoriesType.RegisterPartner))
			{
				var newPartner = new Partner();
				if (validator.IsValid(_Partner))
				{
					newPartner.Name = _Partner.Name;
					newPartner.Email = _Partner.Email;
					newPartner.TelNum = _Partner.TelNum;
					newPartner.Adress = _Partner.Adress;
					newPartner.RegDate = DateTime.Now;
					newPartner.Login = _Partner.Login;
					newPartner.Pass = CryptoPass.GetHashString(_Partner.Pass);
					//newPartner.AcessSet = CombineAccess(_Rights);
					newPartner.SaveAndFlush();
					var newPartnerID = Partner.FindAllByProperty("Login", _Partner.Login);
					if (PartnerAccessSet.AccesPartner(AccessCategoriesType.CloseDemand))
					{
						var newBrigad = new Brigad();
						newBrigad.Name = newPartner.Name;
						newBrigad.PartnerID = newPartnerID[0];
						newBrigad.Adress = newPartner.Adress;
						newBrigad.BrigadCount = 1;
						newBrigad.SaveAndFlush();
					}
					return true;
				}
			}
			return false;
		}
	}

}