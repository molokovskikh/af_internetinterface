using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InternetInterface.Models;
using NHibernate.Criterion;
using NHibernate.Impl;


namespace InternetInterface.Controllers
{


	public class RegisterController : SmartDispatcherController
	{
		[AccessibleThrough(Verb.Post)]
		public void RegisterClient([DataBind("client")]Client _user, bool Popolnenie, uint tariff)
		{
			var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			if ((MapPartner.Length != 0) && (AccessCategories.AccesPartner(MapPartner[0], (uint)AccessCategoriesType.RegisterClient)))
			{
				if (RegistrLogicClient(_user, Popolnenie, tariff))
				{
					PropertyBag["Applying"] = "true";
					PropertyBag["Client"] = new Client();
					PropertyBag["Tariffs"] = Tariff.FindAll();
					PropertyBag["Popolnen"] = Popolnenie;
				}
				else
				{
					_user.SetValidationErrors(Validator.GetErrorSummary(_user));
					PropertyBag["Client"] = _user;
					PropertyBag["Tariffs"] = Tariff.FindAll();
					PropertyBag["Applying"] = "false";
					PropertyBag["Popolnen"] = Popolnenie;
				}
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void RegisterPartner([DataBind("Partner")]Partner _Partner, [DataBind("ForRight")]List<uint> _Rights)
		{
			var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			if ((MapPartner.Length != 0) && (AccessCategories.AccesPartner(MapPartner[0], (uint)AccessCategoriesType.RegisterPartner)))
			{
				if (RegistrLogicPartner(_Partner, _Rights))
				{
					PropertyBag["Applying"] = "true";
					PropertyBag["Rights"] = ActiveRecordBase<AccessCategories>.FindAll(DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("Code <> 16")));
					PropertyBag["Partner"] = new Partner();
				}
				else
				{
					_Partner.SetValidationErrors(Validator.GetErrorSummary(_Partner));
					PropertyBag["Rights"] = ActiveRecordBase<AccessCategories>.FindAll(DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("Code <> 16")));
					PropertyBag["Partner"] = _Partner;
					PropertyBag["Applying"] = "false";
				}
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}

		private uint CombineAccess(List<uint> Rights)
		{
			uint rightSet = 0;
			foreach (uint right in Rights)
			{
				rightSet += right;
			}
			if (((rightSet | (uint)AccessCategoriesType.SendDemand) == (uint)AccessCategoriesType.SendDemand) &&
			((rightSet | (uint)AccessCategoriesType.GetClientInfo) != (uint)AccessCategoriesType.GetClientInfo))
			{
				rightSet += (uint) AccessCategoriesType.GetClientInfo;
			}
			return rightSet;
		}

		public bool RegistrLogicPartner(Partner _Partner, List<uint> _Rights)
		{
			var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			if ((MapPartner.Length != 0) && (AccessCategories.AccesPartner(MapPartner[0], (uint)AccessCategoriesType.RegisterPartner)))
			{
				var newPartner = new Partner();
				if (Validator.IsValid(_Partner))
				{
					newPartner.Name = _Partner.Name;
					newPartner.Email = _Partner.Email;
					newPartner.TelNum = _Partner.TelNum;
					newPartner.Adress = _Partner.Adress;
					newPartner.RegDate = DateTime.Now;
					newPartner.Login = _Partner.Login;
					newPartner.Pass = CryptoPass.GetHashString(_Partner.Pass);
					newPartner.AcessSet = CombineAccess(_Rights);
					newPartner.SaveAndFlush();
					var newPartnerID = Partner.FindAllByProperty("Login", _Partner.Login);
					if ((newPartner.AcessSet & (uint)AccessCategoriesType.CloseDemand) == (uint)AccessCategoriesType.CloseDemand)
					{
						var newBrigad = new Brigad();
						newBrigad.Name = newPartner.Name;
						newBrigad.PartnerID = newPartnerID[0];
						newBrigad.Adress = newPartner.Adress;
						newBrigad.BrigadCount = 1;
						newBrigad.SaveAndFlush();
					}
					return true;
				}
			}
			return false;
		}

		public bool RegistrLogicClient(Client _client, bool _Popolnenie, uint _tariff)
		{
			var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			if ((MapPartner.Length != 0) && (AccessCategories.AccesPartner(MapPartner[0], 2)))
			{
				var newClient = new Client();
				if (Validator.IsValid(_client))
				{
					newClient.Name = _client.Name;
					newClient.Surname = _client.Surname;
					newClient.Patronymic = _client.Patronymic;
					newClient.City = _client.City;
					newClient.AdressConnect = _client.AdressConnect;
					newClient.PassportSeries = _client.PassportSeries;
					newClient.PassportNumber = _client.PassportNumber;
					newClient.WhoGivePassport = _client.WhoGivePassport;
					newClient.RegistrationAdress = _client.RegistrationAdress;
					newClient.RegDate = DateTime.Now;
					newClient.Tariff = Tariff.FindAllByProperty("Id", _tariff)[0];
					newClient.Balance = _Popolnenie ? newClient.Tariff.Price : 0;
					newClient.Login = _client.Login;
					newClient.Password = CryptoPass.GetHashString(_client.Password);
					newClient.HasRegistered = Partner.FindAllByProperty("Pass", Session["HashPass"].ToString())[0];
					newClient.SaveAndFlush();
					return true;
				}
			}
			return false;
		}

		//[AccessibleThrough(Verb.Get)]
		public void RegisterClient()
		{
			var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			if ((MapPartner.Length != 0) && (AccessCategories.AccesPartner(MapPartner[0], (uint)AccessCategoriesType.RegisterClient)))
			{
				PropertyBag["Tariffs"] = Tariff.FindAll();
				PropertyBag["Client"] = new Client();
				PropertyBag["Applying"] = "false";
				PropertyBag["Popolnen"] = false;
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}

		public void RegisterPartner()
		{
			var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			if ((MapPartner.Length != 0) && (AccessCategories.AccesPartner(MapPartner[0], (uint)AccessCategoriesType.RegisterPartner)))
			{
				PropertyBag["Rights"] = ActiveRecordBase<AccessCategories>.FindAll(DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("Code <> 16")));
				PropertyBag["Partner"] = new Partner();
				PropertyBag["Applying"] = "false";
				/*PropertyBag["Popolnen"] = false;*/
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}
	}

}