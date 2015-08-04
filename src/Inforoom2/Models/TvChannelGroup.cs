using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using Inforoom2.Helpers;
using NHibernate.Mapping;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{


	[Class(0, Table = "TvChannelGroups", NameType = typeof(TvChannelGroup))]
	public class TvChannelGroup : BaseModel
	{
		[Property, Description("Наименование группы TV-каналов")]
		public virtual string Name { get; set; }


		[Bag(0, Table = "PlanTvChannelGroups")]
		[Key(1, Column = "TvChannelGroup", NotNull = false)]
		[ManyToMany(2, Column = "Plan", ClassType = typeof(Plan))]
		public virtual IList<Plan> Plans { get; set; }

		[Bag(0, Table = "TvChannelTvChannelGroups", Cascade = "All")]
		[Key(1, Column = "TvChannelGroup", NotNull = false)]
		[ManyToMany(2, Column = "TvChannel", ClassType = typeof(TvChannel))]
		public virtual IList<TvChannel> TvChannels { get; set; }

		public TvChannelGroup()
		{
			TvChannels = new List<TvChannel>();
			Plans = new List<Plan>();
		}

	}
}