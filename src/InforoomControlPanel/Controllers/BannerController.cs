using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Models;
using InternetInterface.Models;
using NHibernate.Linq;
using NHibernate.Util;
using PackageSpeed = Inforoom2.Models.PackageSpeed;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления баннерами
	/// </summary>
	public class BannerController : ControlPanelController
	{
		/// <summary>
		/// Список слайдов
		/// </summary>
		public ActionResult BannerIndex()
		{
			var banner = DbSession.Query<Banner>().OrderBy(s => s.Region.Name).ThenByDescending(s => s.Enabled).ThenByDescending(s => s.Id).ToList();
			var pathFromConfigUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadUrl"];
			if (pathFromConfigUrl == null)
			{
				throw new Exception("Значение 'inforoom2UploadUrl' отсуствует в Global.config!");
			}
			ViewBag.pathFromConfigURL = pathFromConfigUrl;
			ViewBag.Banner = banner;
			return View();
		}

		/// <summary>
		/// Форма добавление слайда
		/// </summary>
		public ActionResult CreateBanner()
		{
			//Создаем слайд
			var banner = new Banner() { Enabled = false };
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();

			ViewBag.Banner = banner;
			ViewBag.RegionList = regionList;

			return View();
		}

		/// <summary>
		/// Добавление слайда в базу
		/// </summary>
		[HttpPost]
		public ActionResult CreateBanner([EntityBinder] Banner banner, HttpPostedFileBase uploadedFile)
		{
			string imagePath = "";
			var pathFromConfig = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadPath"];
			var pathFromConfigUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadUrl"];
			if (pathFromConfig == null) {
				throw new Exception("Значение 'inforoom2UploadPath' отсуствует в Global.config!");
			}
			if (pathFromConfigUrl == null)
			{
				throw new Exception("Значение 'inforoom2UploadUrl' отсуствует в Global.config!");
			}
			ViewBag.pathFromConfigURL = pathFromConfigUrl;
			var ext = uploadedFile == null ? "" : new FileInfo(uploadedFile.FileName).Extension;
			string NewFileName = System.Guid.NewGuid() + ext;
			if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") {
				try {
					imagePath = pathFromConfig + "Images/" + NewFileName;
					uploadedFile.SaveAs(imagePath);
				}
				catch (Exception) {
					imagePath = "";
				}
			}
			banner.ImagePath = "";
			banner.LastEdit = DateTime.Now;
			banner.Partner = DbSession.Query<Employee>().FirstOrDefault(s => s.Login == User.Identity.Name);
			var errors = ValidationRunner.Validate(banner);
			if (errors.Length == 0 && imagePath != "") {
				banner.ImagePath = "Images/" + NewFileName;
				if (banner.Enabled) {
					var changeEnabled = DbSession.Query<Banner>().Where(s => s.Region == banner.Region).ToList();
					foreach (var item in changeEnabled) {
						if (item != banner) {
							item.Enabled = false;
							DbSession.Save(item);
						}
					}
				}
				DbSession.Save(banner);
				SuccessMessage("Баннер успешно добавлен");
				return RedirectToAction("BannerIndex");
			}

			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.RegionList = regionList;
			ViewBag.banner = banner;
			return View("CreateBanner");
		}

		/// <summary>
		/// Просмотр слайда
		/// </summary>
		public ActionResult EditBanner(int id)
		{
			var pathFromConfig = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadPath"];
			var pathFromConfigUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadUrl"];
			if (pathFromConfig == null)
			{
				throw new Exception("Значение 'inforoom2UploadPath' отсуствует в Global.config!");
			}
			if (pathFromConfigUrl == null)
			{
				throw new Exception("Значение 'inforoom2UploadUrl' отсуствует в Global.config!");
			}
			ViewBag.pathFromConfigURL = pathFromConfigUrl;
			//Создаем слайд
			var banner = DbSession.Query<Banner>().FirstOrDefault(s => s.Id == id);
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.Banner = banner;
			ViewBag.RegionList = regionList;
			return View();
		}

		/// <summary>
		/// Изменение слайда
		/// </summary>
		public ActionResult UpdateBanner([EntityBinder] Banner banner, HttpPostedFileBase uploadedFile)
		{
			string imagePath = banner.ImagePath ?? "";
			var pathFromConfig = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadPath"];
			var pathFromConfigUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadUrl"];
			if (pathFromConfig == null) {
				throw new Exception("Значение 'inforoom2UploadPath' отсуствует в Global.config!");
			}
			if (pathFromConfigUrl == null)
			{
				throw new Exception("Значение 'inforoom2UploadUrl' отсуствует в Global.config!");
			}
			ViewBag.pathFromConfigURL = pathFromConfigUrl;
			var ext = uploadedFile == null ? "" : new FileInfo(uploadedFile.FileName).Extension;
			string NewFileName = System.Guid.NewGuid() + ext;
			if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") {
				try {
					imagePath = pathFromConfig + "Images/" + NewFileName;
					uploadedFile.SaveAs(imagePath);
				}
				catch (Exception) {
					imagePath = "";
				}
			}
			banner.Partner = DbSession.Query<Employee>().FirstOrDefault(s => s.Login == User.Identity.Name);
			var errors = ValidationRunner.Validate(banner);
			if (errors.Length == 0 && imagePath != "") {
				if (uploadedFile != null) {
					if (System.IO.File.Exists(pathFromConfig + banner.ImagePath)) {
						System.IO.File.Delete(pathFromConfig + banner.ImagePath);
					}
					banner.ImagePath = "Images/" + NewFileName;
				}
				if (banner.Enabled) {
					var changeEnabled = DbSession.Query<Banner>().Where(s => s.Region == banner.Region).ToList();
					foreach (var item in changeEnabled) {
						if (item != banner) {
							item.Enabled = false;
							DbSession.Save(item);
						}
					}
				}
				DbSession.Save(banner);
				SuccessMessage("Баннер успешно сохранен");
				return RedirectToAction("BannerIndex");
			}
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.RegionList = regionList;
			ViewBag.Banner = banner;
			return View("EditBanner");
		}


		/// <summary>
		/// Удаление баннера
		/// </summary>
		public ActionResult DeleteBanner(int id)
		{
			var banner = DbSession.Query<Banner>().FirstOrDefault(s => s.Id == id);
			var pathFromConfig = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadPath"];
			if (pathFromConfig == null) {
				throw new Exception("Значение 'inforoom2UploadPath' отсуствует в Global.config!");
			}

			if (System.IO.File.Exists(pathFromConfig + banner.ImagePath)) {
				System.IO.File.Delete(pathFromConfig + banner.ImagePath);
			}
			DbSession.Delete(banner);
			DbSession.Flush();
			SuccessMessage("Баннер успешно удален");
			return RedirectToAction("BannerIndex");
		}
	}
}