using System;

namespace Inforoom2.Models
{
	public class BillingHistory
	{
		public decimal Sum;
		public DateTime Date;
		public string Comment; // Комментарий того, кто регистрирует платеж
		public string WhoRegistered; // Имя того, кто регистрирует платеж
		public string Description;
	}
}