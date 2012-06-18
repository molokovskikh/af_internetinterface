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
	public class UserInfoControllerFixture : BaseControllerTest
	{
		private UserInfoController controller;
		private ISession session;
		private SessionScope scope;
		private ISessionFactoryHolder sessionHolder;

		[SetUp]
		public void Setup()
		{
			controller = new UserInfoController();
			PrepareController(controller);

			scope = new SessionScope();
			sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			session = sessionHolder.CreateSession(typeof(ActiveRecordBase));

			controller.DbSession = session;
		}

		[TearDown]
		public void TearDown()
		{
			sessionHolder.ReleaseSession(session);
			scope.Dispose();
		}

		[Test]
		public void Delete_all_endpoint_on_client_dissolve()
		{
			var statuses = session.Query<Status>().ToArray();
			var worked = statuses.First(s => s.Type == StatusType.Worked);
			var dissolved = statuses.First(s => s.Type == StatusType.Dissolved);
			var commutator = session.Query<NetworkSwitches>().First();

			var client = ClientHelper.Client();
			client.Status = worked;
			var endpoint = new ClientEndpoints(client, 1, commutator);
			client.Endpoints.Add(endpoint);
			var house = new House("Студенческая", 12);
			session.Save(client);
			session.Save(house);

			session.Flush();
			controller.EditInformation(client.Id, dissolved.Id, null, house.Id, AppealType.All, null, new ClientFilter(), false);
			CheckValidationError(client.PhysicalClient);

			session.Flush();
			session.Clear();
			endpoint = session.Get<ClientEndpoints>(endpoint.Id);
			Assert.That(endpoint, Is.EqualTo(null));
		}

		private void CheckValidationError(object item)
		{
			var errors = controller.Validator.GetErrorSummary(item);
			if (errors != null && errors.ErrorsCount > 0)
				Assert.Fail(errors.ErrorMessages.Select((s, i) => Tuple.Create(errors.InvalidProperties[i], s)).Implode());
		}
	}
}