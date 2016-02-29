
var modelWindow = "#RequestMarkerColorChange ";

$(function() {
	//инициализация объектов, просставление исходных значений
	var colorPicker = $(modelWindow + '[name="color"]');
	if (colorPicker.length != 0) {
		colorPicker.colorpicker().on('changeColor.colorpicker', function(event) {
			$(modelWindow + "[name = 'colorExample']").attr("style", "width: 100%; padding-right: 4px;border-right: " + $(modelWindow + "[name = 'color']").val() + " 14px solid;");
		});
	}
	//инициализация происходит с секундной задержкой, скрываем содержимое списка при загрузке, после инициализации убираем скрытие.
	var multipleDropDown = $('.multiple.search');
	if (multipleDropDown.length > 0) {

		multipleDropDown.removeClass("hid");
		multipleDropDown.dropdown({
			onChange: function(value, text, $selectedItem) {
				$("input[name='requestMarkers']").val(value);
			}
		});
		//задаем исходное значение списку маркеров
		$('.multiple.search').dropdown('set selected', $("input[name='requestMarkers']").val().split(','));
	}
	$("#ConnectionRequestMarkerDropDown").change(function() {
		onConnectionRequestMarkerDropDowntChange(this);

	});
	$("[name='markerList']").change(function() {
		onMarkerListChange(this);
	});

	$(".markedItem .itemId").click(function() {
		var stCurent = $(this).attr("style");
		if (stCurent == undefined || stCurent == null) {
			$(this).parents(".markedItem").find("input[dataTitle='itemId']").val($(this).html());
			var color = $("#ConnectionRequestMarkerDropDown option[value='" + $("#ConnectionRequestMarkerDropDown").val() + "']").attr("color");
			color = color == undefined ? "#E0E0E0" : color;
			$(this).attr("style", "padding-right: 5px;border-right: " + color + " 14px solid;");
		} else {
			$(this).parents(".markedItem").find("input[dataTitle='itemId']").val("0");
			$(this).removeAttr("style");
		}
	});


});

function onMarkerListChange(_this) {
	var marker = $(modelWindow + "[name='markerList'] option[value='" + $(_this).val() + "']");
	var name = $(marker).attr("title");
	var color = $(marker).attr("color");
	var deleted = $(marker).attr("deleted");
	$("#clientReciverMessage").html("");
	if (deleted != "true") {
		if (!$(modelWindow + ".btn.btn-red").hasClass("hid")) {
			$(modelWindow + ".btn.btn-red").addClass("hid");
		}
	} else {
		$(modelWindow + ".btn.btn-red").removeClass("hid");
	}
	$(modelWindow + "[name = 'name']").val(name);
	$(modelWindow + '[name="color"]').data('colorpicker').color.setColor(color);
	$(modelWindow + "[name = 'color']").val(color);

	$(modelWindow + "[name = 'colorExample']").attr("style", "padding-right: 5px;border-right: " + $(modelWindow + "[name = 'color']").val() + " 14px solid;");

}

function onConnectionRequestMarkerDropDowntChange(_this) {
	$(".markedItem .itemId").each(function() {
		var stCurent = $(this).attr("style");
		if (stCurent != undefined && stCurent != null) {
			var color = $("#ConnectionRequestMarkerDropDown option[value='" + $("#ConnectionRequestMarkerDropDown").val() + "']").attr("color");
			color = color == undefined ? "#E0E0E0" : color;
			$(this).attr("style", "padding-right: 5px;border-right: " + color + " 14px solid;");
		}
	});
	if ($(_this).val() == "") {
		$("[name='changeRequestMarker']").html("Убрать метки");
	} else {
		$("[name='changeRequestMarker']").html("Установить метки");
	}
}

function archeveIn(id) {
	$.ajax({
		url: cli.getParam("baseurl") + "Client/MarkerArchiveIn?id=" + id,
		type: 'POST',
		dataType: "json",
		success: function(data) {
			window.location.href = window.location.href;
		},
		error: function(data) {
			window.location.href = window.location.href;
		}
	});
}

function archeveOut(id) {
	$.ajax({
		url: cli.getParam("baseurl") + "Client/MarkerArchiveOut?id=" + id,
		type: 'POST',
		dataType: "json",
		success: function(data) {
			window.location.href = window.location.href;
		},
		error: function(data) {
			window.location.href = window.location.href;
		}
	});
}


function GetMarkerListAjax(inputData) {
	$("#clientReciverMessage").html("");
	if (inputData != undefined && inputData != null && inputData.length !== 0) {
		if (!(typeof inputData == "string")) {
			var html = "";
			for (var i = 0; i < inputData.length; i++) {
				html += "<option value='" + inputData[i].Id + "' color='" + inputData[i].Color + "' deleted='" + inputData[i].Deleted + "' title='" + inputData[i].Name + "'>" + inputData[i].Name + "</option>";
			}
			$(modelWindow + "[name='markerList']").html(html);
			$("#clientReciverMessage").html("Список обновлен");
		} else {
			$("#clientReciverMessage").html(data);
		}
		$(modelWindow + "[name='markerList']").change();
	} else {
		$.ajax({
			url: cli.getParam("baseurl") + "Client/MarkerList",
			type: 'POST',
			dataType: "json",
			success: function(data) {
				if (data != undefined && data.length > 0 && !(typeof data == "string")) {
					var html = "";
					for (var i = 0; i < data.length; i++) {
						html += "<option value='" + data[i].Id + "' color='" + data[i].Color + "' deleted='" + data[i].Deleted + "' title='" + data[i].Name + "'>" + data[i].Name + "</option>";
					}
					$(modelWindow + "[name='markerList']").html(html);
					$("#clientReciverMessage").html("Список обновлен");
				} else {
					$("#clientReciverMessage").html(data);
				}
				$(modelWindow + "[name='markerList']").change();
			},
			error: function(data) {
				$(modelWindow + "[name='markerList']").html("");
				$("#clientReciverMessage").html("Ответ не был получен");
			},
			statusCode: {
				404: function() {
					$(modelWindow + "[name='markerList']").html("");
					$("#clientReciverMessage").html("Ответ не был получен");
				}
			}
		});
	}

}

function addMarker(_this) {
	var name = $(modelWindow + "[name = 'name']").val();
	var color = $(modelWindow + '[name="color"]').data('colorpicker').color.toHex().replace("#", "");
	$("#clientReciverMessage").html("");
	AddMarkerAjax(name, color);
	$("#exitButton").attr("href", $("#exitButtonRef").val());
	$("#exitButton").removeAttr("data-dismiss");
}

function updateMarker(_this) {
	var id = +$("[name='markerList']").val();
	var name = $(modelWindow + "[name = 'name']").val();
	var color = $(modelWindow + '[name="color"]').data('colorpicker').color.toHex().replace("#", "");
	$("#clientReciverMessage").html("");
	UpdateMarkerAjax(id, name, color);
	$("#exitButton").attr("href", $("#exitButtonRef").val());
	$("#exitButton").removeAttr("data-dismiss");
}

function removeMarkerAjax(_this) {
	var id = +$("[name='markerList']").val();
	$("#clientReciverMessage").html("");
	RemoveMarkerAjax(id);
	$("#exitButton").attr("href", $("#exitButtonRef").val());
	$("#exitButton").removeAttr("data-dismiss");

}

function AddMarkerAjax(name, color) {
	$.ajax({
		url: cli.getParam("baseurl") + "Client/MarkerAdd?name=" + name + "&color=" + color,
		type: 'POST',
		dataType: "json",
		success: function(data) {
			GetMarkerListAjax(data);
		},
		error: function(data) {
			$(modelWindow + "[name='markerList']").html("");
			$("#clientReciverMessage").html("Ответ не был получен");
		},
		statusCode: {
			404: function() {
				$(modelWindow + "[name='markerList']").html("");
				$("#clientReciverMessage").html("Ответ не был получен");
			}
		}
	});
}

function UpdateMarkerAjax(id, name, color) {
	$.ajax({
		url: cli.getParam("baseurl") + "Client/MarkerUpdate?id=" + id + "&name=" + name + "&color=" + color,
		type: 'POST',
		dataType: "json",
		success: function(data) {
			GetMarkerListAjax(data);
		},
		error: function(data) {
			$(modelWindow + "[name='markerList']").html("");
			$("#clientReciverMessage").html("Ответ не был получен");
		},
		statusCode: {
			404: function() {
				$(modelWindow + "[name='markerList']").html("");
				$("#clientReciverMessage").html("Ответ не был получен");
			}
		}
	});
}

function RemoveMarkerAjax(id) {
	$.ajax({
		url: cli.getParam("baseurl") + "Client/MarkerRemove?id=" + id,
		type: 'POST',
		dataType: "json",
		success: function(data) {
			GetMarkerListAjax(data);
		},
		error: function(data) {
			$(modelWindow + "[name='markerList']").html("");
			$("#clientReciverMessage").html("Ответ не был получен");
		},
		statusCode: {
			404: function() {
				$(modelWindow + "[name='markerList']").html("");
				$("#clientReciverMessage").html("Ответ не был получен");
			}
		}
	});
}