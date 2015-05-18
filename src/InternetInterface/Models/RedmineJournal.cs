using System;
using Castle.ActiveRecord;

namespace InternetInterface.Models
{
	/// <summary>
	/// Конкретная заметка (комментарий) по задаче в redmine
	/// </summary>
	[ActiveRecord("journals", Schema = "Redmine", Lazy = true)]
	public class RedmineJournal : ChildActiveRecordLinqBase<RedmineJournal>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property(Column = "journalized_id")]
		public virtual uint RedmineIssueId { get; set; }

		[Property(Column = "journalized_type")]
		public virtual string JournalType { get; set; }

		[Property(Column = "user_id")]
		public virtual uint UserId { get; set; }

		[Property]
		public virtual string Notes { get; set; }

		[Property(Column = "created_on")]
		public virtual DateTime? CreateDate { get; set; }

		[Property(Column = "private_notes")]
		public virtual bool IsPrivate { get; set; }
	}
}
