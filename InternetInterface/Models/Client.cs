using System;
using System.Collections;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;
using Castle.MonoRail.Framework;

namespace InternetInterface.Models
{

	[ActiveRecord("PhysicalClients", Schema = "internet", Lazy = true)]
	public class Client : ChildActiveRecordLinqBase<Client>
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

		public static bool RegistrLogicClient(Client _client, bool _Popolnenie, uint _tariff, ValidatorRunner validator, Partner hasRegistered)
		{
			if (PartnerAccessSet.AccesPartner(AccessCategoriesType.RegisterClient))
			{
				var newClient = new Client();
				if (validator.IsValid(_client))
				{
					newClient.Name = _client.Name;
					newClient.Surname = _client.Surname;
					newClient.Patronymic = _client.Patronymic;
					newClient.City = _client.City;
					newClient.AdressConnect = _client.AdressConnect;
					newClient.PassportSeries = _client.PassportSeries;
					newClient.PassportNumber = _client.PassportNumber;
					newClient.WhoGivePassport = _client.WhoGivePassport;
					newClient.RegistrationAdress = _client.RegistrationAdress;
					newClient.RegDate = DateTime.Now;
					newClient.Tariff = Tariff.FindAllByProperty("Id", _tariff)[0];
					newClient.Balance = _Popolnenie ? newClient.Tariff.Price : 0;
					newClient.Login = _client.Login;
					newClient.Password = CryptoPass.GetHashString(_client.Password);
					newClient.HasRegistered = hasRegistered;
					newClient.SaveAndFlush();
					return true;
				}
			}
			return false;
		}
	}

}