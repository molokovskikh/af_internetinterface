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
			var newClient = new Client();
			//string Applying = string.Empty;
			if (Validator.IsValid(_user))
			{
				{
					newClient.Name = _user.Name;
					newClient.Surname = _user.Surname;
					newClient.Patronymic = _user.Patronymic;
					newClient.City = _user.City;
					newClient.AdressConnect = _user.AdressConnect;
					newClient.PassportSeries = _user.PassportSeries;
					newClient.PassportNumber = _user.PassportNumber;
					newClient.WhoGivePassport = _user.WhoGivePassport;
					newClient.RegistrationAdress = _user.RegistrationAdress;
					newClient.RegDate = DateTime.Now;
					newClient.Tariff = Tariff.FindAllByProperty("Id", tariff)[0];
					newClient.Balance = Popolnenie ? newClient.Tariff.Price : 0;
					newClient.Login = _user.Login;
					newClient.Password = CryptoPass.GetHashString(_user.Password);
					newClient.HasRegistered = Partner.FindAllByProperty("Pass", Session["HashPass"].ToString())[0];
					newClient.SaveAndFlush();
					PropertyBag["Applying"] = "true";
					PropertyBag["Client"] = new Client();
					PropertyBag["Tariffs"] = Tariff.FindAll();
					PropertyBag["Popolnen"] = Popolnenie;
				}
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


		//[AccessibleThrough(Verb.Get)]
		public void Register()
		{
			if (Partner.FindAllByProperty("Pass", Session["HashPass"]).Length == 0)
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
			else
			{
				PropertyBag["Tariffs"] = Tariff.FindAll();
				PropertyBag["Client"] = new Client();
				PropertyBag["Applying"] = "false";
				PropertyBag["Popolnen"] = false;
			}
		}
	}

}