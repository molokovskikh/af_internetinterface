using System;
using System.ComponentModel;
using Common.Tools;
using Inforoom2.validators;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{ 
	/// <summary>
	/// Задачи на Redmine
	/// </summary>
	[Class(0, Table = "issues", Schema = "Redmine", NameType = typeof(RedmineTask))]
	public class RedmineTask : BaseModel
	{
		[Property]
		public virtual int root_id { get; set; }
		public RedmineTask()
		{
			created_on = DateTime.Now;
			updated_on = DateTime.Now;
			lft = 1;
			rgt = 2;
			tracker_id = 2; //Новшество
			status_id = 1; //новый
			priority_id = 4; //нормальный приоритет
			author_id = 18; //анонимус
		}

		public RedmineTask(string name, string body)
			: this()
		{
			subject = name;
			description = body;
		}

		[Property, ValidatorGreaterThanZero]
		public virtual int project_id { get; set; }

		[Property, ValidatorGreaterThanZero]
		public virtual int tracker_id { get; set; }

		[Property]
		public virtual int status_id { get; set; }

		[Property]
		public virtual DateTime created_on { get; set; }

		[Property]
		public virtual DateTime updated_on { get; set; }

		[Property]
		public virtual DateTime due_date { get; set; }

		[Property]
		public virtual string subject { get; set; }

		[Property]
		public virtual string description { get; set; }

		[Property]
		public virtual int assigned_to_id { get; set; }

		[Property]
		public virtual int priority_id { get; set; }

		[Property]
		public virtual int author_id { get; set; }

		[Property, ValidatorGreaterThanZero]
		public virtual int lft { get; set; }

		[Property, ValidatorGreaterThanZero]
		public virtual int rgt { get; set; }

	}
}