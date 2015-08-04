using System;
using System.ComponentModel;
using System.Net;
using Inforoom2.Helpers;
using Inforoom2.validators;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{


	[Class(0, Table = "TvChannels", NameType = typeof(TvChannel))]
	public class TvChannel : PriorityModel
	{

		[Property, NotNullNotEmpty, Description("Наименование TV-канала")]
		public virtual string Name { get; set; }

		[ManyToOne,NotNull]
		public virtual TvProtocol TvProtocol { get; set; }	

		[Property,Description("Адрес"),NotNullNotEmpty]
		public virtual string Url { get; set; }

		[Property, Description("Порт"),ValidatorNumberic(0,ValidatorNumberic.Type.Greater)]
		public virtual int? Port { get; set; }

		[Property, Description("Маркер, отражающий, доступен ли канал полюзователю или нет")]
		public virtual bool Enabled { get; set; }

		public TvChannel()
		{
		}

		/// <summary>
		/// Генерирует код для файла плейлиста с расширением *.m3u
		/// </summary>
		/// <returns>Содержимое файла</returns>
		public virtual string GenerateM3uCode()
		{
			var result = "#EXTINF:-1 tvg-name=\"{0}\",{0}\n#EXTVLCOPT:deinterlace=-1\n#EXTVLCOPT:deinterlace-mode=yadif2x\n#EXTVLCOPT:udp-caching=1000\n{1}://@{2}:{3}\n";

			result = String.Format(result, Name, TvProtocol.Name, Url, Port);
			return result;
		}
	}
}