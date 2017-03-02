using System;
using System.Linq;
using Inforoom2.Models;
using Inforoom2.Test.Functional.Account;
using Inforoom2.Test.Infrastructure;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.ClientActions
{
	/// <summary>
	/// "ЭТО ИНТЕГРАЦИОННЫЙ ТЕСТ, КОТОРЫЙ НЕОБХОДИМО ПЕРЕМЕСТИТЬ В НУЖНЫЙ КАТАЛОГ,
	/// для начала необходим класс Базовой интеграционной фикстуры.
	/// 
	/// Тестирование логов
	/// </summary>
	internal class LogAppealFixture : BaseFixture
	{
		[Test, Description("Проверка наличия логов после изменения ФИО в модели клиента")]
		public void CheckFIOEditingForAppeal()
		{
			const string newNameForApply = "NewNameForApply";
			const string newSurnameForApply = "NewSurnameForApply";
			const string newPatronymicForApply = "NewPatronymicForApply";

			var client = DbSession.Query<Client>().FirstOrDefault();
			Assert.That(client.PhysicalClient.Name, Is.Not.StringContaining(newNameForApply));
			Assert.That(client.PhysicalClient.Name, Is.Not.StringContaining(newNameForApply));
			Assert.That(client.PhysicalClient.Name, Is.Not.StringContaining(newNameForApply));
			client.PhysicalClient.Name = newNameForApply;
			client.PhysicalClient.Surname = newSurnameForApply;
			client.PhysicalClient.Patronymic = newPatronymicForApply;
			DbSession.Update(client);
			DbSession.Flush();
			Assert.That(client.PhysicalClient.Name, Does.Contain(newNameForApply));
			Assert.That(client.PhysicalClient.Name, Does.Contain(newNameForApply));
			Assert.That(client.PhysicalClient.Name, Does.Contain(newNameForApply));
			// проверка Appeal
			var appeal = DbSession.Query<Appeal>().OrderByDescending(s => s.Id).FirstOrDefault();
			Assert.IsNotNull(appeal, "Логи в БД отсуствуют!");
			Assert.That(appeal.Message, Does.Contain(newNameForApply), "Логи не содержат текущее изменение ФИО в модели клиента " + client.Id);
		}

		[Test, Description("Проверка отсуствия логов после изменения нелогируемых полей в модели клиента")]
		public void CheckNoLogEditingForAppeal()
		{
			int newParam = DateTime.Now.AddYears(-10).Year;
			var client = DbSession.Query<Client>().FirstOrDefault();
			Assert.That(client.PhysicalClient.BirthDate.Year, Is.Not.EqualTo(newParam));
			client.PhysicalClient.BirthDate = DateTime.Now.AddYears(-10);
			DbSession.Update(client);
			DbSession.Flush();
			Assert.That(client.PhysicalClient.BirthDate.Year, Is.EqualTo(newParam));
			// проверка Appeal
			var appeal = DbSession.Query<Appeal>().OrderByDescending(s => s.Id).FirstOrDefault();
			Assert.IsNotNull(appeal, "Логи в БД отсуствуют!");
			Assert.That(appeal.Message, Is.Not.StringContaining(newParam.ToString()), "Логи содержат данные о нелогируемых полях!" + client.Id);
		}

		[Test, Description("Проверка наличия логов после изменения тарифного плана в модели физ.клиента")]
		public void CheckTariffEditingForAppeal()
		{
			var client = DbSession.Query<Client>().FirstOrDefault();
			var plan = DbSession.Query<Plan>().FirstOrDefault(s => s != client.Plan);
			Assert.That(client.PhysicalClient.Plan.Id, Is.Not.EqualTo(plan.Id));
			client.PhysicalClient.Plan = plan;
			DbSession.Update(client);
			DbSession.Flush();
			Assert.That(client.PhysicalClient.Plan.Id, Is.EqualTo(plan.Id));
			// проверка Appeal
			var appeal = DbSession.Query<Appeal>().OrderByDescending(s => s.Id).FirstOrDefault();
			Assert.IsNotNull(appeal, "Логи в БД отсуствуют!");
			Assert.That(appeal.Message, Does.Contain(plan.Name), "Логи не содержат текущее изменение тарифного плана в модели физ.клиента " + client.Id);
		}

		[Test, Description("Проверка наличия логов после изменения адреса в модели физ.клиента")]
		public void CheckAddressEditingForAppeal()
		{
			var client = DbSession.Query<Client>().FirstOrDefault();
			var addressHouse = DbSession.Query<House>().FirstOrDefault(s=>s.Id!=client.PhysicalClient.Address.House.Id);
			Assert.That(client.PhysicalClient.Address.Id, Is.Not.EqualTo(addressHouse.Id));
			client.PhysicalClient.Address.House = addressHouse; 
			DbSession.Update(client);
			DbSession.Flush();
			Assert.That(client.PhysicalClient.Address.House.Id, Is.EqualTo(addressHouse.Id));
			// проверка Appeal
			var appeal = DbSession.Query<Appeal>().OrderByDescending(s => s.Id).FirstOrDefault();
			Assert.IsNotNull(appeal, "Логи в БД отсуствуют!");
			Assert.That(appeal.Message, Does.Contain(addressHouse.Number), "Логи не содержат текущее изменение тарифного плана в модели физ.клиента " + client.Id);
		}

		[Test, Description("Проверка наличия логов после изменения тарифного плана в модели физ.клиента")]
		public void CheckNewsEditingForAppeal()
		{
			string newTitle = "Weeeee";
			var newBlock = DbSession.Query<NewsBlock>().FirstOrDefault();
			Assert.That(newBlock.Title, Is.Not.EqualTo(newTitle));
			newBlock.Title = newTitle;
			DbSession.Update(newBlock);
			DbSession.Flush();
			Assert.That(newBlock.Title, Is.EqualTo(newTitle));
			// проверка Appeal
			var log = DbSession.Query<Log>().OrderByDescending(s => s.Id).FirstOrDefault();
			Assert.IsNotNull(log, "Логи в БД отсуствуют!");
			Assert.That(log.Message, Does.Contain(newTitle), "Логи не содержат текущее изменение  наименовании плана в модели информационного блока " + newBlock.Id);
		}
	}
}