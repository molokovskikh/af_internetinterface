using System;
using System.Collections.Generic;
using System.Linq;
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
		public TwoRule(AccessCategoriesType head, AccessCategoriesType child)
		{
			Head = head;
			Child = child;
			AddEvent = AddFreeMethod;
			DeleteEvent = DeleteFreeMethod;
		}

		public TwoRule()
		{
			// TODO: Complete member initialization
		}

		public AccessCategoriesType Head;
		public AccessCategoriesType Child;

		public delegate void AddEventHandler(Partner partner);
		public delegate void DeleteleEventHandler(Partner partner);

		public AddEventHandler AddEvent;
		public DeleteleEventHandler DeleteEvent;

		private static void AddFreeMethod(Partner partner)
		{
		}

		private static void DeleteFreeMethod(Partner partner)
		{
		}


	}

	public class AccessDependence
	{
		private static List<TwoRule> accessDependence;
		private static List<AccessCategoriesType> toAdd;
		private static List<AccessCategoriesType> toDelete;
		private static List<int> hasDelete;

		private static void SetAccessDependence()
		{
			toAdd = new List<AccessCategoriesType>();
			toDelete = new List<AccessCategoriesType>();
			hasDelete = new List<int>();
			accessDependence = new List<TwoRule>
			                   	{
			                   		//new TwoRule(AccessCategoriesType.GetClientInfo,AccessCategoriesType.SendDemand),
			                   		/*new TwoRule
			                   			{
			                   				Head = AccessCategoriesType.GetClientInfo,
			                   				Child = AccessCategoriesType.SendDemand
			                   			},*/
			                   		new TwoRule(AccessCategoriesType.GetClientInfo, AccessCategoriesType.SendDemand),
			                   		new TwoRule(AccessCategoriesType.ChangeBalance, AccessCategoriesType.CloseDemand)
			                   		/*new TwoRule("GetClientInfo","SendDemand"),
									new TwoRule("SendDemand","CloseDemand"),
									new TwoRule("CloseDemand","RegisterPartner"),
									new TwoRule("RegisterPartner","ChangeBalance")*/
			                   	};
			//accessDependence.Add("GetClientInfo", "SendDemand");
			//return accessDependence;
		}

		private static void GenerateAddList(List<TwoRule> dictionary, AccessCategoriesType field)
		{
			foreach (var dic in dictionary.Where(dic => dic.Child == field))
			{
				toAdd.Add(dic.Head);
				GenerateAddList(dictionary, dic.Head);
			}
		}

		private static void GenerateDeleteList(List<TwoRule> dictionary, AccessCategoriesType field)
		{
			foreach (var dic in dictionary.Where(dic => dic.Head == field))
			{
				toDelete.Add(dic.Child);
				GenerateDeleteList(dictionary, dic.Child);
			}
		}

		public static void SetCrossAccess(List<int> oldAccessSet, List<int> newAccessSet, Partner partner)
		{
			SetAccessDependence();
			foreach (var twoRule in accessDependence)
			{
				if (!(newAccessSet.Contains((int)twoRule.Head)) &&
					(oldAccessSet.Contains((int)twoRule.Head)))
				{
					GenerateDeleteList(accessDependence, twoRule.Head);
					foreach (var todel in toDelete)
					{
						var delSendDemWithoutGCI = PartnerAccessSet.FindAll(DetachedCriteria.For(typeof(PartnerAccessSet))
																				.Add(Expression.Eq("PartnerId", partner))
																				.Add(Expression.Eq("AccessCat",
																								   AccessCategories.Find((int)todel))));
						foreach (var partnerAccessSet in delSendDemWithoutGCI)
						{
							hasDelete.Add(partnerAccessSet.AccessCat.Id);
							partnerAccessSet.DeleteAndFlush();
						}
						//twoRule.DeleteEvent(partner);
					}
				}
				toAdd.Clear();
				toDelete.Clear();
			}
			foreach (var twoRule in accessDependence)
			{
				if ((newAccessSet.Contains((int)twoRule.Child)) &&
					(!oldAccessSet.Contains((int)twoRule.Child)))
				{
					GenerateAddList(accessDependence, twoRule.Child);
					if (hasDelete.Contains((int)twoRule.Child))
					{
						toAdd.Add(twoRule.Child);
					}
					var partnerAccessSet = PartnerAccessSet.FindAll(DetachedCriteria.For(typeof (PartnerAccessSet))
					                                                	.Add(Expression.Eq("PartnerId", partner)));
					foreach (var toadd in toAdd)
					{
						/*if (PartnerAccessSet.FindAll(DetachedCriteria.For(typeof (PartnerAccessSet))
						                             	.Add(Expression.Eq("PartnerId", partner))
														.Add(Expression.Eq("AccessCat", AccessCategories.Find((int)toadd)))).Length == 0)*/
						if (partnerAccessSet.Where(c => c.AccessCat.Id == (int)toadd).ToList().Count == 0)
						{
							var newRight = new PartnerAccessSet
							               	{
												AccessCat = AccessCategories.Find((int)toadd),
							               		PartnerId = partner
							               	};
							newRight.SaveAndFlush();
						}
					}
				}
				toAdd.Clear();
				toDelete.Clear();
			}
		}

		public static void SetCrossAccessForRegister(List<int> newRights, Partner partner)
		{
			SetAccessDependence();
			foreach (var twoRule in accessDependence)
			{
				if (newRights.Contains((int) twoRule.Child))
				{
					GenerateAddList(accessDependence, twoRule.Child);
					foreach (var toadd in toAdd)
					{
						if (!newRights.Contains((int)toadd))
						{
							var newRight = new PartnerAccessSet
							{
								AccessCat = AccessCategories.Find((int)toadd),
								PartnerId = partner
							};
							newRight.SaveAndFlush();
						}
					}
				}
				toAdd.Clear();
			}
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

	}
}