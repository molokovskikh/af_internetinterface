﻿// В первую очередь
$(function () {
	//   

	// при изменении значения региона
	$("#RegionDropDown").change(function () {
		
		$("#StreetDropDown").html("<option selected='selected'></option>");
		$("#HouseDropDown").html("<option selected='selected'></option>");
		$("#PlanDropDown").html("<option selected='selected'></option>");

		$("#HouseDropDown select").val($("#HouseDropDown option:first"));
		$("#StreetDropDown select").val($("#StreetDropDown option:first"));
		$("#PlanDropDown select").val($("#PlanDropDown option:first"));

		if ($(this).val() != null && $(this).val() != "") {
			getPlansList($(this).val(), getPlansFuncAfter);
			getStreetList($(this).val(), getStreetFuncAfter);
		}
	});
	// при изменении значения улицы
	$("#StreetDropDown").change(function () {
		if ($(this).val() != "") {
			getHouseList($(this).val(), getHouseFuncAfter);
		}
	});
	if (($("#StreetDropDown option").length == 0 || $("#StreetDropDown option:first").val() == null)
		&& ($("#streetError") == null || $("#streetError").val() != "0")) {
		// получения значений для текущего региона
		if ($("#RegionDropDown option").length > 0 && $("#RegionDropDown").val() != "") {
			$("#RegionDropDown select").val($("#RegionDropDown option:first"));
			getStreetList($("#RegionDropDown").val(), getStreetFuncAfter);
		}
	}  

	if (($("#HouseDropDown option").length == 0 || $("#HouseDropDown option:first").val() == null)
		&& ($("#houseError") == null || $("#houseError").val() != "0")) {
		// получения значений для текущей улицы
		if ($("#StreetDropDown option").length > 0) {
			$("#StreetDropDown select").val($("#StreetDropDown option:first"));
			getHouseList($("#StreetDropDown").val(), getHouseFuncAfter);
		}
	}

		$("#StreetDropDown").click(function () {
			$("#RegionDropDown").unbind("click");
			if ($("#StreetDropDown option").length == 0) {
			$("#RegionDropDown select").val($("#RegionDropDown option:first"));
			getStreetList($("#RegionDropDown").val(), getStreetFuncAfter);
		}
		});
		$("#HouseDropDown").click(function () {
			$("#HouseDropDown").unbind("click");
			if ($("#HouseDropDown option").length == 0) {
			$("#StreetDropDown select").val($("#StreetDropDown option:first"));
			getHouseList($("#StreetDropDown").val(), getHouseFuncAfter);
		}
		});

});

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
getHouseList = function (streetId, funcAfter) { 
	$.ajax({
		url: cli.getParam("baseurl") + "/Address/GetHouseList?streetId=" + streetId,
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
		url: cli.getParam("baseurl")+"/Address/GetPlansListForRegion?regionId=" + regionId,
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
	$("#StreetDropDown").html("<option></option>");
	$(data).each(function () {
		// заполнение списка улиц
		$("#StreetDropDown").append("<option value='" + this.Id + "'>" + this.Name + "</option>");
	}); 
	// если список не пустой, выбор первого элемента и запрос домов по улице, иначе - очистка списка домов
	if ($("#StreetDropDown option").length > 0 || ($("#StreetDropDown option").length >= 1 && $("#StreetDropDown option:first").val() == null)) {
		$("#StreetDropDown select").val($("#StreetDropDown option:first"));
		getHouseList($("#StreetDropDown").val(), getHouseFuncAfter);
	} else {
		$("#HouseDropDown").html("<option></option>");
	}
}
getHouseFuncAfter = function (data) {
	$("#HouseDropDown").html("<option></option>");
	$(data).each(function () {
		// заполнение списка домов
		$("#HouseDropDown").append("<option value='" + this.Id + "'>" + this.Number + "</option>");
	});
	// если список не пустой, выбор первого элемента
	if ($("#HouseDropDown option").length > 0 ||($("#HouseDropDown option").length >= 1 && $("#HouseDropDown option:first").val() == null)) {
		$("#HouseDropDown select").val($("#HouseDropDown option:first"));
	}
}
getPlansFuncAfter = function (data) {
	$("#PlanDropDown").html("<option></option>");
	$(data).each(function () {
		// заполнение списка тарифов 
		$("#PlanDropDown").append("<option value='" + this.Id + "'>" + this.Name + "</option>");
	});
	// если список не пустой, выбор первого элемента
	if ($("#PlanDropDown option").length > 0 || ($("#PlanDropDown option").length >= 1 && $("#PlanDropDown option:first").val() == null)) {
		$("#PlanDropDown select").val($("#PlanDropDown option:first"));
	}
}