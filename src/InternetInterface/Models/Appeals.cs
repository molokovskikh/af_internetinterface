﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Helpers;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
    public enum AppealType
    {
        User = 1,
        System = 3
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
           var appeal = HttpUtility.HtmlEncode(Appeal).Replace("\r\n", "<br/>");
            return AppealHelper.TnasformRedmineToLink(appeal);
        }
	}
}