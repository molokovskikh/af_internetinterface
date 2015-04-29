using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Common.Tools;
using Inforoom2.validators;
using NHibernate.Mapping.Attributes;
using NHibernate.Util;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "PhysicalClients", Schema = "internet", NameType = typeof(PhysicalClient))]
	public class PhysicalClient : BaseModel
	{
		[Property]
		public virtual string Password { get; set; }

		[ManyToOne(Column = "_Address", Cascade = "save-update"), NotNull(Message = "Адрес указан не полностью!")]
		public virtual Address Address { get; set; }

		[Property(Column = "_Email", NotNull = true), Email(Message = "Неверная форма email")]
		public virtual string Email { get; set; }

		[ManyToOne(Column = "Tariff"), NotNull(Message = "Выберите тариф")]
		public virtual Plan Plan { get; set; }

		[Property(Column = "_LastTimePlanChanged")]
		public virtual DateTime LastTimePlanChanged { get; set; }

		[Property]
		public virtual decimal Balance { get; set; }

		[Property]
		public virtual decimal ConnectSum { get; set; }

		[Property]
		public virtual decimal VirtualBalance { get; set; }

		[Property, Description("Клиент проверен. устанавливается в админке, при редактировании клиента")]
		public virtual bool Checked { get; set; }

		[Property]
		public virtual decimal MoneyBalance { get; set; }

		[Property(Column = "IdDocType"), Description("Документ, удостоверяющий личность")]
		public virtual CertificateType CertificateType { get; set; }

		[Property(Column = "IdDocName"), NotNullNotEmpty(Message = "Введите название"), Description("Название документа, удостоверяющего личность")]  
		public virtual string CertificateName { get; set; }

		[Property]  
		public virtual string PassportNumber { get; set; }

		[Property]  
		public virtual string PassportSeries { get; set; }

		[DataType(DataType.Date)]
		[Property, ValidatorNotEmpty]
		public virtual DateTime PassportDate { get; set; }

		[Property(Column = "RegistrationAdress")]  
		public virtual string RegistrationAddress { get; set; }

		[Property(Column = "WhoGivePassport")]  
		public virtual string PassportResidention { get; set; }

		[Property, Description("Номер абонента Ситилайн")]
		public virtual int? ExternalClientId { get; set; }

		[Property(Column = "_PhoneNumber", NotNull = true), NHibernate.Validator.Constraints.NotEmpty(Message = "Введите номер телефона")]
		public virtual string PhoneNumber { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите имя")]
		public virtual string Name { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите фамилию")]
		public virtual string Surname { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите отчество")]
		public virtual string Patronymic { get; set; }

		[DataType(DataType.Date)]
		[Property(Column = "DateOfBirth"), ValidatorNotEmpty]
		public virtual DateTime BirthDate { get; set; }

		[OneToOne(PropertyRef = "PhysicalClient")]
		public virtual Client Client { get; set; }

		public virtual string FullName
		{
			get { return Surname + " " + Name + " " + Patronymic; }
		}

		public virtual UserWriteOff RequestChangePlan(Plan planToSwitchOn)
		{
			var price = Plan.GetTransferPrice(planToSwitchOn);
			if (!IsEnoughBalance(price)) {
				return null;
			}
			return SwitchPlan(planToSwitchOn, price);
		}

		private UserWriteOff SwitchPlan(Plan planTo, decimal price)
		{
			var comment = string.Format("Изменение тарифа, старый '{0}' новый '{1}'", Plan.Name, planTo.Name);
			Plan = planTo;
			WriteOff(price);
			var writeOff = new UserWriteOff {
				Client = Client,
				Date = DateTime.Now,
				Sum = price,
				Comment = comment,
				IsProcessedByBilling = true
			};
			LastTimePlanChanged = DateTime.Now;
			if (Client.Internet.ActivatedByUser)
				Client.Endpoints.ForEach(e => e.PackageId = Plan.PackageSpeed.PackageId);
			return writeOff;
		}

		public virtual bool IsEnoughBalance(decimal sum)
		{
			if (sum < 0) {
				return false;
			}
			return Balance - sum > 0;
		}

		public virtual WriteOff WriteOff(decimal sum, bool writeoffVirtualFirst = false)
		{
			var writeoff = CalculateWriteoff(sum, writeoffVirtualFirst);

			if (writeoff == null)
				return null;

			Balance -= writeoff.WriteOffSum;
			VirtualBalance -= writeoff.VirtualSum;
			MoneyBalance -= writeoff.MoneySum;

			return writeoff;
		}

		public virtual WriteOff CalculateWriteoff(decimal sum, bool writeoffVirtualFirst = false)
		{
			if (sum <= 0)
				return null;

			decimal virtualWriteoff;
			decimal moneyWriteoff;

			if (writeoffVirtualFirst) {
				virtualWriteoff = Math.Min(sum, VirtualBalance);
			}
			else {
				virtualWriteoff = Math.Min(Math.Abs(Math.Min(MoneyBalance - sum, 0)), VirtualBalance);
			}
			moneyWriteoff = sum - virtualWriteoff;

			return new WriteOff {
				Client = Client,
				WriteOffDate = SystemTime.Now(),
				WriteOffSum = Math.Round(sum, 2),
				MoneySum = Math.Round(moneyWriteoff, 2),
				VirtualSum = Math.Round(virtualWriteoff, 2),
				Sale = Client.Discount,
				BeforeWriteOffBalance = Client.Balance
			};
		}


		///  Генерация пароля для пользователя
		///  *взято из старой админки////////////////////// 
		public static string GeneratePassword( PhysicalClient ph )
		{
			var availableChars = "23456789qwertyupasdfghjkzxcvbnmQWERTYUPASDFGHJKLZXCVBNM";
			var password = String.Empty;
			while (password.Length < 8) {
				int availableChars_elem = 0;

				var rngCsp = new RNGCryptoServiceProvider();
				var randomNumber = new byte[1];
				do {
					rngCsp.GetBytes(randomNumber);
				} while (!(randomNumber[0] < (availableChars.Length - 1) * (Byte.MaxValue / (availableChars.Length - 1))));

				availableChars_elem = (randomNumber[0] % (availableChars.Length - 1)) + 1;

				password += availableChars[availableChars_elem];
			}
			string hash = string.Empty;
				if (password != null) {
				byte[] bytes = Encoding.Unicode.GetBytes(password);
				var CSP = new MD5CryptoServiceProvider();
				byte[] byteHash = CSP.ComputeHash(bytes);
				foreach (byte b in byteHash)
					hash += string.Format("{0:x2}", b);
			}
			ph.Password = hash;
			return password;
		}

		//////////////////////////////////////////////////
	}

	public enum CertificateType
	{
		[Display(Name = "Паспорт РФ")] [Description("Паспорт РФ")] Passport = 0,
		[Display(Name = "Иной документ")] [Description("Иной документ")] Other = 1
	}
}