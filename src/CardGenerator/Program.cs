using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Framework.Config;
using Environment = NHibernate.Cfg.Environment;

namespace CardGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			Init();

			var sumToCount = new Dictionary<decimal, int> {
				{50, 2000},
				{100, 2000},
				{250, 2000},
				{300, 2000},
				{500, 3000},
				{700, 1000},
			};

			var cards = Generate(sumToCount);
			Save(cards);
		}

		private static void Init()
		{
			var configuration = new InPlaceConfigurationSource();
			configuration.PluralizeTableNames = true;
			configuration.Add(typeof (ActiveRecordBase),
				new Dictionary<string, string>
				{
					{Environment.Dialect, "NHibernate.Dialect.MySQLDialect"},
					{Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
					{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
					{Environment.ConnectionStringName, "DB"},
					{Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle" },
					{Environment.Hbm2ddlKeyWords, "none"},
				});
			ActiveRecordStarter.Initialize(
				new [] {typeof (PaymentCard).Assembly},
				configuration);
		}

		private static List<PaymentCard> Generate(Dictionary<decimal, int> sumToCount)
		{
			var cards = new List<PaymentCard>();
			foreach (var pair in sumToCount)
			{
				for(var i = 0; i < pair.Value; i++)
				{
					PaymentCard existsCard;
					PaymentCard card;
					do
					{
						card = new PaymentCard(pair.Key);
						existsCard = cards.FirstOrDefault(c => String.Equals(c.Serial, card.Serial, StringComparison.InvariantCultureIgnoreCase));
					} while (existsCard != null);
					cards.Add(card);
					ActiveRecordMediator<PaymentCard>.Save(card);
				}
			}
			return cards;
		}

		private static void Save(List<PaymentCard> cards)
		{
			foreach (var group in cards.GroupBy(c => c.Sum))
			{
				var name = String.Format("{0}.txt", group.Key.ToString().PadLeft(6, '0'));
				using (var stream = new StreamWriter(File.OpenWrite(name), Encoding.GetEncoding(1251)))
				{
					foreach (var card in group)
					{
						stream.WriteLine("{0}\t{1}", card.Serial, card.Pin);
					}
				}
			}
		}
	}

	[ActiveRecord(Schema = "Internet")]
	public class PaymentCard
	{
		private static Random random = new Random();
		private const string pinChars = "23456789qwertyupasdfghjkzxcvbnm";
		private const string serialChars = "123456789";

		public PaymentCard()
		{}

		public PaymentCard(decimal sum)
		{
			Sum = sum;
			GenerateAt = DateTime.Now;
			Serial = RandomDigits(12, serialChars);
			Pin = RandomDigits(6, pinChars);
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public string Serial { get; set; }

		[Property]
		public string Pin { get; set; }

		[Property]
		public decimal Sum { get; set; }

		[Property]
		public DateTime GenerateAt { get; set; }

		public string RandomDigits(int lenght, string chars)
		{
			if (String.IsNullOrEmpty(chars))
				throw new Exception("Не заданы символы для генерации");

			string result = "";
			while (result.Length <= lenght)
			{
				result += chars[random.Next(0, chars.Length - 1)];
			}
			return result;
		}
	}
}
