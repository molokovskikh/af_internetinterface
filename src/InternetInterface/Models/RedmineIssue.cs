using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Boo.Lang.Compiler.Ast;
using Castle.ActiveRecord;
using Common.Web.Ui.MonoRailExtentions;
using NHibernate.Criterion;
using NPOI.SS.Formula.Functions;

namespace InternetInterface.Models
{
	/// <summary>
	/// Задача в redmine
	/// </summary>
	[ActiveRecord("issues", Schema = "Redmine", Lazy = true)]
	public class RedmineIssue : ChildActiveRecordLinqBase<RedmineIssue>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateGreaterThanZero]
		public virtual int project_id { get; set; }

		[Property, ValidateGreaterThanZero]
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

		[Property, ValidateGreaterThanZero]
		public virtual int lft { get; set; }

		[Property, ValidateGreaterThanZero]
		public virtual int rgt { get; set; }

		[Property]
		public virtual int root_id { get; set; }
		public RedmineIssue()
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

		public RedmineIssue(string name, string body) : this()
		{
			subject = name;
			description = body;
		}
	}
}