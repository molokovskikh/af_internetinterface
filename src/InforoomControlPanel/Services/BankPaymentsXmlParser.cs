using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Linq;

namespace InforoomControlPanel.Services
{
	public class BankPaymentsXmlParser
	{
		public static List<BankPayment> Parse(ISession dbSession, string file)
		{
			using (var stream = File.OpenRead(file)) return Parse(dbSession, file, stream);
		}

		public static List<BankPayment> Parse(ISession dbSession, string file, Stream stream)
		{
			List<BankPayment> payments;
			if (Path.GetExtension(file).ToLower() == ".txt")
				payments = ParseText(stream);
			else
				payments = ParseXml(stream);

			return Identify(dbSession, payments).ToList();
		}

		public static IEnumerable<BankPayment> Identify(ISession dbSession, IEnumerable<BankPayment> payments)
		{
			var recipients = dbSession.Query<Recipient>().ToList();
			var ignoredInns = new List<IgnoredInn>(); // dbSession.Query<IgnoredInn>().ToList(); /// перенес, но после обнаружил, что в старой админке записи из БД не подтягиваются 
			// вероятно, это часть незаконченного функционала, поэтому на данный момент создаю пустой список)
			foreach (var payment in payments) {
				payment.Recipient =
					recipients.FirstOrDefault(r => r.BankAccountNumber == payment.RecipientAccountCode);
				if (payment.Recipient == null)
					continue;

				var inn = payment.PayerInn;
				if (!ignoredInns.Any(i => String.Equals(i.Inn, inn, StringComparison.InvariantCultureIgnoreCase))) {
					var payer = dbSession.Query<Client>().FirstOrDefault(p => p.LegalClient.Inn == inn);
					payment.Payer = payer;
				}

				if (dbSession.Query<BankPayment>().Any(p => p.Payer == payment.Payer
				                                            && p.PayedOn == payment.PayedOn
				                                            && p.Sum == payment.Sum
				                                            && p.DocumentNumber == payment.DocumentNumber))
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
					payment.PayerAccountCode = value;
				}
				else if (label.Equals("Плательщик1", StringComparison.CurrentCultureIgnoreCase)) {
					payment.PayerName = value;
				}
				else if (label.Equals("ПлательщикИНН", StringComparison.CurrentCultureIgnoreCase)) {
					payment.PayerInn = value;
				}
				else if (label.Equals("ПлательщикБИК", StringComparison.CurrentCultureIgnoreCase)) {
					payment.PayerBankBic = value;
				}
				else if (label.Equals("ПлательщикБанк1", StringComparison.CurrentCultureIgnoreCase)) {
					payment.PayerBankDescription = value;
				}
				else if (label.Equals("ПлательщикКорсчет", StringComparison.CurrentCultureIgnoreCase)) {
					payment.PayerBankAccountCode = value;
				}
				else if (label.Equals("ПолучательСчет", StringComparison.CurrentCultureIgnoreCase)) {
					payment.RecipientAccountCode = value;
				}
				else if (label.Equals("Получатель1", StringComparison.CurrentCultureIgnoreCase)) {
					payment.RecipientName = value;
				}
				else if (label.Equals("ПолучательИНН", StringComparison.CurrentCultureIgnoreCase)) {
					payment.RecipientInn = value;
				}
				else if (label.Equals("ПолучательБИК", StringComparison.CurrentCultureIgnoreCase)) {
					payment.RecipientBankBic = value;
				}
				else if (label.Equals("ПолучательБанк1", StringComparison.CurrentCultureIgnoreCase)) {
					payment.RecipientBankDescription = value;
				}
				else if (label.Equals("ПолучательКорсчет", StringComparison.CurrentCultureIgnoreCase)) {
					payment.RecipientBankAccountCode = value;
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

				var payment = new BankPayment
				{
					DocumentNumber = documentNumber,
					PayedOn =
						DateTime.Parse(dateNode.Value, CultureInfo.GetCultureInfo("ru-RU")),
					RegistredOn = DateTime.Now,
					Sum = Decimal.Parse(sum, CultureInfo.InvariantCulture),
					Comment = comment,
					PayerBankDescription =
						node.XPathSelectElement("BankPayer/Description").Value,
					PayerBankBic = node.XPathSelectElement("BankPayer/BIC").Value,
					PayerBankAccountCode = bankAccountantCode,
					PayerName = node.XPathSelectElement("Payer/Name").Value,
					PayerAccountCode = node.XPathSelectElement("Payer/AccountCode").Value,
					RecipientBankDescription = node.XPathSelectElement("BankRecipient/Description").Value,
					RecipientBankBic = node.XPathSelectElement("BankRecipient/BIC").Value,
					RecipientBankAccountCode = node.XPathSelectElement("BankRecipient/AccountCode").Value,
					RecipientName = node.XPathSelectElement("Recepient/Client/Name").Value,
					RecipientInn = node.XPathSelectElement("Recepient/Client/INN").Value,
					RecipientAccountCode = node.XPathSelectElement("Recepient/Client/AccountCode").Value,
				};
				var element = node.XPathSelectElement("Payer/INN");
				if (element != null)
					payment.PayerInn = element.Value;

				payments.Add(payment);
			}

			return payments;
		}
	}
}