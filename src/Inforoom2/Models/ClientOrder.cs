using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	//перенесено из старой админки (нужен при проверке на необходимость показывать Варнинг)
	[Class(0, Table = "Orders", Schema = "internet", NameType = typeof(ClientOrder))]
	public class ClientOrder : BaseModel
	{
		public ClientOrder()
		{
			OrderServices = new List<OrderService>();
		}

		[Property, Description("Номер заказа")]
		public virtual uint Number { get; set; }

		[Property,  Description("Дата активации заказа")]
		public virtual DateTime? BeginDate { get; set; }

		[Property, Description("Дата окончания заказа")]
		public virtual DateTime? EndDate { get; set; }
		
		//обработана ли активация заказ, устанавливает биллинг нужен для учета списаний не периодических услуг
		[Property]
		public virtual bool IsActivated { get; set; }

		//обработана ли деактивация заказа, устанавливает биллинг нужен для учета списаний периодических услуг
		[Property]
		public virtual bool IsDeactivated { get; set; }

		[Bag(0, Table = "OrderServices", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "OrderId")]
		[OneToMany(2, ClassType = typeof(OrderService))]
		public virtual IList<OrderService> OrderServices { get; set; }
	}

	//перенесено из старой админки (нужен при проверке на необходимость показывать Варнинг)
	[Class(0, Table = "OrderServices", Schema = "internet", NameType = typeof(OrderService), Lazy = true)]
	public class OrderService : BaseModel
	{
		[Property, Description("Стоимость")]
		public virtual decimal Cost { get; set; }

		[Property, Description("Услуга периодичная")]
		public virtual bool IsPeriodic { get; set; }
	}
}