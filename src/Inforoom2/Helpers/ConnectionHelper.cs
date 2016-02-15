using System;
using System.Linq;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Linq;

namespace Inforoom2.Helpers
{
	public class ConnectionHelper
	{
		public string Port { get; set; }
		public int? Pool { get; set; }
		public int Switch { get; set; }
		public int Brigad { get; set; }
		public string StaticIp { get; set; }
		public bool Monitoring { get; set; }
		public int PackageId { get; set; }

		public IpPool GetPool(ISession dbSession)
		{
			return Pool.HasValue ? dbSession.Query<IpPool>().FirstOrDefault(s => s.Id == Pool.Value) : null;
		}

		public PackageSpeed GetPackageSpeed(ISession dbSession)
		{
			return dbSession.Query<PackageSpeed>().FirstOrDefault(s => s.PackageId == PackageId);
		}

		public Switch GetSwitch(ISession dbSession)
		{
			return dbSession.Query<Switch>().FirstOrDefault(s => s.Id == Switch);
		}

		public int? GetPortNumber()
		{
			int number = -1;
			Int32.TryParse(this.Port, out number);
			return number != -1 ? (int?) number : null;
		}

		public string Validate(ISession dbSession, bool register, int endpointId = 0)
		{
			var switchItem = dbSession.Query<Switch>().FirstOrDefault(s => s.Id == Switch);
			if (string.IsNullOrEmpty(this.Port) && !register)
				return "Введите порт";
			if (switchItem == null && !register)
				return "Выберете коммутатор";
			int res = -1;
			if (Int32.TryParse(this.Port, out res)) {
				if (res > switchItem.PortCount || res < 0)
					return "Порт должен быть в пределах от 0 до " + switchItem.PortCount;
				if (switchItem.Endpoints.Any(s => s.Port == res && s.Id != endpointId))
					return "Такая пара порт/коммутатор уже существует";
			}
			return string.Empty;
		}
	}
}