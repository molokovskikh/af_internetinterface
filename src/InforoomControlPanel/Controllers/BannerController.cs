using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using Inforoom2.Components;
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
	public class BannerController : AdminController
	{
		/// <summary>
		/// Список слайдов
		/// </summary>
		public ActionResult BannerIndex()
		{
			var banner = DbSession.Query<Banner>().OrderBy(s => s.Region.Name).ThenByDescending(s => s.Enabled).ThenByDescending(s => s.Id).ToList();
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
			var ext = uploadedFile == null ? "" : new FileInfo(uploadedFile.FileName).Extension;
			string NewFileName = System.Guid.NewGuid() + ext;
			if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") {
				try {
					imagePath = Server.MapPath("~/Images/Uploaded/" + NewFileName);
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
				banner.ImagePath = "/Images/Uploaded/" + NewFileName;
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
			var ext = uploadedFile == null ? "" : new FileInfo(uploadedFile.FileName).Extension;
			string NewFileName = System.Guid.NewGuid() + ext;
			if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") {
				try {
					imagePath = Server.MapPath("~/Images/Uploaded/" + NewFileName);
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
					if (System.IO.File.Exists(Server.MapPath(banner.ImagePath))) {
						System.IO.File.Delete(Server.MapPath(banner.ImagePath));
					}
					banner.ImagePath = "/Images/Uploaded/" + NewFileName;
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
			var Banner = DbSession.Query<Banner>().FirstOrDefault(s => s.Id == id);

			if (System.IO.File.Exists(Server.MapPath(Banner.ImagePath))) {
				System.IO.File.Delete(Server.MapPath(Banner.ImagePath));
			}

			DbSession.Delete(Banner);
			DbSession.Flush();
			SuccessMessage("Баннер успешно удален");
			return RedirectToAction("BannerIndex");
		}
	}
}