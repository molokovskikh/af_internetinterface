using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using InternetInterface.Test.Helpers;
using InternetInterface.Models;
using NUnit.Framework;
using WatiN.Core;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	class RequestViewFixture : WatinFixture
	{
		[Test]
		public void ViewTest()
		{
			using (var browser = Open("UserInfo/RequestView.rails"))
			{
				Assert.That(browser.Text, Is.StringContaining("Email"));
				Assert.That(browser.Text, Is.StringContaining("Адрес"));
				var checkCount =  browser.Table(Find.ById("clientTable")).TableRows.Count;
				var checkedList = new List<int>();
				var checkIndexGenerator = new Random();
				for (int i = 1; i < checkCount; i++)
				{
					if (checkIndexGenerator.Next(8) == 3)
					checkedList.Add(checkIndexGenerator.Next(checkCount));
				}
				foreach (var i in checkedList)
				{
					browser.CheckBox(Find.ByName("LabelList["+i+"]")).Checked = true;
				}
				var Labels = Models.Label.FindAll();
				var rSelector = new Random();
				var selectedlabel = Labels[rSelector.Next(Labels.Length)].Id.ToString();
				browser.SelectList(Find.ById("labelch")).SelectByValue(selectedlabel);
				browser.Button(Find.ById("SetlabelButton")).Click();
				Thread.Sleep(400);
				foreach (var i in checkedList)
				{
					browser.CheckBox(Find.ById("LabelDemandCheck" + i)).Checked = true;
				}
				browser.SelectList(Find.ById("labelch")).SelectByValue("0");
				browser.Button(Find.ById("SetlabelButton")).Click();
				browser.TextField(Find.ById("LabelName")).AppendText("TestLabel");
				browser.Button(Find.ById("CreateLabelButton")).Click();
				var deleteLabel = Models.Label.FindAllByProperty("Name", "TestLabel");
				foreach (var label in deleteLabel)
				{
					browser.SelectList(Find.ById("deletelabelch")).SelectByValue(label.Id.ToString());
					browser.Button(Find.ById("DeleteLabelButton")).Click();
					Thread.Sleep(400);
				}
			}
		}
	}
}
