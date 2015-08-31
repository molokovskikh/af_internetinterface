using System;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Common.Tools;
using Inforoom2.Helpers;
using Inforoom2.validators;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;
using NHibernate.Validator.Engine;

namespace Inforoom2.Models
{
	[Class(0, Table = "PackageSpeed", NameType = typeof(PackageSpeed))]
	public class PackageSpeed : BaseModel
	{
		[Property]
		public virtual int PackageId { get; set; }

		[Property, ValidatorNotNull]
		public virtual int? Speed { get; set; }

		[Property]
		public virtual bool IsPhysic { get; set; }

		[Property]
		public virtual bool Confirmed { get; set; }

		[Property, ValidatorNotEmpty]
		public virtual string Description { get; set; }

		/// <summary>
		/// Получение скорости в пакета в Мегабитах
		/// </summary>
		public virtual float GetSpeed()
		{
			//Приводим к флоту, так как изначально скорость записывается в байтах
			//После деления, у всех скоростей, что меньше 1 мегабита, будут 0 отображаться.
			var sp = (float)Speed;
			float val = sp == 0 ? 0 : sp / 1000000;
			return val;
		}

		/// <summary>
		/// Добавление задачи на Redmine
		/// </summary>
		/// <param name="dbSession">сессия Хибернейта</param>
		/// <param name="linkToConfirmPackageSpeed">ссылка для подтверждения скорости</param>
		public virtual RedmineTask AssignRedmineTask(ISession dbSession, string linkToConfirmPackageSpeed)
		{
			int assignedToId = 0;
			Int32.TryParse(ConfigHelper.GetParam("SpeedAddingRedmineTaskAssignTo"), out assignedToId);
			int daysForATask = 0;
			Int32.TryParse(ConfigHelper.GetParam("daysForAReadmineTaskAboutSpeed"), out daysForATask);
			string redSubject = string.Format("Добавление новой скорости");
			string redDescription = string.Format(@"Необходимо добавить скорость, указанную по ссылке * {0}, ввести 'PackageId' и подтвердить добавление скорости."
				, linkToConfirmPackageSpeed);

			//новая задача на Redmine
			var redmineTask = new RedmineTask() {
				project_id = 27, // Проект "Интернет интерфейс"
				status_id = 1, // Статус "Новый"
				priority_id = 4, //нормальный приоритет
				author_id = 18, //автор анонимус
				assigned_to_id = assignedToId,
				created_on = SystemTime.Now(),
				due_date = SystemTime.Today().AddDays(daysForATask),
				subject = redSubject,
				description = redDescription
			};
			dbSession.Save(redmineTask);
			return redmineTask;
		}
	}
}