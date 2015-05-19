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
 
	 

// AJAX-Ззапросы
getStreetList = function (regionId, funcAfter) {
	$.ajax({
		url: cli.getParam("baseurl") + "/Address/GetStreetList?regionId=" + regionId,
		type: 'POST',
		dataType: "json",
		success: function (data) {
			//функция заполнения списка улиц
			funcAfter(data);
		},
		error: function (request) {
		}
	});
}
getHouseList = function (streetId, regionId, funcAfter) {
	$.ajax({
		url: cli.getParam("baseurl") + "/Address/GetHouseList?streetId=" + streetId + "&regionId=" + regionId,
		type: 'POST',
		dataType: "json",
		success: function (data) {
			//функция заполнения списка домов  
			funcAfter(data);
		},
		error: function (request) {
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
	var tmp = $("#StreetDropDown option").clone();
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
}
getHouseFuncAfter = function (data) {
	var tmp = $("#HouseDropDown option").clone();
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
	var tmp = $("#PlanDropDown option").clone();
	$("#PlanDropDown").html("<option></option>");
	// заполнение списка тарифов
	$(data).each(function () {
		var el = tmp.clone();
		el.attr(addressHelper.getPlanIdAttribute(), this.Id);
		el.val(this[addressHelper.getPlanValueType()]);
		el.html(this.Name);
		$("#PlanDropDown").append(el);
	});
	$("#PlanDropDown").val($("#PlanDropDown option:first"));
}


// при изменении значения региона
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
		getStreetList($(_this).attr(addressHelper.getRegionIdAttribute()), getStreetFuncAfter);
	}
});

// при изменении значения улицы
$("#StreetDropDown").change(function () {
	var _this = $("#StreetDropDown :selected");
	if ($(_this).attr(addressHelper.getStreetIdAttribute()) != "") { 
		getHouseList($(_this).attr(addressHelper.getStreetIdAttribute()),
			$("#RegionDropDown :selected").attr(addressHelper.getRegionIdAttribute()), getHouseFuncAfter);
	}
});


if (($("#StreetDropDown option").length == 0 || $("#StreetDropDown option:first").val() == null)
	&& ($("#streetError") == null || $("#streetError").val() != "0")) {
	// получения значений для текущего региона
	if ($("#RegionDropDown option").length > 0 && $("#RegionDropDown").val() != "") {
		$("#RegionDropDown").val($("#RegionDropDown option:first"));
		getStreetList($("#RegionDropDown :selecte").attr(addressHelper.getRegionIdAttribute()), getStreetFuncAfter);
	}
}

if (($("#HouseDropDown option").length == 0 || $("#HouseDropDown option:first").val() == null)
	&& ($("#houseError") == null || $("#houseError").val() != "0")) {
	// получения значений для текущей улицы
	if ($("#StreetDropDown option").length > 0) {
		$("#StreetDropDown").val($("#StreetDropDown option:first"));
		getHouseList($("#StreetDropDown :selecte").attr(addressHelper.getStreetIdAttribute()),
			$("#RegionDropDown :selecte").attr(addressHelper.getRegionIdAttribute()), getHouseFuncAfter);
	}
}