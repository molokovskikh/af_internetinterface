using System.Linq;
using Common.Web.Ui.NHibernateExtentions;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.Admin;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.Switch
{
	class RegionIpPoolsFixture : SwitchFixture
	{
		[Test, Description("Добавление тарифного плана")]
		public void RegionIpPoolAddRemove()
		{
			var toDelete = DbSession.Query<IpPoolRegion>().ToList();
			DbSession.DeleteEach(toDelete);

			var description = "Описание Раз"; 
			var ipPool = DbSession.Query<IpPool>().FirstOrDefault();
			var region = DbSession.Query<Region>().FirstOrDefault(s => s.Name.IndexOf("Белгород") != -1);

			Open("Switch/RegionIpPools");
			var inputObj = browser.FindElementByCssSelector("[name='newIpPoolRegion.Description']");
			Assert.That(inputObj.GetAttribute("value"), Is.Not.EqualTo(description.ToString()), "Описание не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(description.ToString());

			Css("[name='newIpPoolRegion.IpPool.Id']").SelectByText(ipPool.GetBeginIp()+" - "+ ipPool.GetEndIp());
			Css("[name='newIpPoolRegion.Region.Id']").SelectByText(region.Name);
			 
			browser.FindElementByCssSelector(".btn-green").Click();

			var stSettings = DbSession.Query<IpPoolRegion>().FirstOrDefault(s=>s.Description == description);  
			Assert.That(stSettings.IpPool.Id, Is.EqualTo(ipPool.Id), "Пул не совпадает.");
			Assert.That(stSettings.Region.Id, Is.EqualTo(region.Id), "Регион не совпадает.");

			browser.FindElementByCssSelector(".entypo-cancel-circled").Click();
			stSettings = DbSession.Query<IpPoolRegion>().FirstOrDefault(s => s.Description == description);
			Assert.That(stSettings, Is.Null, "Элемент не был удален.");


		}
	}
}
