using System;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.MonoRail.TestSupport;
using Common.Tools;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class UserInfoControllerFixture : SessionControllerFixture
	{
		private UserInfoController controller;

		[SetUp]
		public void Setup()
		{
			controller = new UserInfoController();
			PrepareController(controller);

			controller.DbSession = session;
		}


		[Test]
		public void Delete_all_endpoint_on_client_dissolve()
		{
			var statuses = session.Query<Status>().ToArray();
			var worked = statuses.First(s => s.Type == StatusType.Worked);
			var dissolved = statuses.First(s => s.Type == StatusType.Dissolved);
			var commutator = new NetworkSwitch("Тестовый коммутатор", session.Query<Zone>().First());
			session.Save(commutator);

			var client = ClientHelper.Client();
			client.Status = worked;
			var endpoint = new ClientEndpoint(client, 1, commutator);
			client.Endpoints.Add(endpoint);
			var house = new House("Студенческая", 12);
			session.Save(client);
			session.Save(house);

			session.Flush();
			controller.EditInformation(client.Id, dissolved.Id, null, house.Id, AppealType.All, null, new ClientFilter());
			CheckValidationError(client.PhysicalClient);

			session.Flush();
			session.Clear();
			endpoint = session.Get<ClientEndpoint>(endpoint.Id);
			Assert.That(endpoint, Is.EqualTo(null));
			session.Clear();
		}

		private void CheckValidationError(object item)
		{
			var errors = controller.Validator.GetErrorSummary(item);
			if (errors != null && errors.ErrorsCount > 0)
				Assert.Fail(errors.ErrorMessages.Select((s, i) => Tuple.Create(errors.InvalidProperties[i], s)).Implode());
		}
	}
}