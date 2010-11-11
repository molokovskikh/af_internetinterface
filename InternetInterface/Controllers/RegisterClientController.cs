using System;
using Castle.MonoRail.Framework;
using InternetInterface.Models;


namespace InternetInterface.Controllers
{


	public class RegisterClientController : SmartDispatcherController
	{
		[AccessibleThrough(Verb.Post)]
		public void Register([DataBind("client")]Client _user, bool Popolnenie, uint tariff)
		{
			var MapPartner = Partner.FindAllByProperty("Pass", Session["HashPass"]);
			if (MapPartner.Length != 0)
			{
				if (RegistrLogic(_user, Popolnenie, tariff))
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


		public bool RegistrLogic(Client _client, bool _Popolnenie, uint _tariff)
		{
			var MapPartner = Partner.FindAllByProperty("Pass", Session["HashPass"]);
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
		public void Register()
		{
			var MapPartner = Partner.FindAllByProperty("Pass", Session["HashPass"]);
			if ((MapPartner.Length != 0) && (AccessCategories.AccesPartner(MapPartner[0], 2)))
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
	}

}