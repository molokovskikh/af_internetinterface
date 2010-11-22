using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using NHibernate.Criterion;

namespace InternetInterface.Models
{
	public enum AccessCategoriesType
	{
		GetClientInfo = 1,
		RegisterClient = 2,
		SendDemand = 3,
		CloseDemand = 4,
		RegisterPartner = 5,
		ChangeBalance = 6
	};

	public class TwoRule
	{
		public TwoRule(string head, string child)
		{
			Head = head;
			Child = child;
		}

		public string Head;
		public string Child;
	}

	public class AccessDependence
	{
		public static List<TwoRule> accessDependence;
		public static List<string> toAdd;
		public static List<string> toDelete;

		public static void SetAccessDependence()
		{
			toAdd = new List<string>();
			toDelete = new List<string>();
			accessDependence = new List<TwoRule>
			                   	{
			                   		new TwoRule("GetClientInfo","SendDemand"),
									new TwoRule("SendDemand","CloseDemand"),
									new TwoRule("CloseDemand","RegisterPartner"),
									new TwoRule("RegisterPartner","ChangeBalance")
			                   	};
			//accessDependence.Add("GetClientInfo", "SendDemand");
			//return accessDependence;
		}

		public static void GenerateAddList(List<TwoRule> dictionary, string field)
		{
			foreach (var dic in dictionary)
			{
				if (dic.Child != field) continue;
				toAdd.Add(dic.Head);
				GenerateAddList(dictionary, dic.Head);
			}
		}

		public static void GenerateDeleteList(List<TwoRule> dictionary, string field)
		{
			foreach (var dic in dictionary)
			{
				if (dic.Head != field) continue;
				toDelete.Add(dic.Child);
				GenerateDeleteList(dictionary, dic.Child);
			}
		}


		public static void SetCrossAccess(List<int> oldAccessSet, List<int> newAccessSet, Partner partner)
		{
			SetAccessDependence();
			foreach (var twoRule in accessDependence)
			{
				AccessCategoriesType accessAdd;
				AccessCategoriesType.TryParse(twoRule.Child, false, out accessAdd);
				if ((newAccessSet.Contains((int)accessAdd)) &&
				(!oldAccessSet.Contains((int)accessAdd)))
				{
					GenerateAddList(accessDependence, twoRule.Child);
					foreach (var toadd in toAdd)
					{
						AccessCategoriesType accessToadd;
						AccessCategoriesType.TryParse(toadd, false, out accessToadd);
						if (PartnerAccessSet.FindAll(DetachedCriteria.For(typeof(PartnerAccessSet))
							.Add(Expression.Eq("PartnerId", partner))
							.Add(Expression.Eq("AccessCat", AccessCategories.Find((int)accessToadd)))).Length == 0)
						{
							var newRight = new PartnerAccessSet
							               	{
							               		AccessCat = AccessCategories.Find((int) accessToadd),
							               		PartnerId = partner
							               	};
							newRight.SaveAndFlush();
						}
					}
				}
				AccessCategoriesType accessDel;
				AccessCategoriesType.TryParse(twoRule.Head, false, out accessDel);
				if (!(newAccessSet.Contains((int)accessDel)) &&
				(oldAccessSet.Contains((int)accessDel)))
				{
					GenerateDeleteList(accessDependence, twoRule.Head);
					foreach (var todel in toDelete)
					{
						AccessCategoriesType accessTodel;
						AccessCategoriesType.TryParse(todel, false, out accessTodel);
						var delSendDemWithoutGCI = PartnerAccessSet.FindAll(DetachedCriteria.For(typeof (PartnerAccessSet))
						                                                     	.Add(Expression.Eq("PartnerId", partner))
						                                                     	.Add(Expression.Eq("AccessCat",
						                                                     	                   AccessCategories.Find((int) accessTodel))));
						foreach (var partnerAccessSet in delSendDemWithoutGCI)
						{
							partnerAccessSet.DeleteAndFlush();
						}
					}
				}
			}
			toAdd.Clear();
			toDelete.Clear();
		}
	}


	[ActiveRecord("AccessCategories", Schema = "internet", Lazy = true)]
	public class AccessCategories : ChildActiveRecordLinqBase<AccessCategories>
	{
		[PrimaryKey]
		public virtual int Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string ReduceName { get; set; }

		/*public static IList<T> FindAllSort<T>()
		{
			return T.FindAll(DetachedCriteria.For(typeof (T)).AddOrder(Order.Asc("Name")))
		}*/
	}
}