using System;
using System.Collections.Generic;
using System.Net.Mail;
using Castle.Core.Smtp;
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
}