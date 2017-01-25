using System.Linq;
using Common.Tools;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.Admin;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.EmployeeGroups
{
	class EmployeeGroupFixture : AdminFixture
	{
		[Test, Description("Добавление тарифного плана")]
		public void EmployeeGroupAddRemoveFixture()
		{
			Open("EmployeeGroup");
			var groupNameA = "Белгород";
			var groupNameB = "Борисоглебск";
			var groupNameC = "РИЦ";

			var inputText = browser.FindElementByCssSelector("[name='name']");
			inputText.Clear();
			inputText.SendKeys(groupNameA);
			browser.FindElementByCssSelector("button[type='submit']").Click();

			inputText = browser.FindElementByCssSelector("[name='name']");
			inputText.Clear();
			inputText.SendKeys(groupNameB);
			browser.FindElementByCssSelector("button[type='submit']").Click();

			inputText = browser.FindElementByCssSelector("[name='name']");
			inputText.Clear();
			inputText.SendKeys(groupNameC);
			browser.FindElementByCssSelector("button[type='submit']").Click();

			var createdGroups = DbSession.Query<EmployeeGroup>().ToList();
			Assert.IsTrue(createdGroups.Count == 3);
			foreach (var item in createdGroups) {
				AssertText(item.Name);
			}

			browser.FindElementByCssSelector($"a[href='/EmployeeGroup/GroupDelete/{createdGroups.First().Id}']").Click();
			var inputDialogMessage = browser.FindElementByCssSelector("[id='messageConfirmationText']");
			Assert.IsTrue(inputDialogMessage.Text ==
				$"Вы действительно хотите удалить группу №{createdGroups[0].Id} {createdGroups[0].Name} ?");
			browser.FindElementByCssSelector("[id='messageConfirmationLink']").Click();

			AssertText("Группа успешно удалена.");

			AssertNoText(createdGroups[0].Name);
			for (int i = 1; i < createdGroups.Count; i++) {
				AssertText(createdGroups[i].Name);
			}
			createdGroups = DbSession.Query<EmployeeGroup>().ToList();

			Assert.IsTrue(createdGroups.Count == 2);
			Assert.IsTrue(createdGroups.First().EmployeeList.Count == 0);

			browser.FindElementByCssSelector($"a[href='/EmployeeGroup/GroupEdit/{createdGroups.First().Id}']").Click();
			var nameTag = "New";

			inputText = browser.FindElementByCssSelector("[name='name']");
			inputText.Clear();
			inputText.SendKeys(createdGroups.First().Name + nameTag);
			browser.FindElementByCssSelector("[value='Изменить']").Click();

			var employees = DbSession.Query<Employee>().Where(s => !s.IsDisabled).OrderBy(s => s.Id).ToList();
			for (int i = 0; i < (employees.Count > 4 ? 4 : employees.Count); i++) {
				WaitForCss("#EmployeeDropDown");
				Css("#EmployeeDropDown").SelectByText(employees[i].Name);
				browser.FindElementByCssSelector("[value='Добавить']").Click();
			}

			Open("EmployeeGroup");
			createdGroups = DbSession.Query<EmployeeGroup>().ToList();
			DbSession.Refresh(createdGroups.First());
			Assert.IsTrue(createdGroups.First().Name.IndexOf(nameTag) != -1);
			Assert.IsTrue(createdGroups.First().EmployeeList.Count == 4);

			browser.FindElementByCssSelector($"a[href='/EmployeeGroup/GroupEdit/{createdGroups.Last().Id}']").Click();
			inputText = browser.FindElementByCssSelector("[name='name']");
			var value = inputText.GetAttribute("value");
			Assert.IsTrue(value == createdGroups.Last().Name && value != createdGroups.First().Name);

			employees = DbSession.Query<Employee>().Where(s => !s.IsDisabled).OrderByDescending(s => s.Id).ToList();
			for (int i = 0; i < (employees.Count > 4 ? 4 : employees.Count); i++) {
				WaitForCss("#EmployeeDropDown");
				Css("#EmployeeDropDown").SelectByText(employees[i].Name);
				browser.FindElementByCssSelector("[value='Добавить']").Click();
			}
			createdGroups = DbSession.Query<EmployeeGroup>().ToList();
			DbSession.Refresh(createdGroups.Last());
			Assert.IsTrue(createdGroups.Last().Name.IndexOf(nameTag) == -1);
			Assert.IsTrue(createdGroups.Last().EmployeeList.Count == 4);

			browser.FindElementByCssSelector(
				$"a[href='/EmployeeGroup/EmployeeToGroupDelete?groupId={createdGroups.Last().Id}&employeeId={employees.First().Id}']")
				.Click();
			browser.FindElementByCssSelector("[id='messageConfirmationLink']").Click();
			DbSession.Refresh(createdGroups.Last());
			Assert.IsTrue(createdGroups.Last().EmployeeList.Count == 3);
			ReportPaymentEmployeeGorupFixture();
		}

		private void ReportPaymentEmployeeGorupFixture()
		{
			var createdGroups = DbSession.Query<EmployeeGroup>().ToList();
			var newPayment = new Payment() {
				Client = DbSession.Query<Client>().First(),
				PaidOn = SystemTime.Now(),
				RecievedOn = SystemTime.Now(),
				Sum = 491.10m,
				BillingAccount = true,
				Employee = createdGroups.First().EmployeeList.First()
			};
			DbSession.Save(newPayment);
			DbSession.Flush();
			Open("SpreadSheet/PaymentsByEmployeeGroups");

			foreach (var group in createdGroups) {
				AssertText(group.Name);
				foreach (var employee in group.EmployeeList) {
					AssertText(employee.Name);
				}
			}
			AssertText("Итого: 0,00 ₽");
			AssertText("Сумма: " + newPayment.Sum.ToString("####"));
			AssertText("Всего: " + newPayment.Sum.ToString("####"));
		}
	}
}