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
	//обновление фильтра адреса
	setAddressFilter("#AddressHidenElement");
}

//получение значения фильтра по адресам (полнотекстовый поиск)
function getAddressFilter(addressHidenElement) {
	var s = $(addressHidenElement).val();
	s = s == "" ? "улица %дом %квартира %подъезд %этаж%" : s;
	var splitedAddress = s.split('%');
	var addressStreet = "", addressHouse = "", addressApartment = "", addressEntrance = "", addressFloor = "";
	//проверяем отформатировани ли адрес
	for (var i = 0; i < splitedAddress.length; i++) {
		if (splitedAddress[i].lastIndexOf("улица") != -1) {
			addressStreet = splitedAddress[i].substring(splitedAddress[i].lastIndexOf("улица") + "улица".length);
			addressStreet = addressStreet.replace(" ", "").length == 0 ? "" : addressStreet;
		}
		if (splitedAddress[i].lastIndexOf("дом") != -1) {
			addressHouse = splitedAddress[i].substring(splitedAddress[i].lastIndexOf("дом") + "дом".length);
			addressHouse = addressHouse.replace(" ", "").length == 0 ? "" : addressHouse;
		}
		if (splitedAddress[i].lastIndexOf("квартира") != -1) {
			addressApartment = splitedAddress[i].substring(splitedAddress[i].lastIndexOf("квартира") + "квартира".length);
			addressApartment = addressApartment.replace(" ", "").length == 0 ? "" : addressApartment;
		}
		if (splitedAddress[i].lastIndexOf("подъезд") != -1) {
			addressEntrance = splitedAddress[i].substring(splitedAddress[i].lastIndexOf("подъезд") + "подъезд".length);
			addressEntrance = addressEntrance.replace(" ", "").length == 0 ? "" : addressEntrance;
		}
		if (splitedAddress[i].lastIndexOf("этаж") != -1) {
			addressFloor = splitedAddress[i].substring(splitedAddress[i].lastIndexOf("этаж") + "этаж".length);
			addressFloor = addressFloor.replace(" ", "").length == 0 ? "" : addressFloor;
		}
	}
	//собираем адрес из "форматированных частей", если такие имеются
	var currentAddress = addressStreet;
	currentAddress += addressHouse;
	currentAddress += addressApartment;
	currentAddress += addressEntrance;
	currentAddress += addressFloor;
	//если значения не форматированны, используем полнотекстный поиск
	if (currentAddress == null || currentAddress == "") {
		$("#searchAutoAddress").val(replaceAll($(addressHidenElement).val(), "%", "."));
		return;
	}

	$("#searchStreet").val(addressStreet);
	$("#searchHouse").val(addressHouse);
	$("#searchApartment").val(addressApartment);
	$("#searchEntrance").val(addressEntrance);
	$("#searchFloor").val(addressFloor);
}

//замена знаков
function replaceAll(str, find, replace) {
	return str.replace(new RegExp(find, 'g'), replace);
}

//задание значения фильтра по адресам (полнотекстовый поиск)
function setAddressFilter(addressHidenElement) {
	var addressAuto = $("#searchAutoAddress").val();
	if (addressAuto != null && addressAuto != "") {
		while (addressAuto.indexOf(".") != -1) {
			addressAuto = addressAuto.replace(".", "%");
		}
		while (addressAuto.indexOf(",") != -1) {
			addressAuto = addressAuto.replace(",", "%");
		}
		$(addressHidenElement).val(addressAuto);
		return;
	}

	var addressStreet = $("#searchStreet").val();
	var addressHouse = $("#searchHouse").val();
	var addressApartment = $("#searchApartment").val();
	var addressEntrance = $("#searchEntrance").val();
	var addressFloor = $("#searchFloor").val();

	var currentAddress = addressStreet != "" ? "улица " + addressStreet + "%" : "%";
	currentAddress += addressHouse != "" ? "дом " + addressHouse + "%" : "%";
	currentAddress += addressApartment != "" ? "квартира " + addressApartment + "%" : "%";
	currentAddress += addressEntrance != "" ? "подъезд " + addressEntrance + "%" : "%";
	currentAddress += addressFloor != "" ? "этаж " + addressFloor + "%" : "%";

	currentAddress = currentAddress.replace("%%%%", "%").replace("%%%", "%").replace("%%", "%");
	currentAddress = replaceAll(currentAddress, "  ", " ");
	$(addressHidenElement).val(currentAddress);
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

	///Формирование адреса клиента
	getAddressFilter("#AddressHidenElement");
	$(".addressSearch").change(function() {
		setAddressFilter("#AddressHidenElement");
		if ($(this).attr("id") == $("#searchAutoAddress").attr("id")) {
			$(".addressSearch").each(function() {
				if ($(this).attr("id") != $("#searchAutoAddress").attr("id")) {
					$(this).val("");
				}
			});
		} else {
			$("#searchAutoAddress").val("");
		}
	});

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