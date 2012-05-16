using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord.Framework;
using InternetInterface.Models;
using NUnit.Framework;
using Test.Support;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	class PaymentFixture : IntegrationFixture
	{
		[Test]
		public void Parse_payments()
		{
			var existsPayment = BankPayment.Queryable.FirstOrDefault(p => p.Comment == "Оплата за мониторинг оптового фармрынка за январь по счету №161 от 11.01..2011г Cумма 800-00,без налога (НДС).");
			if (existsPayment != null)
				existsPayment.DeleteAndFlush();

			var file = @"..\..\..\TestData\20110114104609.xml";
			var payments = BankPayment.ParseXml(File.OpenRead(file));
			Assert.That(payments.Count, Is.GreaterThan(0));
			var payment = payments.First();
			Assert.That(payment.Sum, Is.EqualTo(800));
			Assert.That(payment.PayedOn, Is.EqualTo(DateTime.Parse("11.01.2011")));
			Assert.That(payment.Comment, Is.EqualTo("Оплата за мониторинг оптового фармрынка за январь по счету №161 от 11.01..2011г Cумма 800-00,без налога (НДС)."));

			Assert.That(payment.PayerClient.Name, Is.EqualTo("ЗАО ТРИОМЕД"));
			Assert.That(payment.PayerBank.Description, Is.EqualTo("ФИЛИАЛ ОРУ ОАО \"МИНБ\""));

			Assert.That(payment.RecipientClient.Name, Is.EqualTo("\\366601001 ООО\"Аналитический центр\""));
			Assert.That(payment.RecipientBank.Description, Is.EqualTo("ВОРОНЕЖСКИЙ Ф-Л ОАО \"ПРОМСВЯЗЬБАНК\" г ВОРОНЕЖ"));
			Assert.That(payment.Sum, Is.EqualTo(800));
			Assert.That(payment.PayedOn, Is.EqualTo(DateTime.Parse("11.01.2011")));
			Assert.That(payment.Comment, Is.EqualTo("Оплата за мониторинг оптового фармрынка за январь по счету №161 от 11.01..2011г Cумма 800-00,без налога (НДС)."));
		}

		[Test]
		public void Parse_payments_without_bank_account_code()
		{
			var payments = BankPayment.ParseXml(File.OpenRead(@"..\..\..\TestData\201102_21.xml"));
			Assert.That(payments.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Parse_raiffeisen_payments()
		{
			var payments = BankPayment.ParseText(File.OpenRead(@"..\..\..\TestData\1c.txt"));
			Assert.That(payments.Count, Is.GreaterThan(0));
			Assert.That(payments.Count, Is.EqualTo(4));
			var payment = payments.First();
			Assert.That(payment.Sum, Is.EqualTo(3000));
			Assert.That(payment.Comment, Is.EqualTo("Обеспечение доступа ИС услуги за 1 квартал 2011г. оплата по счету N 1815 от 11 января 2011 г. Без НДС"));
			Assert.That(payment.DocumentNumber, Is.EqualTo("18"));
			Assert.That(payment.PayedOn, Is.EqualTo(new DateTime(2011, 1, 27)));
		}

		[Test]
		public void Parser_with_output_payments()
		{
			var file = @"..\..\..\TestData\20110113.xml";
			var payments = BankPayment.ParseXml(File.OpenRead(file));
			Assert.That(payments.Count, Is.GreaterThan(0));
		}


		[Test]
		public void Parse_payment_without_inn()
		{
			var payments = BankPayment.ParseXml(File.OpenRead(@"..\..\..\TestData\\201103_04-16.03.xml"));
			Assert.That(payments.Count, Is.GreaterThan(0));
		}
	}
}
