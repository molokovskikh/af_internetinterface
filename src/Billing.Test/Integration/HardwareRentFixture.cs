using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Models;
using NHibernate;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	public class HardwareRentFixture : MainBillingFixture
	{
		private RentalHardware _hardware;

		/// <summary>
		/// Добавление нового оборудования в аренду клиенту
		/// </summary>
		private void AddRentalHardwareToClient(bool isActivated = true)
		{
			// Для примера в качестве арендуемого оборудования взят коммутатор
			_hardware = ActiveRecordMediator<RentalHardware>.FindAllByProperty("Name", "Коммутатор").FirstOrDefault() ??
			            new RentalHardware {Name = "Коммутатор"};
			_hardware.Price = (_hardware.Price == 0m) ? 150m : _hardware.Price;
			_hardware.FreeDays = 30;
			ActiveRecordMediator.SaveAndFlush(_hardware);

			// Создать и активировать услугу "Аренда оборудования" для клиента
			var clientHardware = new ClientRentalHardware
			{
				Hardware = _hardware,
				Client = client,
				ModelName = "test model",
				SerialNumber = "12345"
			};
			if (isActivated) {
				clientHardware.Activate();
			}
			else {
				clientHardware.Activate();
				clientHardware.Deactivate();
			}

			ActiveRecordMediator.SaveAndFlush(clientHardware);
		}

		[Test(Description = "Проверка формирования списаний за аренду оборудования у заблокированного клиента")]
		public void Writeoff_pay_for_hardware_rent()
		{
			using (new SessionScope()) {
				//одно оборудование (раннее) должно быть деактивироованным (чтобы проверить верность списаний с учетом архива)
				AddRentalHardwareToClient(false);
				AddRentalHardwareToClient();
				ActiveRecordMediator.Refresh(client);

				// Изменить текущий баланс и статус клиента
				client.PhysicalClient.Balance = 1000m;
				client.SetStatus(ActiveRecordMediator<Status>.FindByPrimaryKey((uint) StatusType.NoWorked));
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
		                    " когда клиент более 1 сутки находится в минусе и при этом пользуется арендованным оборудованием")
		]
		public void Create_redmine_issue_due_to_hardware_rent_debt()
		{
			using (new SessionScope()) {
				AddRentalHardwareToClient();
				ActiveRecordMediator.Refresh(client);

				// Установить статус заблокирован десять дней назад
				var status = ActiveRecordMediator<Status>.FindByPrimaryKey((uint) 7);
				client.SetStatus(status);
				client.PaidDay = false;
				client.StatusChangedOn = SystemTime.Now().AddDays(-10);
				ActiveRecordMediator.UpdateAndFlush(client);

				//После непосредственной блокировки не должно быть задачи в редмайне, так как еще не прошел месяц
				billing.ProcessWriteoffs();
				ActiveRecordMediator.Refresh(client);
				var redmineIssues = ActiveRecordMediator<RedmineIssue>.FindAll().ToList();
				var clientIssues =
					redmineIssues.Where(ri => ri.subject.Contains(client.Id.ToString("D5")) && ri.status_id != 5).ToList();
				Assert.That(clientIssues.Count, Is.EqualTo(0), "Задачи в редмайн быть не должно");

				//Теперь какбудто клиент уже давно заблокирован
				client.StatusChangedOn = SystemTime.Now().AddMonths(-2);
				client.PaidDay = false;
				ActiveRecordMediator.UpdateAndFlush(client);
				billing.ProcessWriteoffs();

				//Должна появиться задача в редмайн, так как бесплатные дни аренды закончились
				ActiveRecordMediator.Refresh(client);
				redmineIssues = ActiveRecordMediator<RedmineIssue>.FindAll().ToList();
				clientIssues =
					redmineIssues.Where(ri => ri.subject.Contains(client.Id.ToString("D5")) && ri.status_id != 5).ToList();
				Assert.That(clientIssues.Count, Is.EqualTo(1), "Нет задачи в Redmine");

				//Проверяем, что при повторном запуске биллинга не будет дублирования задачи
				client.PaidDay = false;
				ActiveRecordMediator.UpdateAndFlush(client);
				billing.ProcessWriteoffs();
				ActiveRecordMediator.Refresh(client);
				redmineIssues = ActiveRecordMediator<RedmineIssue>.FindAll().ToList();
				clientIssues =
					redmineIssues.Where(ri => ri.subject.Contains(client.Id.ToString("D5")) && ri.status_id != 5).ToList();
				Assert.That(clientIssues.Count, Is.EqualTo(1), "Дублирование задачи в Redmine");
			}
		}
	}
}