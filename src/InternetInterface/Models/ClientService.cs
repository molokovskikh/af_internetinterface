using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models.Services;
using InternetInterface.Models.Universal;
using InternetInterface.Services;
using NHibernate;
using NPOI.SS.Formula.Functions;

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

		[Property("Activated")]
		public virtual bool IsActivated { get; set; }

		[Property("Diactivated")]
		public virtual bool IsDeactivated { get; set; }

		[Property, Description("Подключить")]
		public virtual bool ActivatedByUser { get; set; }

		[Property]
		public virtual bool IsFree { get; set; }

		[HasAndBelongsToMany(
			Schema = "Internet",
			Table = "AssignedChannels",
			ColumnKey = "AssignedServiceId",
			ColumnRef = "ChannelGroupId"), Description("Пакеты каналов")]
		public virtual IList<ChannelGroup> Channels { get; set; }

		[BelongsTo]
		public virtual Partner Activator { get; set; }

		/// <summary>
		/// если эта услуга привязана к точке подключения
		/// например как фиксированный ip адрес мы должны удалить ее вместе с точкой подключения
		/// </summary>
		[BelongsTo]
		public virtual ClientEndpoint Endpoint { get; set; }

		[BelongsTo]
		public virtual RentableHardware RentableHardware { get; set; }

		[Property]
		public virtual string SerialNumber { get; set; }

		[Property]
		public virtual string Model { get; set; }

		[HasMany]
		public virtual IList<UploadDoc> Docs { get; set; }

		private void DeleteFromClient()
		{
			if (Service.CanDelete(this)) {
				//строка ниже не работает, в тесте ServiceFixture.ActiveDeactive хотя должна, какой то бред
				if (Id == 0)
					Client.ClientServices.Remove(this);
				else
					Client.ClientServices.Remove(Client.ClientServices.First(c => c.Id == Id));
				Client.Save();
			}
		}

		public virtual bool TryActivate()
		{
			if (Service.CanActivate(this)) {
				Service.Activate(this);
				return true;
			}
			return false;
		}

		public virtual void TryDeactivate()
		{
			if (Service.CanDeactivate(this)) {
				DeleteFromClient();
				//перед деактивацией, услугу нужно удалить из
				//списка услуг клиента тк она может влиять на цену
				Service.ForceDeactivate(this);
			}
		}

		public virtual void ForceDeactivate()
		{
			Service.ForceDeactivate(this);
			DeleteFromClient();
		}

		public virtual void PaymentProcessed()
		{
			Service.PaymentClient(this);
		}

		public virtual void WriteOffProcessed()
		{
			Service.WriteOff(this);
		}

		public virtual decimal GetPrice()
		{
			if (IsFree)
				return 0;
			return Service.GetPrice(this);
		}

		public virtual void UpdateChannels(List<ChannelGroup> channelGroups)
		{
			foreach (var channelGroup in channelGroups.Except(Channels).ToArray()) {
				Channels.Add(channelGroup);
				Client.UserWriteOffs.Add(new UserWriteOff(Client,
					channelGroup.ActivationCost,
					String.Format("Подключение пакета каналов {0}", channelGroup.Name)));
			}
			foreach (var channelGroup in Channels.Except(channelGroups).ToArray()) {
				Channels.Remove(channelGroup);
			}
		}

		public virtual bool IsService(Service service)
		{
			return NHibernateUtil.GetClass(Service) == NHibernateUtil.GetClass(service);
		}

		public virtual List<RentDocItem> GetDocItems()
		{
			if (RentableHardware == null)
				return Enumerable.Empty<RentDocItem>().ToList();
			return new [] {
					new RentDocItem(String.Format("{0} {1}, серийный № {2}", RentableHardware.Name, Model, SerialNumber), 1)
				}
				.Concat(RentableHardware.DefaultDocItems)
				.ToList();
		}
	}
}