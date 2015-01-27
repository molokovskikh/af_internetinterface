using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Web.Ui.Models.Audit;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	public class ConnectInfo
	{
		public string Port { get; set; }
		public uint? Pool { get; set; }
		public uint Switch { get; set; }
		public uint Brigad { get; set; }
		public string static_IP { get; set; }
		public bool Monitoring { get; set; }
		public uint PackageId { get; set; }
	}

	[ActiveRecord(Schema = "Internet", Table = "LawyerPerson", Lazy = true), Auditable]
	public class LawyerPerson : ValidActiveRecordLinqBase<LawyerPerson>
	{
		public LawyerPerson()
		{
		}

		public LawyerPerson(RegionHouse region)
		{
			Region = region;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property("FullName"), ValidateNonEmpty("Введите полное наименование"), Auditable("Полное наименование")]
		public virtual string Name { get; set; }

		[Property, ValidateNonEmpty("Введите краткое наименование"), Auditable("Краткое наименование")]
		public virtual string ShortName { get; set; }

		[Property, Auditable("Юридический адрес")]
		public virtual string LawyerAdress { get; set; }

		[Property, Auditable("Фактический адрес")]
		public virtual string ActualAdress { get; set; }

		[Property, Auditable("ИНН")]
		public virtual string INN { get; set; }

		[Property, Auditable("Контактное лицо")]
		public virtual string ContactPerson { get; set; }

		public virtual decimal? Tariff
		{
			get
			{
				return client.Orders.Where(o => o.IsActivated && !o.IsDeactivated)
					.SelectMany(o => o.OrderServices)
					.Where(s => s.IsPeriodic)
					.Sum(s => s.Cost);
			}
		}

		[Property]
		public virtual decimal Balance { get; set; }

		[Property]
		public virtual DateTime PeriodEnd { get; set; }

		[Property, Auditable("Почтовый адрес")]
		public virtual string MailingAddress { get; set; }

		[OneToOne(PropertyRef = "LawyerPerson")]
		public virtual Client client { get; set; }

		[BelongsTo("RegionId")]
		public virtual RegionHouse Region { get; set; }

		public virtual bool NeedShowWarning()
		{
			var param = ConfigurationManager.AppSettings["LawyerPersonBalanceWarningRate"];
			var rate = (decimal)float.Parse(param, CultureInfo.InvariantCulture);
			var cond = Balance <= -(Tariff * rate) && Balance < 0;
			return cond;
		}

		public virtual List<WriteOff> Calculate(DateTime dateTime)
		{
			if (PeriodEnd == DateTime.MinValue) {
				PeriodEnd = SystemTime.Today().LastDayOfMonth();
			}

			var results = new List<WriteOff>();
			//списываем деньги за отключенные услуги
			var toDeactivate = client.Orders.Where(o => !o.IsDeactivated && o.OrderStatus == OrderStatus.Disabled).ToArray();
			results.AddRange(toDeactivate
				.SelectMany(s => s.OrderServices)
				.Where(s => s.IsPeriodic)
				.Select(s => new WriteOff(client, s)));
			toDeactivate.Each(o => {
				o.IsDeactivated = true;
				client.CreareAppeal(String.Format("Деактивирован заказ {0}", o.Description));
				var endpoint = o.EndPoint;
				if (endpoint != null) {
					o.EndPoint = null;
					if (!client.Orders.Select(x => x.EndPoint).Contains(endpoint))
						client.Endpoints.Remove(endpoint);
				}
			});

			var toActivate = client.Orders.Where(o => !o.IsActivated && o.OrderStatus == OrderStatus.Enabled).ToArray();
			results.AddRange(toActivate
				.SelectMany(s => s.OrderServices)
				.Where(s => !s.IsPeriodic)
				.Select(s => new WriteOff(client, s)));
			toActivate.Each(o => o.IsActivated = true);

			if (PeriodEnd > dateTime) {
				return results.Where(w => w.Sum > 0).ToList();
			}

			results.AddRange(client.Orders.Where(o => o.OrderStatus == OrderStatus.Enabled)
				.SelectMany(s => s.OrderServices)
				.Where(s => s.IsPeriodic)
				.Select(s => new WriteOff(client, s)));
			PeriodEnd = PeriodEnd.AddMonths(1).LastDayOfMonth();
			return results.Where(w => w.Sum > 0).ToList();
		}

		public virtual Dictionary<string, string> ParseAddress()
		{
			var obj = new Dictionary<string, string>();
			obj["City"] = "--";
			obj["Street"] = "--";
			obj["House"] = "--";
			obj["Apartment"] = "--";
			return obj;
			//Можно парсить адреса юриков но будут ошибки и результат в районе 70%
			IList<string> actual = ActualAdress == null ? new string[0] : ActualAdress.Split(',');
			IList<string> paper = LawyerAdress == null ? new string[0] : LawyerAdress.Split(',');
			if (actual.Count == 0)
				return obj;
			if (actual.Count == 1) {
				obj["City"] = actual[0];
				return obj;
			}
			int digital;
			int.TryParse(actual[0], out digital);
			bool officeFlag = actual[actual.Count - 1].Contains("оф");
			//Стандартный адрес
			if (actual.Count == 3 && !officeFlag) {
				obj["City"] = actual[0];
				obj["Street"] = actual[1];
				obj["House"] = actual[2];
			}

			if (actual.Count == 3 && officeFlag) {
				obj["Street"] = actual[0];
				obj["House"] = actual[1];
			}
			//индекс+стандартный адрес
			if (digital != 0 && actual.Count == 4) {
				obj["City"] = actual[1];
				obj["Street"] = actual[2];
				obj["House"] = actual[3];
			}
			//индекс+область+стандартный адрес
			if (digital != 0 && actual.Count == 5 && actual[1].Contains("обл")) {
				obj["City"] = actual[2];
				obj["Street"] = actual[3];
				obj["House"] = actual[4];
			}
			if (actual.Count == 2) {
				obj["Street"] = actual[0];
				obj["House"] = actual[1];
			}
			return obj;
		}
	}
}