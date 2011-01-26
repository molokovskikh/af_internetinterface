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
		RegisterClient = 3,
		SendDemand = 5,
		//CloseDemand = 7,
		RegisterPartner = 9,
		ChangeBalance = 11,
		VisiblePassport = 13,
		VisibleDemand = 15,
		AccessDHCP = 17,
		EditClientInfo = 19,
		ShowSecretData = 21
	};

	public class TwoRule
	{
		public AccessCategoriesType HEADAccess
		{
			set { Head = GetRule(value); }
		}
		public AccessCategoriesType CHIDLAccess
		{
			set { Child = GetRule(value); }
		}


		public AccessCategories Head;
		public AccessCategories Child;

		private static AccessCategories GetRule(AccessCategoriesType rule)
		{
			return AccessCategories.Find((int) rule);
		}
	}

	public class AccessDependence
	{
		private static List<TwoRule> accessDependence;
		private static List<AccessCategories> toAdd;
		private static List<AccessCategories> toDelete;
		private static List<int> hasDelete;


		/// <summary>
		/// Инициализация связей прав. accessDependence - коллекция прав, члены которой - попарно связанные права
		/// </summary>
		private static void SetAccessDependence()
		{
			toAdd = new List<AccessCategories>();
			toDelete = new List<AccessCategories>();
			hasDelete = new List<int>();
			accessDependence = new List<TwoRule>
			                   	{
			                   		new TwoRule
			                   			{
			                   				HEADAccess = AccessCategoriesType.GetClientInfo,
			                   				CHIDLAccess = AccessCategoriesType.ShowSecretData,
			                   			},
									new TwoRule
			                   			{
			                   				HEADAccess = AccessCategoriesType.GetClientInfo,
			                   				CHIDLAccess = AccessCategoriesType.EditClientInfo
			                   			},
									new TwoRule
			                   			{
			                   				HEADAccess = AccessCategoriesType.GetClientInfo,
			                   				CHIDLAccess = AccessCategoriesType.SendDemand,
			                   			},
									new TwoRule
			                   			{
			                   				HEADAccess = AccessCategoriesType.GetClientInfo,
			                   				CHIDLAccess = AccessCategoriesType.VisiblePassport
			                   			}
			                   	};
		}

		/// <summary>
		/// Генерируем список недостающих прав на добавление.
		/// </summary>
		/// <param name="dictionary"></param>
		/// <param name="field"></param>
		private static void GenerateAddList(List<TwoRule> dictionary, AccessCategories field)
		{
			foreach (var dic in dictionary.Where(dic => dic.Child == field))
			{
				toAdd.Add(dic.Head);
				GenerateAddList(dictionary, dic.Head);
			}
		}

		/// <summary>
		/// Генерируем список недостающих прав на удаление.
		/// </summary>
		/// <param name="dictionary"></param>
		/// <param name="field"></param>
		private static void GenerateDeleteList(List<TwoRule> dictionary, AccessCategories field)
		{
			foreach (var dic in dictionary.Where(dic => dic.Head == field))
			{
				toDelete.Add(dic.Child);
				GenerateDeleteList(dictionary, dic.Child);
			}
		}

		/// <summary>
		/// Этот метод передазначает права при редактировании партрера.
		/// </summary>
		/// <param name="oldAccessSet"></param>
		/// <param name="newAccessSet"></param>
		/// <param name="partner"></param>
		public static void SetCrossAccess(List<int> oldAccessSet, List<int> newAccessSet, UserCategorie userCategorie)
		{
			SetAccessDependence();
			foreach (var twoRule in accessDependence)
			{
				if (!(newAccessSet.Contains(twoRule.Head.Id)) &&
					(oldAccessSet.Contains(twoRule.Head.Id)))
				{
					GenerateDeleteList(accessDependence, twoRule.Head);
					foreach (var todel in toDelete)
					{
						var delSendDemWithoutGCI = CategorieAccessSet.FindAll(DetachedCriteria.For(typeof(CategorieAccessSet))
																				.Add(Expression.Eq("Categorie", userCategorie))
																				.Add(Expression.Eq("AccessCat",todel)));
						foreach (var categorieAccessSet in delSendDemWithoutGCI)
						{
							hasDelete.Add(categorieAccessSet.AccessCat.Id);
							categorieAccessSet.DeleteAndFlush();
							//todel.DeleteTo(partner);
						}
					}
				}
				toAdd.Clear();
				toDelete.Clear();
			}

			foreach (var twoRule in accessDependence)
			{
				if ((newAccessSet.Contains(twoRule.Child.Id)) &&
					(!oldAccessSet.Contains(twoRule.Child.Id)))
				{
					GenerateAddList(accessDependence, twoRule.Child);
					if (hasDelete.Contains(twoRule.Child.Id))
					{
						toAdd.Add(twoRule.Child);
					}
					var partnerAccessSet = CategorieAccessSet.FindAll(DetachedCriteria.For(typeof(CategorieAccessSet))
																		.Add(Expression.Eq("Categorie", userCategorie)));
					foreach (var toadd in toAdd)
					{
						/*if (PartnerAccessSet.FindAll(DetachedCriteria.For(typeof (PartnerAccessSet))
						                             	.Add(Expression.Eq("PartnerId", partner))
														.Add(Expression.Eq("AccessCat", AccessCategories.Find((int)toadd)))).Length == 0)*/
						if (partnerAccessSet.Where(c => c.AccessCat == toadd).ToList().Count == 0)
						{
							var newRight = new CategorieAccessSet
							               	{
												AccessCat = toadd,
												Categorie = userCategorie
							               	};
							newRight.SaveAndFlush();
						}
					}
				}
				toAdd.Clear();
				toDelete.Clear();
			}
		}

		/// <summary>
		/// При создании нового патнера применить этот метод для назначения недостающих прав доступа
		/// (если пользователь забыл поставить нужных галочек).
		/// </summary>
		/// <param name="newRights"></param>
		/// <param name="userCategorie"></param>
		public static void SetCrossAccessForRegister(List<int> newRights, UserCategorie userCategorie)
		{
			SetAccessDependence();
			foreach (var twoRule in accessDependence)
			{
				if (newRights.Contains(twoRule.Child.Id))
				{
					GenerateAddList(accessDependence, twoRule.Child);
					foreach (var toadd in toAdd)
					{
						if (!newRights.Contains(toadd.Id))
						{
							var newRight = new CategorieAccessSet
							{
								AccessCat = toadd,
								Categorie = userCategorie
							};
							newRight.SaveAndFlush();
							//toadd.AcceptTo(partner);
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

		public virtual void AcceptTo(UserCategorie userCategorie)
		{
			var partners = Partner.FindAllByProperty("Categorie", userCategorie);
			foreach (var partner in partners)
			{
				AcceptToOne(partner);
			}
		}

		public virtual void AcceptToOne(Partner partner)
		{
			if ((int)AccessCategoriesType.ChangeBalance == Id)
			{
				var findedAgents = FindAgentByPartner(partner);
				if (findedAgents.Count == 0)
				{
					var newAgent = new Agent
					               	{
					               		Name = partner.Name,
					               		Partner = partner
					               	};
					newAgent.SaveAndFlush();
				}
				else
				{
					foreach (var findedAgent in findedAgents)
					{
						findedAgent.Partner = partner;
						findedAgent.UpdateAndFlush();
					}
				}
			}
		}

		public virtual void DeleteTo(UserCategorie userCategorie)
		{
		}

		private static List<Brigad> FindBrigadsByPartner(Partner partner)
		{
			return Brigad.FindAll(DetachedCriteria.For(typeof (Brigad))
			                      	.Add(Expression.Eq("PartnerID", partner))).ToList();
		}

		private static List<Agent> FindAgentByPartner(Partner partner)
		{
			return Agent.FindAll(DetachedCriteria.For(typeof(Agent))
									.Add(Expression.Eq("Partner", partner))).ToList();
		}
	}
}