getStreetList = function (regionId, funcAfter) {
	$.ajax({
		url: "/Address/GetStreetList?regionId=" + regionId,
		type: 'POST',
		dataType: "json",
		success: function (data) {
			funcAfter(data);
		},
		error: function (request) { 
		}
	});
}
getHouseList = function (streetId, funcAfter) { 
	$.ajax({
		url: "/Address/GetHouseList?streetId=" + streetId,
		type: 'POST',
		dataType: "json",
		success: function (data) {
			funcAfter(data);
		},
		error: function (request) { 
		}
	});
}

getStreetFuncAfter = function (data) {
	$("#streetListBox").html("");
	$(data).each(function () {
		$("#streetListBox").append("<option value='" + this.Id + "'>" + this.Name + "</option>");
	});
	if ($("#streetListBox option").length > 0) {
		$("#streetListBox select").val($("#streetListBox option:first"));
		getHouseList($("#streetListBox").val(), getHouseFuncAfter);
	} else {
		$("#houseListBox").html("");
	}

}
getHouseFuncAfter = function (data) {
	$("#houseListBox").html("");
	$(data).each(function() {
		$("#houseListBox").append("<option value='" + this.Id + "'>" + this.Number + "</option>");
	});
	if ($("#houseListBox option").length > 0) {
		$("#houseListBox select").val($("#houseListBox option:first"));
	}
}

$(function () {
	$("#regionListBox").change(function () {
		if ($(this).val()!="") {
			getStreetList($(this).val(),getStreetFuncAfter );
		}
	});
	$("#streetListBox").change(function () {
		if ($(this).val() != "") {
			getHouseList($(this).val(), getHouseFuncAfter);
		}
	});
	$("#houseListBox").change(function () { 
	});

	if ($("#regionListBox option").length > 0) {
		$("#regionListBox select").val($("#regionListBox option:first"));
		getStreetList($("#regionListBox").val(), getStreetFuncAfter);
	}
	if ($("#streetListBox option").length > 0) {
		$("#streetListBox select").val($("#streetListBox option:first"));
		getHouseList($("#streetListBox").val(), getHouseFuncAfter);
	} 
});