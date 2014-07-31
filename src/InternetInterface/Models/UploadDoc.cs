using System.IO;
using System.Web;
using Castle.ActiveRecord;
using NPOI.SS.Formula.Functions;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "Internet")]
	public class UploadDoc
	{
		public UploadDoc()
		{
		}

		public UploadDoc(HttpPostedFile file)
		{
			Filename = file.FileName;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Filename { get; set; }

		[BelongsTo]
		public virtual ClientService AssignedService { get; set; }

		public string GetFilePath(AppConfig config)
		{
			return Path.Combine(config.DocPath, Id.ToString());
		}

		public void SaveFile(HttpPostedFile file, AppConfig config)
		{
			file.SaveAs(GetFilePath(config));
		}
	}
}