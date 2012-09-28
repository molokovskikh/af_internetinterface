using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models.Services;
using InternetInterface.Models.Universal;
using InternetInterface.Services;
using NHibernate;

namespace InternetInterface.Models
{
	[ActiveRecord("ClientServices", Schema = "Internet", Lazy = true)]
	public class ClientService
	{
		public ClientService()
		{
			Channels = new List<ChannelGroup>();
		}

		public ClientService(Client client, Service service)
			: this()
		{
			Client = client;
			Service = service;
			if (service.SupportUserAcivation)
				ActivatedByUser = true;
		}

		public ClientService(Client client, Service service, bool activatedByUser)
			: this(client, service)
		{
			ActivatedByUser = activatedByUser;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual Client Client { get; set; }

		[BelongsTo]
		public virtual Service Service { get; set; }

		[Property]
		public virtual DateTime? BeginWorkDate { get; set; }

		[Property]
		public virtual DateTime? EndWorkDate { get; set; }

		[Property]
		public virtual bool Activated { get; set; }

		[Property]
		public virtual bool Diactivated { get; set; }

		[Property, Description("Подключить")]
		public virtual bool ActivatedByUser { get; set; }

		[HasAndBelongsToMany(
			Schema = "Internet",
			Table = "AssignedChannels",
			ColumnKey = "AssignedServiceId",
			ColumnRef = "ChannelGroupId"), Description("Пакеты каналов")]
		public virtual IList<ChannelGroup> Channels { get; set; }

		[BelongsTo]
		public virtual Partner Activator { get; set; }

		[OneToOne(PropertyRef = "Service", Cascade = CascadeEnum.All)]
		public virtual DebtWorkInfo DebtInfo { get; set; }

		private void DeleteFromClient()
		{
			if (Service.CanDelete(this)) {
				//сторока ниже не работает, в тестt ServiceFixture.ActiveDeactive хотя должна, какой то бред
				if (Id == 0)
					Client.ClientServices.Remove(this);
				else
					Client.ClientServices.Remove(Client.ClientServices.First(c => c.Id == Id));
				Client.Save();
			}
		}

		public virtual void Activate()
		{
			if (Service.CanActivate(this))
				Service.Activate(this);
		}

		public virtual void Deactivate()
		{
			if (Service.CanDeactivate(this)) {
				DeleteFromClient();
				//перед деактивацией, услугу нужно удалить из
				//списка услуг клиента тк она может влиять на цену
				Service.CompulsoryDeactivate(this);
			}
		}

		public virtual void CompulsoryDeactivate()
		{
			Service.CompulsoryDeactivate(this);
			DeleteFromClient();
		}

		public virtual decimal GetPrice()
		{
			return Service.GetPrice(this);
		}

		public virtual void PaymentClient()
		{
			Service.PaymentClient(this);
		}

		public virtual void WriteOff()
		{
			Service.WriteOff(this);
		}

		public virtual void UpdateChannels(List<ChannelGroup> channelGroups)
		{
			foreach (var channelGroup in channelGroups.Except(Channels).ToArray()) {
				Channels.Add(channelGroup);
				Client.UserWriteOffs.Add(new UserWriteOff(Client,
					channelGroup.ActivationCost,
					String.Format("Подключение пакета каналов {0}", channelGroup.Name),
					false));
			}
			foreach (var channelGroup in Channels.Except(channelGroups).ToArray()) {
				Channels.Remove(channelGroup);
			}
		}
	}
}