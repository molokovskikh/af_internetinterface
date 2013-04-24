using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using Common.Tools;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "Billing", Table = "Recipients")]
	public class Recipient : ActiveRecordLinqBase<Recipient>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string FullName { get; set; }

		[Property]
		public virtual string Address { get; set; }

		[Property]
		public virtual string INN { get; set; }

		[Property]
		public virtual string KPP { get; set; }

		[Property]
		public virtual string BIC { get; set; }

		[Property]
		public virtual string Bank { get; set; }

		[Property]
		public virtual string BankLoroAccount { get; set; }

		[Property]
		public virtual string BankAccountNumber { get; set; }

		[Property]
		public virtual string Boss { get; set; }

		[Property]
		public virtual string Accountant { get; set; }

		[Property]
		public virtual string AccountWarranty { get; set; }

		public static IList<Recipient> All()
		{
			return Queryable.OrderBy(r => r.Name).ToList();
		}
	}

	public class IgnoredInn : ActiveRecordLinqBase<IgnoredInn>
	{
		public IgnoredInn()
		{
		}

		public IgnoredInn(string inn)
		{
			Inn = inn;
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[Property, ValidateNonEmpty]
		public string Inn { get; set; }
	}

	[ActiveRecord("BankPayments", Schema = "Internet")]
	public class BankPayment : ActiveRecordLinqBase<BankPayment>
	{
		public BankPayment(Client payer, DateTime payedOn, decimal sum)
			: this(payer)
		{
			Sum = sum;
			PayedOn = payedOn;
		}

		public BankPayment(Client payer)
			: this()
		{
			Payer = payer;
			Recipient = payer.Recipient;
		}

		public BankPayment()
			: base()
		{
			UpdatePayerInn = true;
		}

		[PrimaryKey]
		public uint Id { get; set; }

		//информация ниже получается из выписки
		//фактическая дата платежа когда он прошел через банк
		[Property, ValidateNonEmpty, Description("Дата платежа")]
		public DateTime PayedOn { get; set; }

		[Property, ValidateGreaterThanZero, Description("Сумма")]
		public decimal Sum { get; set; }

		[Property, Description("Описание платежа")]
		public string Comment { get; set; }

		[Property, Description("Номер документа")]
		public string DocumentNumber { get; set; }

		[Nested(ColumnPrefix = "Payer"), Description("ИНФОРМАЦИЯ О КЛИЕНТЕ")]
		public BankClient PayerClient { get; set; }

		[Nested(ColumnPrefix = "PayerBank"), Description("ИНФОРМАЦИЯ О БАНКЕ КЛИЕНТА")]
		public BankInfo PayerBank { get; set; }

		[Nested(ColumnPrefix = "Recipient"), Description("ИНФОРМАЦИЯ О ПОЛУЧАТЕЛЕ")]
		public BankClient RecipientClient { get; set; }

		[Nested(ColumnPrefix = "RecipientBank"), Description("ИНФОРМАЦИЯ О БАНКЕ ПОЛУЧАТЕЛЯ")]
		public BankInfo RecipientBank { get; set; }

		//все что выше получается из выписки
		//дата занесения платежа
		[BelongsTo(Column = "PayerId", Cascade = CascadeEnum.SaveUpdate) /*, ValidateNonEmpty("Обязательно укажите плательщика")*/]
		public virtual Client Payer { get; set; }

		[BelongsTo(Column = "RecipientId")]
		public virtual Recipient Recipient { get; set; }

		[Property, Description("Когда зарегистрирован")]
		public DateTime RegistredOn { get; set; }

		[Property, Description("Комментарий оператора")]
		public string OperatorComment { get; set; }

		[OneToOne(PropertyRef = "BankPayment")]
		public virtual Payment Payment { get; set; }

		public bool UpdatePayerInn { get; set; }

		public void RegisterPayment()
		{
			RegistredOn = DateTime.Now;
		}

		public string GetWarning()
		{
			if (Payer != null
				|| PayerClient == null)
				return "";

			var payers = GetPayerForInn(PayerClient.Inn);
			if (payers.Count == 0) {
				return String.Format("Не удалось найти ни одного плательщика с ИНН {0}", PayerClient.Inn);
			}
			else if (payers.Count == 1) {
				Payer = payers.Single();
				return "";
			}
			else {
				return String.Format("Найдено более одного плательщика с ИНН {0}, плательщики с таким ИНН {1}",
					PayerClient.Inn,
					payers.Implode(p => p.Name));
			}
		}

		public static List<BankPayment> Parse(string file)
		{
			using (var stream = File.OpenRead(file)) {
				return Parse(file, stream);
			}
		}

		public virtual List<Client> GetPayerForInn(string INN)
		{
			return ActiveRecordLinq.AsQueryable<Client>().Where(p => p.LawyerPerson.INN == INN).ToList();
		}

		public static List<BankPayment> Parse(string file, Stream stream)
		{
			List<BankPayment> payments;
			if (Path.GetExtension(file).ToLower() == ".txt")
				payments = ParseText(stream);
			else
				payments = ParseXml(stream);

			return Identify(payments).ToList();
		}

		public static IEnumerable<BankPayment> Identify(IEnumerable<BankPayment> payments)
		{
			var recipients = Recipient.Queryable.ToList();
			var ignoredInns = IgnoredInn.Queryable.ToList();
			foreach (var payment in payments) {
				payment.Recipient =
					recipients.FirstOrDefault(r => r.BankAccountNumber == payment.RecipientClient.AccountCode);
				if (payment.Recipient == null)
					continue;

				var inn = payment.PayerClient.Inn;
				if (!ignoredInns.Any(i => String.Equals(i.Inn, inn, StringComparison.InvariantCultureIgnoreCase))) {
					var payer = ActiveRecordLinq.AsQueryable<Client>().FirstOrDefault(p => p.LawyerPerson.INN == inn);
					payment.Payer = payer;
				}

				if (payment.IsDuplicate())
					continue;

				yield return payment;
			}
		}

		public static List<BankPayment> ParseText(Stream file)
		{
			var reader = new StreamReader(file, Encoding.GetEncoding(1251));
			string line;
			var payments = new List<BankPayment>();
			while ((line = reader.ReadLine()) != null) {
				if (line.Equals("СекцияДокумент=Платежное поручение", StringComparison.CurrentCultureIgnoreCase)) {
					var payment = ParsePayment(reader);
					payments.Add(payment);
				}
			}
			return payments;
		}

		public static BankPayment ParsePayment(StreamReader reader)
		{
			string line;
			var payment = new BankPayment();
			payment.PayerClient = new BankClient();
			payment.PayerBank = new BankInfo();
			payment.RecipientBank = new BankInfo();
			payment.RecipientClient = new BankClient();
			while ((line = reader.ReadLine()) != null) {
				if (line.Equals("КонецДокумента", StringComparison.CurrentCultureIgnoreCase))
					break;

				if (!line.Contains("="))
					continue;
				var parts = line.Split('=');
				if (parts.Length < 2)
					continue;
				var label = parts[0];
				var value = parts[1];
				if (label.Equals("ДатаПоступило", StringComparison.CurrentCultureIgnoreCase)) {
					payment.PayedOn = DateTime.ParseExact(value, "dd.MM.yyyy", CultureInfo.CurrentCulture);
				}
				else if (label.Equals("Сумма", StringComparison.CurrentCultureIgnoreCase)) {
					payment.Sum = decimal.Parse(value, CultureInfo.InvariantCulture);
				}
				else if (label.Equals("НазначениеПлатежа", StringComparison.CurrentCultureIgnoreCase)) {
					payment.Comment = value;
				}
				else if (label.Equals("Номер", StringComparison.CurrentCultureIgnoreCase)) {
					payment.DocumentNumber = value;
				}
				else if (label.Equals("ПлательщикСчет", StringComparison.CurrentCultureIgnoreCase)) {
					payment.PayerClient.AccountCode = value;
				}
				else if (label.Equals("Плательщик1", StringComparison.CurrentCultureIgnoreCase)) {
					payment.PayerClient.Name = value;
				}
				else if (label.Equals("ПлательщикИНН", StringComparison.CurrentCultureIgnoreCase)) {
					payment.PayerClient.Inn = value;
				}
				else if (label.Equals("ПлательщикБИК", StringComparison.CurrentCultureIgnoreCase)) {
					payment.PayerBank.Bic = value;
				}
				else if (label.Equals("ПлательщикБанк1", StringComparison.CurrentCultureIgnoreCase)) {
					payment.PayerBank.Description = value;
				}
				else if (label.Equals("ПлательщикКорсчет", StringComparison.CurrentCultureIgnoreCase)) {
					payment.PayerBank.AccountCode = value;
				}
				else if (label.Equals("ПолучательСчет", StringComparison.CurrentCultureIgnoreCase)) {
					payment.RecipientClient.AccountCode = value;
				}
				else if (label.Equals("Получатель1", StringComparison.CurrentCultureIgnoreCase)) {
					payment.RecipientClient.Name = value;
				}
				else if (label.Equals("ПолучательИНН", StringComparison.CurrentCultureIgnoreCase)) {
					payment.RecipientClient.Inn = value;
				}
				else if (label.Equals("ПолучательБИК", StringComparison.CurrentCultureIgnoreCase)) {
					payment.RecipientBank.Bic = value;
				}
				else if (label.Equals("ПолучательБанк1", StringComparison.CurrentCultureIgnoreCase)) {
					payment.RecipientBank.Description = value;
				}
				else if (label.Equals("ПолучательКорсчет", StringComparison.CurrentCultureIgnoreCase)) {
					payment.RecipientBank.AccountCode = value;
				}
			}
			return payment;
		}

		public static List<BankPayment> ParseXml(Stream file)
		{
			var doc = XDocument.Load(file);
			var payments = new List<BankPayment>();
			foreach (var node in doc.XPathSelectElements("//payment")) {
				var documentNumber = node.XPathSelectElement("NDoc").Value;
				var dateNode = node.XPathSelectElement("SendDate");
				if (dateNode == null)
					continue;
				var sum = node.XPathSelectElement("Summa").Value;
				var comment = node.XPathSelectElement("AssignPayment").Value;

				//если платеж из банка России (ЦБ) то у него нет корсчета
				var bankAccountantCode = "";
				if (node.XPathSelectElement("BankPayer/AccountCode") != null)
					bankAccountantCode = node.XPathSelectElement("BankPayer/AccountCode").Value;

				var payment = new BankPayment {
					DocumentNumber = documentNumber,
					PayedOn =
						DateTime.Parse(dateNode.Value, CultureInfo.GetCultureInfo("ru-RU")),
					RegistredOn = DateTime.Now,
					Sum = Decimal.Parse(sum, CultureInfo.InvariantCulture),
					Comment = comment,
					PayerBank = new BankInfo(
						node.XPathSelectElement("BankPayer/Description").Value,
						node.XPathSelectElement("BankPayer/BIC").Value,
						bankAccountantCode),
					PayerClient = new BankClient(
						node.XPathSelectElement("Payer/Name").Value,
						null,
						node.XPathSelectElement("Payer/AccountCode").Value),
					RecipientBank = new BankInfo(
						node.XPathSelectElement("BankRecipient/Description").Value,
						node.XPathSelectElement("BankRecipient/BIC").Value,
						node.XPathSelectElement("BankRecipient/AccountCode").Value),
					RecipientClient = new BankClient(
						node.XPathSelectElement("Recepient/Client/Name").Value,
						node.XPathSelectElement("Recepient/Client/INN").Value,
						node.XPathSelectElement("Recepient/Client/AccountCode").Value),
				};
				var element = node.XPathSelectElement("Payer/INN");
				if (element != null)
					payment.PayerClient.Inn = element.Value;

				payments.Add(payment);
			}

			return payments;
		}

		public class BankInfo
		{
			[Property, Description("Описание")]
			public string Description { get; set; }

			[Property, Description("БИК")]
			public string Bic { get; set; }

			[Property, Description("Номер счета")]
			public string AccountCode { get; set; }

			public BankInfo()
			{
			}

			public BankInfo(string description, string bic, string accountCode)
			{
				Description = description;
				Bic = bic;
				AccountCode = accountCode;
			}
		}

		public class BankClient
		{
			[Property, Description("ИНН")]
			public string Inn { get; set; }

			[Property, Description("Название")]
			public string Name { get; set; }

			[Property, Description("Номер счета")]
			public string AccountCode { get; set; }

			public BankClient()
			{
			}

			public BankClient(string name, string inn, string accountCode)
			{
				Name = name;
				Inn = inn;
				AccountCode = accountCode;
			}
		}

		[ValidateSelf]
		public void Validate(ErrorSummary summary)
		{
			if (Payer != null) {
				if (Recipient.Id != Payer.Recipient.Id)
					summary.RegisterErrorMessage(
						"Recipient",
						string.Format("Получатель платежей '{0}' плательщика должен соответствовать получателю платежей выбранном в платеже", Payer.Recipient.Name));
			}
			if (Recipient.BankAccountNumber != "40702810602000758601")
				summary.RegisterErrorMessage("Recipient", "Получатель платежей может быть только Инфорум");
		}

		private bool IsDuplicate()
		{
			if (Payer == null)
				return false;

			return Queryable.FirstOrDefault(p => p.Payer == Payer
				&& p.PayedOn == PayedOn
				&& p.Sum == Sum
				&& p.DocumentNumber == DocumentNumber) != null;
		}

		public void DoUpdate()
		{
			UpdateInn();
		}

		public void UpdateInn()
		{
			if (!UpdatePayerInn)
				return;

			if (Payer != null
				&& Payer.LawyerPerson != null
				&& PayerClient != null
				&& !String.IsNullOrEmpty(PayerClient.Inn)) {
				Payer.LawyerPerson.INN = PayerClient.Inn;
				//Payer.Save();
			}
		}
	}
}