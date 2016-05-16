using System.IO;
using System.Linq;
using Common.Tools;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure.Helpers;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.Payments
{
	internal class PaymentsFixture : ControlPanelBaseFixture
	{
		private Client CurrentClient;

		[SetUp]
		//в начале 
		public void Setup()
		{
			//получаем обычного (нормально) клиента
			CurrentClient =
				DbSession.Query<Client>()
					.FirstOrDefault(s => s.Comment == ClientCreateHelper.ClientMark.legalClient.GetDescription());
			//добавляем ему контакт
			CurrentClient.Contacts.Add(new Contact() {
				ContactString = "9102868651",
				Type = ContactType.SmsSending,
				Client = CurrentClient
			});
			CurrentClient.Recipient = DbSession.Query<Recipient>().FirstOrDefault();
			//сохраняем
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			//получаем обновленную модель клиента
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient); 
		}

		[Test, Description("Страница клиента. Физ. лицо. Вывод личной информации")]
		public void PaymentsManagement()
		{
			PaymentAdd();
			PaymentEdit();
			PaymentRemove();

			//AssertText("Платеж успешно добавлен и ожидает обработки");

			////Зачислить как бонус
			//var inputObj = browser.FindElementByCssSelector(blockNamePaymentsAdd + "input[name='isBonus']");
			//Assert.That(inputObj.GetAttribute("checked"), Is.Null, "Зачислить как бонус - не совпадает с должным.");
			//inputObj.Click();

			//browser.FindElementByCssSelector(blockNamePaymentsAdd + ".btn.btn-success").Click();

			//Assert.That(CurrentClient.Balance, Is.LessThan(clientBalance + sum), "Баланс клиента не совпадает с должным.");

			//RunBillingProcessPayments(CurrentClient);
			//DbSession.Refresh(CurrentClient.PhysicalClient);

			//Assert.That(CurrentClient.Balance, Is.GreaterThan(clientBalance + sum), "Баланс клиента не совпадает с должным.");
			//AssertText("Платеж успешно добавлен и ожидает обработки");

		}
		
		public void PaymentAdd()
		{
			//обновляем страницу клиента
			Open("Payments/PaymentList");
			string blockName = ".Payments.PaymentList ";
			browser.FindElementByCssSelector(blockName + "#addPayment").Click();
			blockName = ".Payments.PaymentCreate ";
			ClosePreviousTab();
			WaitForVisibleCss(blockName);
      DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);
			var currentBalance = CurrentClient.Balance;
			var paymentSum = 500;
			var docNumber = 1;
			//ЛС-клиента
			var inputObj = browser.FindElementByCssSelector(blockName + "input[name='bankPayment.Payer.Id']");
			WaitForVisibleCss(blockName + "input[name='bankPayment.Payer.Id']");
			inputObj.Clear();
			inputObj.SendKeys(CurrentClient.Id.ToString());
			WaitAjax(10);
			browser.FindElementByCssSelector(blockName + "#clientReciverMessage strong").Click();
			WaitAjax(10);
			//Сумма
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='bankPayment.Sum']");
			WaitForVisibleCss(blockName + "input[name='bankPayment.Sum']");
			inputObj.Clear();
			inputObj.SendKeys(paymentSum.ToString());
			//№ документа
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='bankPayment.DocumentNumber']");
			WaitForVisibleCss(blockName + "input[name='bankPayment.DocumentNumber']");
			inputObj.Clear();
			inputObj.SendKeys(docNumber.ToString());
			browser.FindElementByCssSelector(blockName + ".btn.btn-success").Click();

			RunBillingProcessPayments(CurrentClient);
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);

			Assert.That(CurrentClient.Balance, Is.EqualTo(currentBalance + paymentSum), "Баланс клиента не совпадает с должным.");
			//нет возможности сохранять без инн
		}
		public void PaymentEdit(int paymentSumNew = 333)
		{
			Open("Payments/PaymentList");
			string blockName = ".Payments.PaymentList ";
			var currentBalance = CurrentClient.Balance;
			var docNumber = 2;
			//	var anotherRecipient = DbSession.Query<Recipient>().FirstOrDefault(s=>s.Id != CurrentClient.Recipient.Id);
			var paymentSum = CurrentClient.Payments.OrderByDescending(s => s.Id).First().Sum;
			browser.FindElementByCssSelector(blockName + "table a.btn.btn-green:first-of-type").Click();
			blockName = ".Payments.PaymentEdit ";
			ClosePreviousTab();
			WaitForVisibleCss(blockName);

			//ЛС-клиента
			var inputObj = browser.FindElementByCssSelector(blockName + "input[name='bankPayment.Payer.Id']");
			WaitForVisibleCss(blockName + "input[name='bankPayment.Payer.Id']");
			inputObj.Clear();
			WaitAjax(10);
			//Сумма
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='bankPayment.Sum']");
			WaitForVisibleCss(blockName + "input[name='bankPayment.Sum']");
			inputObj.Clear();
			inputObj.SendKeys(paymentSumNew.ToString());
			//№ документа
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='bankPayment.DocumentNumber']");
			WaitForVisibleCss(blockName + "input[name='bankPayment.DocumentNumber']");
			inputObj.Clear();
			inputObj.SendKeys(docNumber.ToString());
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();
			//нет возможности сохранять без инн
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='bankPayment.Payer.Id']");
			WaitForVisibleCss(blockName + "input[name='bankPayment.Payer.Id']");
			inputObj.Clear();
			inputObj.SendKeys(CurrentClient.Id.ToString());
			WaitAjax(10);
			browser.FindElementByCssSelector(blockName + "#clientReciverMessage strong").Click();
			WaitAjax(10);
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();

			RunBillingProcessPayments(CurrentClient);
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);

			Assert.That(CurrentClient.Balance, Is.EqualTo(currentBalance - (paymentSum - paymentSumNew)), "Баланс клиента не совпадает с должным.");

		}
		public void PaymentRemove()
		{
			//обновляем страницу клиента
			Open("Payments/PaymentList");
			string blockName = ".Payments.PaymentList ";
			var currentBalance = CurrentClient.Balance;
			var paymentSum = CurrentClient.Payments.OrderByDescending(s=>s.Id).First().Sum;
			WaitForVisibleCss(blockName + "table a.btn.btn-red");
			browser.FindElementByCssSelector(blockName + "table a.btn.btn-red").Click(); 
	 
			RunBillingProcessPayments(CurrentClient);
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);

			Assert.That(CurrentClient.Balance, Is.EqualTo(currentBalance - paymentSum), "Баланс клиента не совпадает с должным.");
			//нет возможности сохранять без инн
		}


		[Test, Description("Пакетное добавление платежей")]
		public void TempPaymentsAdd()
		{
			TempPaymentListAdd();
      TempPaymentEdit();
			TempPaymentRemove();
			TempPaymentEdit();
      TempPaymentsSave();
			TempPaymentAfterSaveEdit();
			TempPaymentAfterSaveRemove();
		}

		public void TempPaymentListAdd()
		{
			//обновляем страницу клиента
			Open("Payments/PaymentList");
			string blockName = ".Payments.PaymentList ";
			browser.FindElementByCssSelector(blockName + "#loadPayment").Click();
			blockName = ".Payments.PaymentProcess ";
			ClosePreviousTab();
			WaitForVisibleCss(blockName);
      browser.FindElementByCssSelector(blockName + ".fileUpload input").SendKeys(Path.GetFullPath("./Assets/paymentsForTest_txt.txt"));
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue").Click();
			WaitForVisibleCss(blockName);
			AssertText("всего 24 платежей");
			//	browser.FindElementByCssSelector(blockName + ".form-group form #loadPayment").Click();
			//	WaitForVisibleCss(blockName);
			browser.FindElementByCssSelector(blockName + ".form-group form [name='paymentsClean']").Click();
			WaitForVisibleCss(blockName);
			AssertText("всего 0 платежей");
			browser.FindElementByCssSelector(blockName + ".fileUpload input").SendKeys(Path.GetFullPath("./Assets/paymentsForTest_xml.xml"));
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue").Click();
			WaitForVisibleCss(blockName);
			AssertText("всего 15 платежей");
			//етсь возможность сохранять без инн
		}
		
		public void TempPaymentEdit()
		{
			Open("Payments/PaymentProcess");
			string blockName = ".Payments.PaymentProcess ";
			var currentBalance = CurrentClient.Balance;
			var paymentSumNew = 333;
			var docNumber = 2;
	 		browser.FindElementByCssSelector(blockName + "table a.btn.btn-green:first-of-type").Click();
			blockName = ".Payments.EditTemp ";
			ClosePreviousTab();
			WaitForVisibleCss(blockName);

			//ЛС-клиента
			var inputObj = browser.FindElementByCssSelector(blockName + "input[name='bankPayment.Payer.Id']");
			WaitForVisibleCss(blockName + "input[name='bankPayment.Payer.Id']");
			inputObj.Clear();
			WaitAjax(10);
			//Дата
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='bankPayment.PayedOn']");
			WaitForVisibleCss(blockName + "input[name='bankPayment.PayedOn']");
			inputObj.Clear();
			inputObj.SendKeys(SystemTime.Now().ToShortDateString());
			browser.FindElementByCssSelector(blockName + "table").Click();
			//Сумма
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='bankPayment.Sum']");
			WaitForVisibleCss(blockName + "input[name='bankPayment.Sum']");
			inputObj.Clear();
			inputObj.SendKeys(paymentSumNew.ToString());
			//№ документа
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='bankPayment.DocumentNumber']");
			WaitForVisibleCss(blockName + "input[name='bankPayment.DocumentNumber']");
			inputObj.Clear();
			inputObj.SendKeys(docNumber.ToString());
			//нет возможности сохранять без инн
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='bankPayment.Payer.Id']");
			WaitForVisibleCss(blockName + "input[name='bankPayment.Payer.Id']");
			inputObj.Clear();
			inputObj.SendKeys(CurrentClient.Id.ToString());
			WaitAjax(10);
			browser.FindElementByCssSelector(blockName + "#clientReciverMessage strong").Click();
			WaitAjax(10);
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();
			//етсь возможность сохранять без инн
		}

		public void TempPaymentRemove()
		{
			Open("Payments/PaymentProcess");
			string blockName = ".Payments.PaymentProcess ";
			AssertText(CurrentClient.Name);
			browser.FindElementByCssSelector(blockName + "table a.btn.btn-red").Click();
			WaitForVisibleCss(blockName);
			AssertNoText(CurrentClient.Name);
		}

		public void TempPaymentsSave()
		{
			Open("Payments/PaymentProcess");
			string blockName = ".Payments.PaymentProcess ";
			var currentBalance = CurrentClient.Balance;
			var paymentsCount = CurrentClient.Payments.Count;
			browser.FindElementByCssSelector(blockName + "[name='paymentsAdd']").Click();
			RunBillingProcessPayments(CurrentClient);
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);
			var paymentSumNew = CurrentClient.Payments.OrderByDescending(s => s.Id).First().Sum;
			Assert.That(CurrentClient.Balance, Is.EqualTo(currentBalance + paymentSumNew), "Баланс клиента не совпадает с должным.");
			Assert.That(CurrentClient.Payments.Count, Is.EqualTo(paymentsCount+1), "Кол-во платежей клиента не совпадает с должным.");
		}

		public void TempPaymentAfterSaveEdit()
		{ 
			CurrentClient.LegalClient.Inn = "123321";
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			var inn = CurrentClient.LegalClient.Inn;
			var currentBalance = CurrentClient.Balance;
			var paymentsCount = CurrentClient.Payments.Count;
			var paymentSum = CurrentClient.Payments.OrderByDescending(s => s.Id).First().Sum;
			decimal paymentSumNew = paymentSum + (1000 - paymentSum);
			Open("Payments/PaymentList");
			PaymentEdit((int)paymentSumNew);
			RunBillingProcessPayments(CurrentClient);
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);
			Assert.That(CurrentClient.LegalClient.Inn, Is.Not.EqualTo(inn), "Инн клиента не совпадает с должным.");
			Assert.That(CurrentClient.Balance, Is.EqualTo(currentBalance - paymentSum + paymentSumNew), "Баланс клиента не совпадает с должным.");
			Assert.That(CurrentClient.Payments.Count, Is.EqualTo(paymentsCount + 1), "Кол-во платежей клиента не совпадает с должным.");
		}

		public void TempPaymentAfterSaveRemove()
		{
			//todo: Нужно правильно связать Payment и BankPayment, т.к. при изменении их связи теряются 

			//Open("Payments/PaymentList");
			//DbSession.Refresh(CurrentClient);
			//DbSession.Refresh(CurrentClient.LegalClient);
			//var currentBalance = CurrentClient.Balance;
			//var paymentsCount = CurrentClient.Payments.Count;
			//var paymentSum = CurrentClient.Payments.OrderByDescending(s => s.Id).First().Sum;
			//PaymentRemove();
			//RunBillingProcessPayments(CurrentClient);
			//DbSession.Refresh(CurrentClient);
			//DbSession.Refresh(CurrentClient.LegalClient);
			//Assert.That(CurrentClient.Balance, Is.EqualTo(currentBalance - paymentSum), "Баланс клиента не совпадает с должным.");
			//Assert.That(CurrentClient.Payments.Count, Is.EqualTo(paymentsCount-1), "Кол-во платежей клиента не совпадает с должным.");
		}

	}
}