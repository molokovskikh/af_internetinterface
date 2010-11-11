using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;

namespace InternetInterface.Models
{

	[ActiveRecord("PhysicalClients", Schema = "internet", Lazy = true)]
	public class Client : ActiveRecordLinqBase<Client>
	{
		public Client()
		{
			ValidationErrors = null;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите имя")]
		public virtual string Name { get; set; }

		[Property, ValidateNonEmpty("Введите фамилию")]
		public virtual string Surname { get; set; }

		[Property, ValidateNonEmpty("Введите отчество")]
		public virtual string Patronymic { get; set; }

		[Property]
		public virtual string City { get; set; }

		[Property, ValidateNonEmpty("Введите адрес подключения")]
		public virtual string AdressConnect { get; set; }

		[Property, ValidateRegExp(@"^(\d{4})?$", "Неправильный формат серии паспорта (4 цифры)"), ValidateNonEmpty("Поле не должно быть пустым")]
		public virtual string PassportSeries { get; set; }

		[Property, ValidateRegExp(@"^(\d{6})?$", "Неправильный формат номера паспорта (6 цифр)"), ValidateNonEmpty("Поле не должно быть пустым")]
		public virtual string PassportNumber { get; set; }

		[Property, ValidateNonEmpty("Заполните поле 'Кем выдан паспорт'")]
		public virtual string WhoGivePassport { get; set; }

		[Property, ValidateNonEmpty("Введите адрес регистрации")]
		public virtual string RegistrationAdress { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[BelongsTo("Tariff")]
		public virtual Tariff Tariff { get; set; }

		[Property, ValidateDecimal("Неверно введено число")]
		public virtual decimal Balance { get; set; }

		[Property, ValidateNonEmpty("Введите логин")]
		public virtual string Login { get; set; }

		[Property, ValidateNonEmpty("Введите пароль")]
		public virtual string Password { get; set; }

		[BelongsTo("HasRegistered")]
		public virtual Partner HasRegistered { get; set; }

		[BelongsTo("HasConnected")]
		public virtual Brigad HasConnected { get; set; }

		[Property]
		public virtual bool Connected { get; set; }

		private static ErrorSummary ValidationErrors;

		public virtual void SetValidationErrors(ErrorSummary _ValidationErrors)
		{
			ValidationErrors = _ValidationErrors;
		}

		public virtual ErrorSummary GetValidationErrors()
		{
			return ValidationErrors;
		}

		public virtual bool IsConnected()
		{
			return this.Connected;
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
	}

}