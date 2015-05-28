using System.Linq;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional.Personal
{
	class ProfileFixture : PersonalFixture
	{
		[Test(Description = "Проверка вывода суммы для разблокировки в ЛК у заблокированного клиента")]
		public void CheckUnlockPriceForClient()
		{
			// Заблокировать "нормального" клиента
			Client.Balance = -10m;
			Client.SetStatus(StatusType.NoWorked, DbSession);
			DbSession.Update(Client);
			DbSession.Flush();

			// Перезагрузить страницу профиля клиента и найти нужную фразу о сумме разблокировки
			Open("Personal/Profile");
			var baseString = "Ваш лицевой счет заблокирован за неуплату, для разблокировки необходимо внести {0} руб.";
			var baseSum = Client.GetUnlockPrice();  // Начальная сумма для разблокировки
			AssertText(string.Format(baseString, baseSum.ToString("F2")));

			// Создать и активировать "Аренду коммутатора" для клиента
			var newHardware = DbSession.Query<RentalHardware>()
				.FirstOrDefault(h => h.Name == "Коммутатор") ?? new RentalHardware {Name = "Коммутатор"};
			if (newHardware.Price == 0m) {
				newHardware.Price = 150m;
				DbSession.Save(newHardware);
			}
			var hardwarePrice = newHardware.Price;  // Общая цена арендуемого оборудования
			var clientHardware = new ClientRentalHardware {
				Hardware = newHardware,
				Client = Client,
				ModelName = "test model #1",
				SerialNumber = "12345"
			};
			clientHardware.Activate(DbSession);
			DbSession.Save(clientHardware);
			// Создать и активировать "Аренду роутера" для клиента
			newHardware = DbSession.Query<RentalHardware>()
				.FirstOrDefault(h => h.Name == "Роутер") ?? new RentalHardware {Name = "Роутер"};
			if (newHardware.Price == 0m) {
				newHardware.Price = 100m;
				DbSession.Save(newHardware);
			}
			hardwarePrice += newHardware.Price;
			clientHardware = new ClientRentalHardware {
				Hardware = newHardware,
				Client = Client,
				ModelName = "test model #2",
				SerialNumber = "54321"
			};
			clientHardware.Activate(DbSession);
			DbSession.Save(clientHardware);
			DbSession.Flush();

			// Перезагрузить страницу профиля клиента и найти нужную фразу о сумме разблокировки
			Open("Personal/Profile");
			AssertText(string.Format(baseString, (baseSum + hardwarePrice).ToString("F2")));
		}

		[Test(Description = "Проверка вывода суммы 1-го платежа для разблокировки в ЛК у нового клиента")]
		public void CheckPaymentSumForDisabledClient()
		{
			var clientMark = ClientCreateHelper.ClientMark.disabledClient.GetDescription();
			var client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Comment == clientMark);
			Assert.IsNotNull(client, "Искомый клиент не найден");

			LoginForClient(client);
			var baseString = "Для начала работы внесите первый платеж {0} руб.";
			AssertText(string.Format(baseString, client.Plan.Price.ToString("F2")));
		}
	}
}
