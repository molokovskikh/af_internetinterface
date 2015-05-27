using System;
using System.Linq;
using Inforoom2.Models;
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
	class LogAppealFixture : BaseFixture
	{ 
		[Test, Description("Проверка наличия логов после изменения ФИО в модели клиента")]
		public void CheckFIOEditingForAppeal(){
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
			Assert.That(client.PhysicalClient.Name, Is.StringContaining(newNameForApply));
			Assert.That(client.PhysicalClient.Name, Is.StringContaining(newNameForApply));
			Assert.That(client.PhysicalClient.Name, Is.StringContaining(newNameForApply));
			// проверка Appeal
			var appeal = DbSession.Query<Appeal>().OrderByDescending(s=>s.Id).FirstOrDefault();
			Assert.IsNotNull(appeal,"Логи в БД отсуствуют!");
			Assert.That(appeal.Message, Is.StringContaining(newNameForApply), "Логи не содержат текущее изменение ФИО в модели клиента " + client.Id);
		}

		[Test, Description("Проверка отсуствия логов после изменения нелогируемых полей в модели клиента")]
		public void CheckNoLogEditingForAppeal()
		{
			int newParam =  DateTime.Now.AddYears(-10).Year;
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
			Assert.That(appeal.Message, Is.StringContaining(plan.Name), "Логи не содержат текущее изменение тарифного плана в модели физ.клиента " + client.Id);
		}
	}
}
