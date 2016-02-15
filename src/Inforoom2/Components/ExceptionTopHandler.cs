using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Inforoom2.Components
{
	public static class ExceptionTopHandler
	{
		public static string GetExceptionTextMessage(Exception exception)
		{
			if (exception is HttpException) return "Данный адрес не существует.";
			if (exception is FormatException) return "Неверно задан формат значения.";
			return exception.Message;
		}
	}
}