using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Inforoom2.Helpers
{
	/// <summary>
	/// Объект для сопряжения работы Сервисов и отрабатываемых контроллеров 
	/// </summary>
	public class ControllerAndServiceMediator
	{
		public ControllerAndServiceMediator()
		{
		}

		public ControllerAndServiceMediator(string urlCurrent)
		{
			UrlCurrent = urlCurrent;
		}

		public string UrlRedirectController { get; set; }
		public string UrlRedirectAction { get; set; }
		public string UrlCurrent { get; private set; }
	}
}