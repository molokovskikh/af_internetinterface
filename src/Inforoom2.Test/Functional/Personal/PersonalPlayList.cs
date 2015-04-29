using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OpenQA.Selenium;

namespace Inforoom2.Test.Functional.Personal
{
	class PersonalPlayList : PersonalFixture
	{
		[Test(Description = "Проверка отображения списка каналов при переходе по ссылке")]
		public void PlayList()
		{
			Open("Personal/PlayList");
			var TVChannelsGroups = Client.PhysicalClient.Plan.TvChannelGroups.ToList();
			for (var i = 0; i < TVChannelsGroups.Count; i++) {
				var TVChannels = Client.PhysicalClient.Plan.TvChannelGroups[i].TvChannels.ToList();
				for (var y = 0; y < TVChannels.Count ; y++)
					AssertText(TVChannels[y].Name);
				AssertText("#EXTINF");
				AssertText("#EXTVLCOPT");
			}
							
		}
	}
}
