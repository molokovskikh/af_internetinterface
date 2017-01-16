using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Intefaces;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using FlushMode = NHibernate.FlushMode;

namespace Inforoom2.Models
{
	[Class(0, Table = "ClientEndpoints", Schema = "internet", NameType = typeof (ClientEndpoint)),
	 Description("Точка подключения")]
	public class ClientEndpoint : BaseModel, ILogAppeal
	{
		public ClientEndpoint()
		{
			StaticIpList = new List<StaticIp>();
			LeaseList = new List<Lease>();
			WarningShow = true;
		}

		[Property]
		public virtual bool? IsEnabled { get; set; }

		[Property]
		public virtual bool Disabled { get; set; }

		[Property(Column = "PackageId"), Description("PackageId")]
		public virtual int? PackageId { get; set; }

		[ManyToOne(Column = "Client", Cascade = "save-update")]
		public virtual Client Client { get; set; }

		[Bag(0, Table = "Leases")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "Endpoint")]
		[OneToMany(2, ClassType = typeof (Lease))]
		public virtual IList<Lease> LeaseList { get; set; }

		[Bag(0, Table = "StaticIps", OrderBy = "Ip", Cascade = "all-delete-orphan", Lazy = CollectionLazy.True),
		 Description("Статические Ip")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "EndPoint")]
		[OneToMany(2, ClassType = typeof (StaticIp))]
		public virtual IList<StaticIp> StaticIpList { get; set; }

		[ManyToOne(Column = "Switch"), Description("Коммутатор")]
		public virtual Switch Switch { get; set; }

		[Property, Description("Порт")]
		public virtual int Port { get; set; }

		[Property, Description("Мониторинг")]
		public virtual bool Monitoring { get; set; }

		[Property, Description("Автоприсвоение фиксированного Ip")]
		public virtual bool? IpAutoSet { get; set; }

		[Property]
		public virtual int? ActualPackageId { get; set; }

        [Property]
        public virtual string Mac { get; set; }

        [Property(Column = "Ip", TypeType = typeof (IPUserType)), Description("Фиксированный Ip")]
		public virtual IPAddress Ip { get; set; }

		public virtual void UpdateActualPackageId(int? packageId)
		{
			ActualPackageId = packageId;
		}

		[Property]
		public virtual int? StableTariffPackageId { get; set; }

		[Property]
		public virtual bool WarningShow { get; set; }

		[ManyToOne(Column = "Pool", Cascade = "save-update"), Description("Ip-пул")]
		public virtual IpPool Pool { get; set; }

		public static ClientEndpoint GetEndpointForIp(string ipstr, ISession session)
		{
			var lease = Lease.GetLeaseForIp(ipstr, session);
			if (lease != null && lease.Endpoint != null)
				return lease.Endpoint;

			var ips = session.Query<StaticIp>().ToList();
			ClientEndpoint endpoint = null;
			try {
				var address = IPAddress.Parse(ipstr);
				endpoint = ips.Where(ip =>
				{
					if (ip.Ip == address.ToString())
						return true;
					if (ip.Mask != null) {
						var subnet = SubnetMask.CreateByNetBitLength(ip.Mask.Value);
						if (address.IsInSameSubnet(IPAddress.Parse(ip.Ip), subnet))
							return true;
					}
					return false;
				}).Select(s => s.EndPoint).FirstOrDefault(s => !s.Disabled);
			}
			catch (Exception e) {
				EmailSender.SendDebugInfo("Не удалось распарсить ip: " + ipstr, e.ToString());
				endpoint = null;
			}

			return endpoint;
		}

		public virtual Client GetAppealClient(ISession session)
		{
			return Client;
		}

		public virtual List<string> GetAppealFields()
		{
			return new List<string>()
			{
				"PackageId",
				"Switch",
				"Port",
				"Pool",
				"Disabled"
			};
		}

		public virtual string GetAdditionalAppealInfo(string property, object oldPropertyValue, ISession session)
		{
			string message = "";
			// для свойства Tariff
			if (property == "Switch") {
				// получаем псевдоним из описания 
				property = this.GetDescription("Switch");
				var oldPlan = oldPropertyValue == null ? null : ((Switch) oldPropertyValue);
				var currentPlan = this.Switch;
				if (oldPlan != null) {
					message += property + " было: " + oldPlan.Name + " <br/>";
				}
				else {
					message += property + " было: " + "значение отсуствует <br/> ";
				}
				if (currentPlan != null) {
					message += property + " стало: " + currentPlan.Name + " <br/>";
				}
				else {
					message += property + " стало: " + "значение отсуствует <br/> ";
				}
			}
			if (property == "Disabled") {
				bool state = false;
				bool.TryParse(oldPropertyValue.ToString(), out state);
				if (state != Disabled && Disabled == true) {
					message += "Применена деактивация точки подключения " + this.Id;
				}
				if (state != Disabled && Disabled == false) {
					message += "Отменена деактивация точки подключения " + this.Id;
				}
			}
			if (property == "Pool") {
				// получаем псевдоним из описания 
				property = this.GetDescription("Pool");
				var oldPlan = oldPropertyValue == null ? null : ((IpPool) oldPropertyValue);
				var currentPlan = Pool;
				if (oldPlan != null)
				{
					session.FlushMode = FlushMode.Never;
					var description = session.Query<IpPoolRegion>().FirstOrDefault(s => s.IpPool.Id == oldPlan.Id);
					message += property + " было: " + (description != null ? description.Description : "") + " <br/>";
					session.FlushMode = FlushMode.Auto;
				}
				else {
					message += property + " было: " + "значение отсуствует <br/> ";
				}
				if (currentPlan != null)
				{
					session.FlushMode = FlushMode.Never;
					var description = session.Query<IpPoolRegion>().FirstOrDefault(s => s.IpPool.Id == currentPlan.Id);
					message += property + " стало: " + (description != null ? description.Description : "") + " <br/>";
					session.FlushMode = FlushMode.Auto;
				}
				else {
					message += property + " стало: " + "значение отсуствует <br/> ";
				}
			}
			return message;
		}
		public virtual void SetStablePackgeId(int? packageId)
		{
			PackageId = packageId;
			StableTariffPackageId = packageId;
		}
	}
}