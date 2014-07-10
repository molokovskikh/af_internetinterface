using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Routing;
using Castle.MonoRail.Framework.Services;
using Castle.MonoRail.Framework.Test;
using Common.Tools;
using InternetInterface.Controllers;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Rhino.Mocks;

namespace InternetInterface.Test.Integration.Controllers
{
	[TestFixture]
	public class PrivateMessagesControllerFixture : ControllerFixture
	{
		private PrivateMessagesController _controller;
		private ISendMessage _smsHelper;
		private IList<XDocument> _documents;
		private IList<SmsMessage> _messages = new List<SmsMessage>();

		[SetUp]
		public void Setup()
		{
			_controller = new PrivateMessagesController();
			_documents = new List<XDocument>();

			Prepare(_controller);

			_smsHelper = MockRepository.GenerateStub<ISendMessage>();
			_smsHelper.Stub(s => s.SendMessage(new SmsMessage()))
				.IgnoreArguments()
				.Repeat.Any()
				.Callback(new Delegates.Function<bool, SmsMessage>(m => {
					_messages.Add(m);
					return true;
				}));
		}

		[Test]
		public void SendSmsFormCommutator()
		{
			var zone = new Zone(Generator.Name());
			session.Save(zone);
			var commutator = new NetworkSwitch(Generator.Name(), zone);
			session.Save(commutator);
			var client = ClientHelper.Client(session);
			session.Save(client);
			var endPoint = new ClientEndpoint(client, 1, commutator);
			session.Save(endPoint);
			var contactText = Generator.Name();
			var contact = new Contact(client, ContactType.SmsSending, contactText);
			session.Save(contact);
			client.Contacts = new List<Contact> {
				contact
			};

			var collection = new NameValueCollection {
				{ "smsMessageButton", "true" }
			};
			var request = MockRepository.GenerateStub<IRequest>();
			request.Stub(r => r.Form).Return(collection);
			request.Stub(r => r.HttpMethod).Return("POST");

			var engineContext = MockRepository.GenerateStub<IEngineContext>();
			engineContext.Stub(c => c.Request).Return(request);
			engineContext.Stub(c => c.Services).Return(new StubMonoRailServices());
			_controller.SetEngineContext(engineContext);

			PrivateMessagesController.SmsHelper = _smsHelper;
			_controller.ForTest = true;
			_controller.ForSwitch(commutator.Id);

			Assert.AreEqual(_messages.Count, 1);
			Assert.AreEqual(_messages[0].Text, "Это тестовое сообщение");
		}
	}
}
