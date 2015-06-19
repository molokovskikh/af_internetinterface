function AddressHelper() {
	///////////////////////|-Region-|///////////////////////////
	var regionIdAttr = "value";
	var regionValueType = "Id";
	this.getRegionValueType = function () {
		return regionValueType;
	}
	this.setRegionValueType = function (name) {
		regionValueType = name;
	}
	this.setRegionIdAttribute = function (name) {
		regionIdAttr = name;
	}
	this.getRegionIdAttribute = function () {
		return regionIdAttr;
	}
	////////////////////////|-Street-|//////////////////////////
	var streetIdAttr = "value";
	var streetValueType = "Id";
	this.getStreetValueType = function () {
		return streetValueType;
	}
	this.setStreetValueType = function (name) {
		streetValueType = name;
	}
	this.setStreetIdAttribute = function (name) {
		streetIdAttr = name;
	}
	this.getStreetIdAttribute = function () {
		return streetIdAttr;
	}
	////////////////////////|-House-|//////////////////////////
	var houseIdAttr = "value";
	var houseValueType = "Id";
	this.getHouseValueType = function () {
		return houseValueType;
	}
	this.setHouseValueType = function (name) {
		houseValueType = name;
	}
	this.setHouseIdAttribute = function (name) {
		houseIdAttr = name;
	}
	this.getHouseIdAttribute = function () {
		return houseIdAttr;
	}
	////////////////////////|-PLAN-|//////////////////////////
	var planIdAttr = "value";
	var planValueType = "Id";
	this.getPlanValueType = function () {
		return planValueType;
	}
	this.setPlanValueType = function (name) {
		planValueType = name;
	}
	this.setPlanIdAttribute = function (name) {
		planIdAttr = name;
	}
	this.getPlanIdAttribute = function () {
		return planIdAttr;
	}
}
var addressHelper = new AddressHelper();
var addressChangeFlagTimerSpeed = 100;


// AJAX-Ззапросы
getStreetChangedFlag = function (regionId, countStreet) {
	if (regionId == null || countStreet == null) {
		setTimeout(function () { streetChangedCheck(false); }, addressChangeFlagTimerSpeed);
		return;
	}
	$.ajax({
		url: cli.getParam("baseurl") + "/Address/GetStreetNumberChangedFlag?regionId=" + regionId + "&count=" + countStreet,
		type: 'POST',
		dataType: "json",
		success: function (data) {
			//функция заполнения списка улиц
			setTimeout(function () { streetChangedCheck(data); }, addressChangeFlagTimerSpeed);
		},
		error: function (data) {
			console.log(regionId + " countStreet-" + countStreet + " " + data);
		}
	});
}
getHouseChangedFlag = function (streetId, regionId, countHouse) {
	if (regionId == null || streetId == null || countHouse == null) {
		setTimeout(function () { houseChangedCheck(false); }, addressChangeFlagTimerSpeed);
		return;
	}
	$.ajax({
		url: cli.getParam("baseurl") + "/Address/GetHouseNumberChangedFlag?streetId=" + streetId + "&count=" + countHouse + "&regionId=" + regionId,
		type: 'POST',
		dataType: "json",
		success: function (data) {
			//функция заполнения списка улиц
			setTimeout(function () { houseChangedCheck(data); }, addressChangeFlagTimerSpeed);
		},
		error: function (data) {
			console.log(streetId + " countHouse-" + countHouse + " " + data);
		}
	});
}


getStreetList = function (regionId, funcAfter, countStreet) {
	if (regionId == null || countStreet == null) {
		setTimeout(function () { streetChangedCheck(false); }, addressChangeFlagTimerSpeed);
		return;
	}
	$.ajax({
		url: cli.getParam("baseurl") + "/Address/GetStreetList?regionId=" + regionId,
		type: 'POST',
		dataType: "json",
		success: function (data) {
			//функция заполнения списка улиц
			funcAfter(data);
			setTimeout(function () { streetChangedCheck(false); }, addressChangeFlagTimerSpeed);
		},
		error: function (request) {
			setTimeout(function () { streetChangedCheck(false); }, addressChangeFlagTimerSpeed);
		}
	});
}
getHouseList = function (streetId, regionId, funcAfter, countHouse) {
	if (regionId == null || streetId == null || countHouse == null) {
		setTimeout(function () { houseChangedCheck(false); }, addressChangeFlagTimerSpeed);
		return;
	}
	$.ajax({
		url: cli.getParam("baseurl") + "/Address/GetHouseList?streetId=" + streetId + "&regionId=" + regionId,
		type: 'POST',
		dataType: "json",
		success: function (data) {
			//функция заполнения списка домов  
			funcAfter(data);
			setTimeout(function () { houseChangedCheck(false); }, addressChangeFlagTimerSpeed);
		},
		error: function (request) {
			setTimeout(function () { houseChangedCheck(false); }, addressChangeFlagTimerSpeed);
		}
	});
}
getPlansList = function (regionId, funcAfter) {
	$.ajax({
		url: cli.getParam("baseurl") + "/Address/GetPlansListForRegion?regionId=" + regionId,
		type: 'POST',
		dataType: "json",
		success: function (data) {
			//функция заполнения списка тарифов
			funcAfter(data);
		},
		error: function (request) {
		}
	});
}


// После AJAX-запросов
getStreetFuncAfter = function (data) {
	var tmp = $("#StreetDropDown option:last").clone();
	$("#StreetDropDown").html("<option></option>");
	// заполнение списка улиц
	$(data).each(function () {
		var el = tmp.clone();
		el.attr(addressHelper.getStreetIdAttribute(), this.Id);
		el.val(this[addressHelper.getStreetValueType()]);
		el.html(this.Name);
		$("#StreetDropDown").append(el);
	});
	$("#StreetDropDown").val($("#StreetDropDown option:first"));
	$("#HouseDropDown").html("<option selected='selected'></option>");
}
getHouseFuncAfter = function (data) {
	var tmp = $("#HouseDropDown option:last").clone();
	$("#HouseDropDown").html("<option></option>");
	// заполнение списка домов
	$(data).each(function () {
		var el = tmp.clone();
		el.attr(addressHelper.getHouseIdAttribute(), this.Id);
		el.val(this[addressHelper.getHouseValueType()]);
		el.html(this.Number);
		$("#HouseDropDown").append(el);
	});
	$("#HouseDropDown").val($("#HouseDropDown option:first"));
}
getPlansFuncAfter = function (data) {
	var tmp = $("#PlanDropDown option:last").clone();
	$("#PlanDropDown").html("<option></option>");
	// заполнение списка тарифов
	$(data).each(function () {
		var el = tmp.clone();
		el.attr(addressHelper.getPlanValueType(), this.Id);
		el.val(this.Id);
		el.html(this.Name);
		$("#PlanDropDown").append(el);
	});
	$("#PlanDropDown").val($("#PlanDropDown option:first"));
}


streetChangedCheck = function (data) {
	if (data) {
		// получения значений для текущего региона  
		if ($("#RegionDropDown option").length > 0 && $("#RegionDropDown").val() != "") {
			getStreetList($("#RegionDropDown :selected").attr(addressHelper.getRegionIdAttribute()), getStreetFuncAfter, $("#StreetDropDown option").length - 1);
			return;
		}
	}
	getStreetChangedFlag($("#RegionDropDown :selected").attr(addressHelper.getRegionIdAttribute()), $("#StreetDropDown option").length - 1);
}
houseChangedCheck = function (data) {
	if (data) {
		// получения значений для текущей улицы
		if ($("#StreetDropDown option").length > 0 && $("#StreetDropDown").val() != "") {
			getHouseList($("#StreetDropDown :selected").attr(addressHelper.getStreetIdAttribute()),
				$("#RegionDropDown :selected").attr(addressHelper.getRegionIdAttribute()), getHouseFuncAfter, $("#HouseDropDown option").length - 1);
			return;
		}
	}
	getHouseChangedFlag($("#StreetDropDown :selected").attr(addressHelper.getStreetIdAttribute())
		, $("#RegionDropDown :selected").attr(addressHelper.getRegionIdAttribute()), $("#HouseDropDown option").length - 1);
}

//при изменении значения региона
$("#RegionDropDown").change(function () {

	$("#StreetDropDown").html("<option selected='selected'></option>");
	$("#HouseDropDown").html("<option selected='selected'></option>");
	$("#PlanDropDown").html("<option selected='selected'></option>");

	$("#HouseDropDown").val($("#HouseDropDown option:first"));
	$("#StreetDropDown").val($("#StreetDropDown option:first"));
	$("#PlanDropDown").val($("#PlanDropDown option:first"));

	var _this = $("#RegionDropDown :selected");

	if ($(_this).attr(addressHelper.getRegionIdAttribute()) != null && $(_this).attr(addressHelper.getRegionIdAttribute()) != "") {
		getPlansList($(_this).attr(addressHelper.getRegionIdAttribute()), getPlansFuncAfter);
	}
});

// при изменении значения улицы
$("#StreetDropDown").change(function () {
	$("#HouseDropDown").html("<option selected='selected'></option>");
});

if ($("#StreetDropDown option:first").attr("val") != null) {
	$("#StreetDropDown").prepend("<option selected='selected'></option>");
}
if ($("#HouseDropDown option:first").attr("val") != null) {
	$("#HouseDropDown").prepend("<option selected='selected'></option>");
}
if ($("#PlanDropDown option:first").attr("val") != null) {
	$("#PlanDropDown").prepend("<option selected='selected'></option>");
}

$("#addStreetButton").click(function (event) {

	var value = $("#RegionDropDown :selected").attr(addressHelper.getRegionIdAttribute());
	if (value != null) {
		if ($(this).attr("href_temp") == null) {
			$(this).attr("href_temp", $(this).attr("href"));
		}
		$(this).attr("href", $(this).attr("href_temp") + "?regionId=" + value);
	}
});
$("#addHouseButton").click(function (event) {
	var value = $("#StreetDropDown :selected").attr(addressHelper.getRegionIdAttribute());
	if (value != null) {
		if ($(this).attr("href_temp") == null) {
			$(this).attr("href_temp", $(this).attr("href"));
		}
		$(this).attr("href", $(this).attr("href_temp") + "?streetId=" + value);
	}
});
streetChangedCheck(false);
houseChangedCheck(false);