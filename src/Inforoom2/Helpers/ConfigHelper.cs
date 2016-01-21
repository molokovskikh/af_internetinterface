using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Web;
using System.Web.Hosting;

namespace Inforoom2.Helpers
{
	/// <summary>
	/// Хелпер для работы с конфигурационными файлами
	/// </summary>
	public class ConfigHelper
	{
		/// <summary>
		/// Получение параметра из файла конфигурации
		/// </summary>
		/// <param name="name">Имя параметра</param>
		/// <returns>Значение параметра</returns>
		public static string GetParam(string name, bool isNullable = false)
		{
			var result = System.Web.Configuration.WebConfigurationManager.AppSettings[name];

			if (result == null && isNullable == false)
				throw new Exception("Не удалось найти параметр {0} в текущем файле кофигурации");

			return result;
		}
	}
}