using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.Banner
{
    class PriceListFixture : ControlPanelBaseFixture
    {
		[Test, Description("Неуспешное создание баннера")]
		public void CreateAndEditPriceList()
		{
			Open("PriceList/PriceListIndex");
			var priceListCount = DbSession.Query<Inforoom2.Models.PublicData>().Where(s=>s.ItemType == PublicDataType.PriceList).Count();
			Assert.True(priceListCount == 0);
			browser.FindElementByCssSelector("#addPriceList").Click();
			WaitForText("Редактор прайс-листа");
			var inputObj = browser.FindElementByCssSelector("input[name='publicData.Name']");
			inputObj.Clear();
			inputObj.SendKeys("Прайс-лист");
			inputObj = browser.FindElementByCssSelector("input[name='publicData.Display']");
			inputObj.Click();
			inputObj = browser.FindElementByCssSelector("#savePriceInfo");
			inputObj.Click();
			WaitForText("Прайс лист успешно сохранен.");
			priceListCount = DbSession.Query<Inforoom2.Models.PublicData>().Where(s => s.ItemType == PublicDataType.PriceList).Count();
			Assert.True(priceListCount == 1);
			
			inputObj = browser.FindElementByCssSelector(".editPriceList");
			inputObj.Click();

			inputObj = browser.FindElementByCssSelector("#addRow");
			inputObj.Click();
			WaitForText("Редактировать прайс-лист");

			var name = "Наименование 1";
			var price = "123";
			var comment = "Комментарий 2";

			inputObj = browser.FindElementByCssSelector("input[name='PublicDataContext.Name']");
			inputObj.Clear();
			inputObj.SendKeys(name);

			inputObj = browser.FindElementByCssSelector("input[name='PublicDataContext.Price']");
			inputObj.Clear();
			inputObj.SendKeys(price);
			
			inputObj = browser.FindElementByCssSelector("textarea[name='PublicDataContext.Comment']");
			inputObj.Clear();
			inputObj.SendKeys(comment);

			inputObj = browser.FindElementByCssSelector("#savePriceInfo");
			inputObj.Click();

			var item = DbSession.Query<Inforoom2.Models.PublicData>().FirstOrDefault(s => s.ItemType == PublicDataType.PriceList);
			var itemFromList = item.Items.FirstOrDefault().ContextGet<ViewModelPublicDataPriceList>();

			Assert.True(itemFromList.Name == name);
			Assert.True(itemFromList.Price == price);
			Assert.True(itemFromList.Comment == comment);
		}
	}
}
