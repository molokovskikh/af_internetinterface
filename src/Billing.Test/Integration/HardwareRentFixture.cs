using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Models;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	public class HardwareRentFixture : MainBillingFixture
	{
		private RentalHardware _hardware;

		/// <summary>
		/// Добавление нового оборудования в аренду клиенту
		/// </summary>
		private void AddRentalHardwareToClient()
		{
			// Для примера в качестве арендуемого оборудования взят коммутатор
			_hardware = ActiveRecordMediator<RentalHardware>.FindAllByProperty("Name", "Коммутатор")
					.FirstOrDefault() ?? new RentalHardware { Name = "Коммутатор"};
			_hardware.Price = (_hardware.Price == 0m) ? 150m : _hardware.Price;
			_hardware.FreeDays = 30;
			ActiveRecordMediator.SaveAndFlush(_hardware);

			// Создать и активировать услугу "Аренда оборудования" для клиента
			var clientHardware = new ClientRentalHardware {
				Hardware = _hardware,
				Client = client,
				ModelName = "test model",
				SerialNumber = "12345"
			};
			clientHardware.Activate();
			ActiveRecordMediator.SaveAndFlush(clientHardware);
		}

		[Test(Description = "Проверка формирования списаний за аренду оборудования у заблокированного клиента")]
		public void Writeoff_pay_for_hardware_rent()
		{
			using (new SessionScope()) {
				AddRentalHardwareToClient();
				ActiveRecordMediator.Refresh(client);

				// Изменить текущий баланс и статус клиента
				client.PhysicalClient.Balance = 1000m;
				client.SetStatus(ActiveRecordMediator<Status>.FindByPrimaryKey((uint)StatusType.NoWorked));
				ActiveRecordMediator.UpdateAndFlush(client);

				for (var i = -10; i < 30; i++) {
					var oldBalance = client.Balance;
					// Сбросить флаг клиента "день оплачен" и обработать списания
					client.PaidDay = false;
					ActiveRecordMediator.UpdateAndFlush(client);
					SystemTime.Now = () => DateTime.Now.AddDays(30 + i); // Дни меняются от 20-го до 59-го
					billing.ProcessWriteoffs();

					// Проверить списания и баланс клиента
					ActiveRecordMediator.Refresh(client);
					var writeoffs = client.WriteOffs
						.Where(w => w.Comment != null && w.Comment.Contains("ежедневная плата за аренду")).ToList();
					// С 20-го до 29-й день (от текущей даты) списаний за аренду быть не должно
					if (i < 0) {
						Assert.That(writeoffs.Count, Is.EqualTo(0), "\nwriteoffs.Count != 0!");
					}
					else {
						// С 30-го дня и далее (от текущей даты) должно формироваться 1 списание за аренду ежедневно
						Assert.That(writeoffs.Count, Is.EqualTo(i + 1), "\nНеверное число списаний!");
						var internetPay = client.GetSumForRegularWriteOff(); // Оплата, всегда вычитаемая из баланса
						var rentalHardwarePay = client.GetPriceForHardware(_hardware);
						Assert.AreEqual(rentalHardwarePay + internetPay, oldBalance - client.Balance);
					}
				}
			}
		}

		[Test(Description = "Проверка формирования задачи в RedMine в ситуации," +
		                    " когда клиент более 30 дней находится в минусе и при этом пользуется арендованным оборудованием")]
		public void Create_redmine_issue_due_to_hardware_rent_debt()
		{
			using (new SessionScope()) {
				AddRentalHardwareToClient();
				ActiveRecordMediator.Refresh(client);

				// Установить первоначальный баланс клиента
				client.PhysicalClient.Balance = 10m;
				ActiveRecordMediator.UpdateAndFlush(client.PhysicalClient);

				// Создать 2 списания + 1 платеж для клиента и обработать в Billing
				var writeoff1 = client.PhysicalClient.WriteOff(50m);
				writeoff1.WriteOffDate = DateTime.Now.AddDays(-20);
				writeoff1.BeforeWriteOffBalance = client.Balance;
				writeoff1.Comment = "WriteOff #1";
				ActiveRecordMediator.SaveAndFlush(writeoff1);
				billing.ProcessWriteoffs();
				var payment = new Payment(client, 120m) {
					BillingAccount = false,
					RecievedOn = DateTime.Now.AddDays(-19),
					PaidOn = DateTime.Now.AddDays(-19),
					Virtual = true,
					Comment = "payment"
				};
				ActiveRecordMediator.SaveAndFlush(payment);
				billing.SafeProcessPayments();
				var writeoff2 = new UserWriteOff(client, 100m, "WriteOff #2") {
					BillingAccount = false,
					Date = DateTime.Now.AddDays(-18)
				};
				ActiveRecordMediator.SaveAndFlush(writeoff2);
				billing.SafeProcessPayments();
				ActiveRecordMediator.Refresh(client);

				for (var i = 1; i <= 30; i++) {
					// Сбросить флаг клиента "день оплачен" и обработать списания
					client.PaidDay = false;
					ActiveRecordMediator.UpdateAndFlush(client);
					SystemTime.Now = () => DateTime.Now.AddDays(i);
					billing.ProcessWriteoffs();

					// Проверить задачи в RedMine, открытые для данного клиента
					ActiveRecordMediator.Refresh(client);
					var redmineIssues = ActiveRecordMediator<RedmineIssue>.FindAll().ToList();
					var clientIssues = redmineIssues
							.Where(ri => ri.subject.Contains(client.Id.ToString("D5")) && ri.status_id != 5).ToList();
					// Первые 12 дней (от текущей даты) открытых задач в RedMine быть не должно
					if (i < 13)
						Assert.IsTrue(clientIssues.Count == 0, "\nЕсть задача в Redmine");
					// Должна быть создана ровно 1 задача в RedMine
					else {
						Assert.IsTrue(clientIssues.Count == 1, "\nredmineIssues = " + clientIssues.Count);
						Assert.IsTrue(clientIssues.Exists(ri => ri.subject.Contains("Возврат оборудования, ЛС")));
					}
				}
			}
		}
	}
}
