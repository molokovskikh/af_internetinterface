using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Xml.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Core.Smtp;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Routing;
using Castle.MonoRail.Framework.Services;
using Castle.MonoRail.Framework.Test;
using Castle.MonoRail.TestSupport;
using Common.Tools;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Test.Integration;
using NHibernate;
using NUnit.Framework;
using Rhino.Mocks;

namespace InternetInterface.Test.Helpers
{
	[TestFixture]
	public class ControllerFixture : BaseControllerTest
	{
		protected SessionScope scope;
		protected ISession session;
		private string referer = "http://www.ivrn.net/";
		private ISessionFactoryHolder sessionHolder;
		protected MailMessage _message;
		protected IEmailSender _sender;
		protected bool sended;
		protected XDocument smsResponse;
		private FakeSmsHelper fakeSmsHelper;

		[SetUp]
		public void SetupFixture()
		{
			Open();

			_sender = MockRepository.GenerateStub<IEmailSender>();
			_sender.Stub(s => s.Send(_message)).IgnoreArguments()
				.Repeat.Any()
				.Callback(new Delegates.Function<bool, MailMessage>(m => {
					_message = m;
					sended = true;
					return true;
				}));

			BaseMailerExtention.SenderForTest = _sender;

			smsResponse = new XDocument(
				new XElement("data",
					new XElement("code", "1"),
					new XElement("descr", "Операция успешно завершена"),
					new XElement("detail", null)));
			fakeSmsHelper = new FakeSmsHelper(smsResponse);
			fakeSmsHelper.SaveSms  = false;
		}

		protected void Open()
		{
			scope = new SessionScope();
			sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
		}

		[TearDown]
		public void TearDown()
		{
			Close();
		}

		protected void Close()
		{
			if (session != null) {
				sessionHolder.ReleaseSession(session);
				session = null;
			}
			scope.SafeDispose();
			scope = null;
		}

		protected List<string> sms
		{
			get
			{
				return fakeSmsHelper.Requests.Select(r => r.ToString()).ToList();
			}
		}

		protected void Prepare(BaseController controller)
		{
			controller.DbSession = session;
			var baseConstoller = controller as InternetInterfaceController;
			if (baseConstoller != null) {
				baseConstoller.SmsHelper = fakeSmsHelper;
			}
			PrepareController(controller);
		}

		protected override IMockResponse BuildResponse(UrlInfo info)
		{
			return new StubResponse(
				info,
				new DefaultUrlBuilder(),
				new StubServerUtility(),
				new RouteMatch(),
				referer);
		}
	}
}
