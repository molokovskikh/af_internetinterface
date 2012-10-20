using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Reflection;
using Castle.Core.Smtp;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;
using Castle.MonoRail.Framework.Internal;
using Castle.MonoRail.Framework.Services;
using Castle.MonoRail.Views.Brail;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using IgorO.ExposedObjectProject;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Rhino.Mocks;
using Test.Support;
using ContactType = InternetInterface.Models.ContactType;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class EmailFixture : IntegrationFixture
	{
		[SetUp]
		public void SetUp()
		{
			BaseMailer.ViewEngineManager = ForTest.GetViewManager();
		}

		[Test]
		public void Lawyer_person_user_write_off_sender()
		{
			var sended = false;
			MailMessage message = null;
			var sender = MockRepository.GenerateStub<IEmailSender>();
			sender.Stub(s => s.Send(message)).IgnoreArguments()
				.Repeat.Any()
				.Callback(new Delegates.Function<bool, MailMessage>(m => {
					message = m;
					sended = true;
					return true;
				}));

			var client = ClientHelper.CreateLaywerPerson();
			var writeOff = new UserWriteOff(client, 500, "testComment");
			writeOff.Sender = sender;
			session.Save(client);
			session.Save(writeOff);

			Flush();

			Assert.IsTrue(sended);
			Assert.That(message.Body, Is.StringContaining("testComment"));
			Assert.That(message.Subject, Is.StringContaining("Списание для Юр.Лица."));
			Assert.That(message.Body, Is.StringContaining("Зарегистрировано разовое списание для Юр.Лица."));
		}
	}

	[TestFixture, Ignore("Чинить")]
	public class MailerFixture : IntegrationFixture
	{
		private Mailer mailer;

		private MailMessage _message;

		[SetUp]
		public void Setup()
		{
			_message = null;
			mailer = Prepare(m => _message = m);
		}

		public static Mailer Prepare(Action<MailMessage> action)
		{
			MailMessage dummy = null;
			var sender = MockRepository.GenerateStub<IEmailSender>();
			sender.Stub(s => s.Send(dummy)).IgnoreArguments()
				.Repeat.Any()
				.Callback(new Delegates.Function<bool, MailMessage>(m => {
					action(m);
					return true;
				}));

			return new Mailer(sender) {
				UnderTest = true,
				SiteRoot = "https://stat.ivrn.net/ii"
			};
		}

		private MailMessage message
		{
			get
			{
				if (_message == null) {
					mailer.Send();
					Assert.That(_message, Is.Not.Null, "Ничего не отправили");
				}
				return _message;
			}
		}

		[Test]
		public void Mail_invoice()
		{
			var client = new Client() {
				LawyerPerson = new LawyerPerson {
					Name = "ООО Рога и Копыта"
				},
				Contacts = {
					new Contact { Type = ContactType.Email, Text = "test@analit.net" }
				}
			};
			var invoice = new Invoice(client, DateTime.Today.ToPeriod(), new List<WriteOff> {
				new WriteOff(client, 500)
			});

			mailer.Invoice(invoice);

			Assert.That(message.To.ToString(), Is.EqualTo("test@analit.net"));
			Assert.That(message.From.ToString(), Is.EqualTo("internet@ivrn.net"));
			Assert.That(message.Subject, Is.StringContaining("Счет за "));
			Assert.That(message.Body, Is.StringContaining("Примите счет"));
		}
	}
}