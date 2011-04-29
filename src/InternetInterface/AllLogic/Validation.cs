using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using InternetInterface.Models;

namespace InternetInterface.AllLogic
{
	public class Validation
	{
		public static string ValidationConnectInfo(ConnectInfo info)
		{
			if (string.IsNullOrEmpty(info.Port))
				return string.Empty;
			int res;
			if (Int32.TryParse(info.Port, out res))
			{
				if (res > 48 || res < 1)
					return "Порт должен быть в пределах о 1 до 48";
				if (ClientEndpoints.Queryable.Where(e => e.Port == res && e.Switch.Id == info.Switch).Count() == 0)
					return string.Empty;
				else
				{
					return "Такая пара порт/свитч уже существует";
				}
			}
			else
			{
				return "Вы ввели некорректное значение порта";
			}
		}
	}
}