using System.Collections.Generic;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models.Services
{
	[Class(0, Table = "Services", NameType = typeof(Service), DiscriminatorValue = "Name")]
	[Discriminator(Column = "Name", TypeType = typeof(string))]
	public class Service : BaseModel
	{
		[Property(Column = "HumanName",NotNull = true, Unique = true), NotEmpty]
		public virtual string Name { get; set; }

		[Property(Column = "_Description", NotNull = true), NotEmpty]
		public virtual string Description { get; set; }
		
		[Property]
		public virtual decimal Price { get; set; }
		
		[Property]
		public virtual bool BlockingAll { get; set; }

		[Property(Column = "_IsActivableFromWeb")]
		public virtual bool IsActivableFromWeb { get; set; }

		[Bag(0, Table = "ClientServices")]
		[Key(1, Column = "service")]
		[OneToMany(2, ClassType = typeof(ClientService))]
		public virtual IList<ClientService> ServiceClients { get; set; }
		
		/// <summary>
		/// Индикатор управления услугой даже для заблокированного клиента
		/// </summary>
		public virtual bool ProcessEvenInBlock
		{
			get { return false; }
		}

		/// <summary>
		/// Возвращает суммарную цену услуги
		/// </summary>
		public virtual decimal GetPrice(ClientService assignedService)
		{
			return Price;
		}

		/// <summary>
		/// Активирует клиентскую услугу assignedService
		/// </summary>
		public virtual void Activate(ClientService assignedService, ISession session)
		{
		
		}

		/// <summary>
		/// Деактивирует клиентскую услугу assignedService
		/// </summary>
		public virtual void Deactivate(ClientService assignedService, ISession session)
		{
			
		}

		public virtual bool IsActiveFor(ClientService assignedService)
		{
			return assignedService.IsActivated;
		}

		/// <summary>
		/// Проверяет, может ли быть запущен процесс активации клиентской услуги assignedService
		/// </summary>
		public virtual bool CanActivate(ClientService assignedService)
		{
			return true;
		}

		/// <summary>
		/// Проверяет, доступна ли клиенту client услуга для активации 
		/// </summary>
		public virtual bool IsActivableFor(Client client)
		{
			return false;
		}
	}

	public enum ServiceType
	{
		BlockAccount = 1,
		Credit = 2,
		StaticIp = 3,
		PermIp = 4,
		Internet = 5,
		Iptv = 7
	}
}