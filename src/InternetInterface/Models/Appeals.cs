using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models.Universal;
using NHibernate;

namespace InternetInterface.Models
{
	public enum AppealType
	{
		All = 0,
		User = 1,
		System = 3,
		FeedBack = 5
	}


	public static class HiberWorker
	{
		private static ISession _currentSession;
		private static ISessionFactoryHolder _holder;

		private static ISession CurrentSession
		{
			get
			{
				if (_currentSession == null) {
					_holder = ActiveRecordMediator.GetSessionFactoryHolder();
					_currentSession = _holder.CreateSession(typeof (ActiveRecordBase));
				}
				return _currentSession;
			}
		}

		private static T Do<T>(Func<ISession, T> action)
		{
			T result;
			try {
				result = action(CurrentSession);
			}
			finally {
				_holder.ReleaseSession(_currentSession);
			}
			return result;
		}

		public static T GetObject<T>(object id)
		{
			return Do(s => s.Get<T>(id));
		}
	}


	[ActiveRecord("Appeals", Schema = "internet", Lazy = true)]
	public class Appeals : ValidActiveRecordLinqBase<Appeals>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Appeal { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		[BelongsTo("Partner")]
		public virtual Partner Partner { get; set; }

		[BelongsTo("Client")]
		public virtual Client Client { get; set; }

		[Property]
		public virtual int AppealType { get; set; }

		public virtual string GetTransformedAppeal()
		{
			return AppealHelper.GetTransformedAppeal(Appeal);
		}

		public static void CreareAppeal(string message, Client client, AppealType type)
		{
			new Appeals
			{
				Appeal = message,
				Client = client,
				AppealType = (int)type,
				Date = DateTime.Now,
				Partner = InitializeContent.Partner
			}.Save();
		}

		public virtual string Type()
		{
			var type = string.Empty;
			if (AppealType == (int)Models.AppealType.User)
				type = "Пользовательское";
			if (AppealType == (int)Models.AppealType.System)
				type = "Системное";
			return type;
		}
	}
}