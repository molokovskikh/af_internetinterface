﻿using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models.Services
{
	[Subclass(0, ExtendsType = typeof(Service), DiscriminatorValue = "WorkLawyer")]
	public class WorkLawyer : Service
	{
	}
}