//// | СКРИПТ ДЛЯ СТРАНИЦЫ ПОИСКА КЛИЕНТОВ | ===========>>

//название классов панелей с содержанием дополнительных полей 
var FilterConditionClassPhysicalClient = ".FilterSubPanel.PhysicalClient";
var FilterConditionClassLegalClient = ".FilterSubPanel.LegalClient";
//положение кнопок очистки
var CleanFilterButtonAtPlaceTop = -1;
var CleanFilterButtonAtPlaceLeft = 50;
//замена параметров сортировки, при смене типа клиента
var FilterOrderItemsList = [
	{
		Id: ".FilterOrder_DissolvedDate",
		PhysName: "",
		LegalName: "LegalClientOrders.First().EndDate"
	}, {
		Id: ".FilterOrder_Plan",
		PhysName: "PhysicalClient.Plan.Name",
		LegalName: ""
	}, {
		Id: ".FilterOrder_Balance",
		PhysName: "PhysicalClient.Balance",
		LegalName: "LegalClient.Balance"
	}
];

//проверка наличия "типа сортировки" в текущем Url, его маркеровка
function checkCurrentOrderUrlCorrespond() {
	//получаем url
	var currentUrl = window.location.href.substring(window.location.href.indexOf("mfilter.orderBy"));
	//пробегаемся по ссылкам сортировки
	$("table thead a").each(function() {
		var mask = $(this).attr("href").substring($(this).attr("href").indexOf("mfilter.orderBy"));
		if (currentUrl == mask) {
			$(this).addClass("checked");
			if ($(this).attr("href").indexOf("mfilter.orderType=Asc") != -1) {
				$(this).addClass("entypo-up-open-mini");
			} else {
				$(this).addClass("entypo-down-open-mini");
			}
			return;
		}
	});
}

//установка фильтров поиска в начальное состояние, при смене типа клиента
function SetFilterCondition(clientType) {
	//Обработка условий

	//для Физ.лиц
	if (clientType == 1) {
		if ($(FilterConditionClassPhysicalClient).hasClass('hid'))
			$(FilterConditionClassPhysicalClient).removeClass('hid');

		if ($(FilterConditionClassLegalClient).hasClass('hid') == false)
			$(FilterConditionClassLegalClient).addClass('hid');
		$(FilterConditionClassLegalClient + " input").val("");
		$(FilterConditionClassLegalClient + " select").val("");
	}
	//для Юр.лиц
	if (clientType == 0) {
		if ($(FilterConditionClassLegalClient).hasClass('hid'))
			$(FilterConditionClassLegalClient).removeClass('hid');

		if ($(FilterConditionClassPhysicalClient).hasClass('hid') == false)
			$(FilterConditionClassPhysicalClient).addClass('hid');
		$(FilterConditionClassPhysicalClient + " input").val("");
		$(FilterConditionClassPhysicalClient + " select").val("");
	}
	//для Всех 
	if (clientType == "") {
		if ($(FilterConditionClassPhysicalClient).hasClass('hid') == false)
			$(FilterConditionClassPhysicalClient).addClass('hid');
		$(FilterConditionClassPhysicalClient + " input").val("");
		$(FilterConditionClassPhysicalClient + " select").val("");

		if ($(FilterConditionClassLegalClient).hasClass('hid') == false)
			$(FilterConditionClassLegalClient).addClass('hid');
		$(FilterConditionClassLegalClient + " input").val("");
		$(FilterConditionClassLegalClient + " select").val("");
	}
}

//установка сортировки в завсимости от типа клиента
function SetFilterOrder(clientType) {
	try {
		//для Физ.лиц
		if (clientType == 1) {
			for (var i = 0; i < FilterOrderItemsList.length; i++) {
				if (FilterOrderItemsList[i].PhysName != "") {
					$("a" + FilterOrderItemsList[i].Id).attr("href", $("#orderId").attr("href").replace(/&mfilter.orderBy=Id/g, "&mfilter.orderBy=" + FilterOrderItemsList[i].PhysName));
					if ($("span" + FilterOrderItemsList[i].Id).hasClass('hid') == false)
						$("span" + FilterOrderItemsList[i].Id).addClass('hid');
					if ($("a" + FilterOrderItemsList[i].Id).hasClass('hid'))
						$("a" + FilterOrderItemsList[i].Id).removeClass('hid');
				} else {
					$("a" + FilterOrderItemsList[i].Id).attr("href", "");
					if ($("span" + FilterOrderItemsList[i].Id).hasClass('hid'))
						$("span" + FilterOrderItemsList[i].Id).removeClass('hid');
					if ($("a" + FilterOrderItemsList[i].Id).hasClass('hid') == false)
						$("a" + FilterOrderItemsList[i].Id).addClass('hid');
				}
			}
		}
		//для Юр.лиц
		if (clientType == 0) {
			for (var i = 0; i < FilterOrderItemsList.length; i++) {
				if (FilterOrderItemsList[i].LegalName != "") {
					$("a" + FilterOrderItemsList[i].Id).attr("href", $("#orderId").attr("href").replace(/&mfilter.orderBy=Id/g, "&mfilter.orderBy=" + FilterOrderItemsList[i].LegalName));
					if ($("span" + FilterOrderItemsList[i].Id).hasClass('hid') == false)
						$("span" + FilterOrderItemsList[i].Id).addClass('hid');
					if ($("a" + FilterOrderItemsList[i].Id).hasClass('hid'))
						$("a" + FilterOrderItemsList[i].Id).removeClass('hid');
				} else {
					$("a" + FilterOrderItemsList[i].Id).attr("href", "");
					if ($("span" + FilterOrderItemsList[i].Id).hasClass('hid'))
						$("span" + FilterOrderItemsList[i].Id).removeClass('hid');
					if ($("a" + FilterOrderItemsList[i].Id).hasClass('hid') == false)
						$("a" + FilterOrderItemsList[i].Id).addClass('hid');
				}
			}
		}
		//для Всех 
		if (clientType == "") {
			for (var i = 0; i < FilterOrderItemsList.length; i++) {
				if ($("span" + FilterOrderItemsList[i].Id).hasClass('hid'))
					$("span" + FilterOrderItemsList[i].Id).removeClass('hid');
				if ($("a" + FilterOrderItemsList[i].Id).hasClass('hid') == false)
					$("a" + FilterOrderItemsList[i].Id).addClass('hid');
			}
		}
	} catch (ex) {
	}
}

//установка фильтра по типу клиента в начальное состояние
function SetFilterStateByClientType(clientType) {
	///переименовываем текст фильтра
	$("#FilterCondition_ClientType option").each(function() {
		if ($(this).html() == " ") $(this).html("Все");
		if ($(this).html() == "Да") $(this).html("Физ. лицо");
		if ($(this).html() == "Нет") $(this).html("Юр. лицо");
	});
	//обработка фильтров
	SetFilterCondition(clientType);
	//обработка сортировки
	SetFilterOrder(clientType);
	//при нажатии условия обновления фильтров и положения кнопок "сброса значения фильтра"
	$("#FilterCondition_ClientType").change(function() {
		SetFilterCondition($(this).val());
		//регульровка положения кнопок "сброса значения фильтра"
		setCleanFilterButtonAtPlace();
	});

}

//регулировка видимости блока с дополнительными фильтрами
function additionalBlockVisibility() {
	//проверяем, есть ли заполненные поля в блоке
	var anyInputHasValue;
	$(".additionalFilterBlock input, .additionalFilterBlock select").each(function() {
		if ($(this).val() != null && $(this).val() != "" && $(this).attr("id") != "OpenInANewTab" && $(this).attr("name") != "openInANewTab") {
			anyInputHasValue = true;
		}
	});
	if (anyInputHasValue) {
		$(".additionalFilterBlock").removeClass("hid");
		$(".filterBlockHeader.additional").removeClass("entypo-right-open-mini").addClass("entypo-down-open-mini");
	} else {
		if ($(".additionalFilterBlock").hasClass("hid")) {
			$(".additionalFilterBlock").addClass("hid");
			$(".filterBlockHeader.additional").removeClass("entypo-down-open-mini").addClass("entypo-right-open-mini");
		}
	}
	//по клику меняем значение видимости
	$(".filterBlockHeader.additional").click(function() {
		if ($(".additionalFilterBlock").hasClass("hid")) {
			$(".additionalFilterBlock").removeClass("hid");
			$(".filterBlockHeader.additional").addClass("entypo-down-open-mini").removeClass("entypo-right-open-mini");
		} else {
			$(".additionalFilterBlock").addClass("hid");
			$(".filterBlockHeader.additional").addClass("entypo-right-open-mini").removeClass("entypo-down-open-mini");
		}
		//регульровка положения кнопок "сброса значения фильтра"
		setCleanFilterButtonAtPlace();
	});
}

//добавление кнопок "сброса значения фильтра"
function AddClearFilterButton() {
	//создание кнопок "сброса значения фильтра"
	var cleanIndex = 0;
	$(".clientTableFilter .col-sm-3 input, .clientTableFilter .col-sm-3 select, .clientTableFilter .col-sm-4 input, .clientTableFilter .col-sm-4 select").each(function() {
		$(this).addClass("cleanIndex" + cleanIndex);
		var buttonElementHtml = "<a class='entypo-cancel-circled cleanButton index" + cleanIndex + "' title='очистить' onclick='cleanFilter(\".cleanIndex" + cleanIndex + "\")'></a>";
		$(this).after(buttonElementHtml);
		$(".cleanButton.index" + cleanIndex).offset({
			top: ($(this).offset().top + $(this).height() - ($(".cleanButton.index" + cleanIndex).height()) + CleanFilterButtonAtPlaceTop),
			left: ($(this).offset().left + $(this).width() - ($(".cleanButton.index" + cleanIndex).width()) + CleanFilterButtonAtPlaceLeft)
		});
		cleanIndex++;
	});
	//при низменении размера окна
	$(window).resize(function() {
		//регульровка положения кнопок "сброса значения фильтра"
		setCleanFilterButtonAtPlace();
	});
}

//регульровка положения кнопок "сброса значения фильтра"
function setCleanFilterButtonAtPlace() {
	var cleanIndex = 0;
	$(".clientTableFilter .col-sm-3 input, .clientTableFilter .col-sm-3 select, .clientTableFilter .col-sm-4 input, .clientTableFilter .col-sm-4 select").each(function() {
		$(".cleanButton.index" + cleanIndex).offset({
			top: ($(this).offset().top + $(this).height() - ($(".cleanButton.index" + cleanIndex).height()) + CleanFilterButtonAtPlaceTop),
			left: ($(this).offset().left + $(this).width() - ($(".cleanButton.index" + cleanIndex).width()) + CleanFilterButtonAtPlaceLeft)
		});
		cleanIndex++;
	});
}

//сброса значения фильтра
function cleanFilter(cleanIndex) {
	$(cleanIndex).val("");
	$(cleanIndex).change();
}

//замена знаков
function replaceAll(str, find, replace) {
	return str.replace(new RegExp(find, 'g'), replace);
}

//добавление скролла на таблицу при сортировке
function addScrollBackToLinks() {
	var sortItems = $(".clientTable .sorting a");
	var tagPhrase = "scrollBackToTheTable=true&";
	$(sortItems).each(function() {
		var indexOfStart = $(this).attr("href").indexOf("?") + 1;
		var newUrl = $(this).attr("href");
		newUrl = newUrl.substr(0, indexOfStart) + tagPhrase + newUrl.substr(indexOfStart);
		console.log(newUrl);
		$(this).attr("href", newUrl);
	});

	if (window.location.href.indexOf("scrollBackToTheTable") != -1) {
		scrollTo(".clientTable");
	}
}

//копирование контактов, при нажатии по ним
function contactsCopyToKeyboard() {
	// | копирование контактов, при нажатии по ним |======>>
	$("tbody td span").click(function() {
		if ($(this).hasClass("contactText") == false) {
			$('.contactText').removeClass("ch");
		}
	});
	$('.contactText').click(function() {
		var hasCh = $(this).hasClass("ch");
		$('.contactText').removeClass("ch");
		// Select the email link anchor text  
		var emailLink = $(this)[0];
		try {

			var range = document.createRange();
			range.selectNode(emailLink);
			window.getSelection().addRange(range);

			var successful = document.execCommand('copy');
			if (successful == false) {
				$('.contactText').removeClass("ch");
			}
			if (successful && $(this).hasClass("ch") == false) {
				$(this).addClass("ch");
			}
			try {
				if (window.clipboardData.getData('Text') != $(emailLink).html()) {
					$('.contactText').removeClass("ch");
				}
			} catch (err) {
			}
		} catch (err) {
			$('.contactText').removeClass("ch");
		}
		// Remove the selections - NOTE: Should use   
		// removeRange(range) when it is supported  
		window.getSelection().removeAllRanges();
	});
	// <<======| копирование контактов, при нажатии по ним |

}


//вызов функций при загрузке, дополнительные действия
$(function() {

	SetFilterStateByClientType($("#FilterCondition_ClientType").val());
	contactsCopyToKeyboard();
	additionalBlockVisibility();
	AddClearFilterButton();
	checkCurrentOrderUrlCorrespond();
	addScrollBackToLinks();

	/////Формирование адреса клиента
	$(".addressFilterBlock input, .addressFilterBlock select").change(function() {
		if ($(this).val() != "") {
			$("#fullAdressSearch").val("");
		}
	});
	$("#fullAdressSearch").change(function() {
		if ($(this).val() != "") {
			$(".addressFilterBlock input, .addressFilterBlock select").val("");
		}
	});
	$("#StreetDropDown").html("");
	$("#HouseDropDown").html("");

	$('select[name="mfilter.filter.Equal.AppealType"] option[value="All"]').remove();

});

//Добавление в куки сведений о необходимости открывать единственную запись в новой вкладке 
$(function() {
	var cookieValue = $.cookie("OpenInANewTab");
	if (cookieValue != null && cookieValue == "true") {
		if ($("#OpenInANewTab").is(":checked") == false) {
			$("#OpenInANewTab").attr("checked", "checked");
		}
	} else {
		$("#OpenInANewTab").removeAttr("checked");
	}
	$("#OpenInANewTab").click(function() {
		var valueOfCookie = $("#OpenInANewTab").is(":checked") + "";
		$.cookie("OpenInANewTab", valueOfCookie, { expires: 365, path: "/" });
	});
});


//IP=>Число
function dot2num(dot) {
	var ipVal = "";
	console.log(isNaN(dot) + " | " + dot);
	try {
		var d = dot.split('.');
		ipVal = ((((((+d[0]) * 256) + (+d[1])) * 256) + (+d[2])) * 256) + (+d[3])
	} catch (e) {
		ipVal = "";
	}
	if (isNaN(ipVal) == true) {
		return "";
	}
	return ipVal;
}

//Число=>IP
function num2dot(num) {
	var d = "";
	try {
		d = num % 256;
		for (var i = 3; i > 0; i--) {
			num = Math.floor(num / 256);
			d = num % 256 + '.' + d;
		}
	} catch (e) {
		d = "";
	}
	return d;
}


function GetSwitchesByZone(zoneDropdownName, switchesDropdownName) {
	var zoneItem = $("[name='" + zoneDropdownName + "']");
	var switchItem = $("[name='" + switchesDropdownName + "']");
	if (zoneItem.length > 0 && switchItem.length > 0) {
		zoneItem.change(function() {
			$.ajax({
				url: cli.getParam("baseurl") + "Client/getSwitchesByZone?name=" + zoneItem.val(),
				type: 'POST',
				dataType: "json",
				success: function(data) {
					var htmlOptopns = "<option value=\"\"> </option>";
					if (data != null) {
						for (var i = 0; i < data.length; i++) {
							htmlOptopns += "<option value=\"" + data[i] + "\">" + data[i] + "</option>";
						}
					}
					switchItem.html(htmlOptopns);
					if (switchItem.attr("pastValue") != null) {
						switchItem.find("option[value='" + switchItem.attr("pastValue") + "']").attr("selected", "selected");
						switchItem.removeAttr("pastValue");
					}
				},
				error: function(data) {
					switchItem.html("");
				},
				statusCode: {
					404: function() {
						switchItem.html("");
					}
				}
			});
		});
		var currentValOption = switchItem.find("option[selected='selected']");
		if (currentValOption.length > 0) {
			var currentVal = currentValOption.val();
			switchItem.attr("pastValue", currentVal);
			zoneItem.change();
		}
	}
}

//Добавление в куки сведений о необходимости открывать единственную запись в новой вкладке 
var ipRentSerchName = "input[name='mfilter.filter.Equal.Endpoints.First().LeaseList.First().Ip'],input[name='mfilter.filter.Equal.Endpoint.LeaseList.First().Ip']";
$(function() {
	var ipEqualValue = $(ipRentSerchName).val();
	if (ipEqualValue != "" && ipEqualValue != null) {
		ipEqualValue = num2dot(ipEqualValue);
		$("#ipEqualShown").val(ipEqualValue);
	}
	$("#ipEqualShown").change(function() {
		var ipEqualText = $("#ipEqualShown").val();
		if (ipEqualText != "" && ipEqualText != null && ipEqualText != undefined) {
			ipEqualText = dot2num(ipEqualText);
			if (ipEqualText == "") {
				console.log("1|" + ipEqualText);
				$("#ipEqualShown").css("background", "#FB7C7C");
				$(ipRentSerchName).val("");
			} else {
				console.log("2|" + ipEqualText);
				$("#ipEqualShown").css("background", "none");
				$(ipRentSerchName).val(ipEqualText);
			}
		} else {
			$(ipRentSerchName).val("");
		}
	});
	GetSwitchesByZone("mfilter.filter.Equal.Endpoint.Switch.Zone.Name", "mfilter.filter.Equal.Endpoint.Switch.Name");
});