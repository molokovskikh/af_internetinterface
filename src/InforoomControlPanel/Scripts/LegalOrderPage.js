
//постоянные значения
//стили
var ModalCreate = "#ModelForOrderEdit ";
var ModalEdit = "#ModelForOrderEdit ";
var NewClientEndpointPanel_style = ".newClientEndpointPanel ";
var NewClientEndpointPanel_clone = ".newClientEndpointPanelClone";
var ClientEndpointPanel_style = ".clientEndpointPanel ";

//-------------------------------------------------------| базовое |-------------------------------------------------------------------//

// Заполнение формы
function onModalOrderEditOpen(id) {
//При открытии окна редактирования заказов, в зависимости от id заполняем форму
	var newOrder = id != null && id != undefined && id != 0 ? false : true;
	var loaded = $(ModalEdit).attr("loaded"); //маркер необходимости обновлять форму
	$(ModalEdit + "[name='order.Id']").val(newOrder ? 0 : id); //обновляем значение идентификатора заказа
	//Оформление формы
	$(ModalEdit + ".modal-title strong").html(newOrder ? "Регистрация заказа" : "Редактирование заказа");
	var currentOrder = $(ModalEdit + "[name='order.Number']").val();

	if (loaded == null || loaded == undefined) {
		//выставляем флаг о факте первого использования формы
		$(ModalEdit).attr("loaded", "true");

		StartEvents(ModalEdit); //перепривязываем необходимые события

		//при первом открытии необходимо сохранить в чистом виде панели формы, для простоты очистки
		var NewClientEndpointPanel_CLONE = $(NewClientEndpointPanel_style).clone();
		NewClientEndpointPanel_CLONE.attr("class", NewClientEndpointPanel_clone);
		NewClientEndpointPanel_CLONE.addClass("hid");
		$("body").data(NewClientEndpointPanel_clone, NewClientEndpointPanel_CLONE);

		//обновление формы
		UpdateOrder(id, $(ModalEdit + "[name='client.Id']").val());
		return;
	}
	//если значение id заказа, сохраненного формой, отличается от переданного - обновляем форму
	if (id == 0 || currentOrder != id) {
		UpdateOrder(id, $(ModalEdit + "[name='client.Id']").val());
	}
}

//перепривязывание событий, исполняемых вначала 
function StartEvents(thisMain) {
	//события для коммутатора, при изменении значения
	$(thisMain).find("#SwitchDropDown").unbind("change").change(function() {
		checkLastEndpointState();
		//получить занятые порты
		GetBusyPorts();
	});
}

function updatePort(_this) {
	if ($(_this).attr("href") == "" || $(_this).attr("href") == null) {
		var portVal = $(_this).find("span").html();
		$("#endpoint_Port").val(portVal);
		$("#endpoint_PortVal").val(portVal);
		$(".portInfoData").html(portVal);
		checkLastEndpointState();
	}
}

//-------------------------------------------------------| статические ip |-------------------------------------------------------------------//

//обновление имен статических Ip
function UpdateStaticIpForModal() {
	$(".ipStaticList").each(
		function() {
			var staticIpList = $(this).find(".staticIpElement");
			for (var i = 0; i < staticIpList.length; i++) {
				$(staticIpList[i]).find("input.fixedIp.id").attr("name", "staticAddress[" + i + "].Id");
				$(staticIpList[i]).find("input.fixedIp.text").attr("name", "staticAddress[" + i + "].Ip");
				$(staticIpList[i]).find("input.fixedIp.value").attr("name", "staticAddress[" + i + "].Mask");
			}
		}
	);
	//перепривязка событий
	updateStaticIpEvents();
}

//добавление статического Ip
function updateStaticIpListForModal(list) {
	if (list != undefined && list != null) {
		var staticIpListTemp = $("#staticIpList tr");
		for (var i = 0; i < staticIpListTemp.length; i++) {
			if (!$(staticIpListTemp[i]).hasClass("hid")) {
				staticIpListTemp[i].remove();
			}
		}
		for (var i = 0; i < list.length; i++) {
			var newElement = $(".staticIpRaw.hid").clone().first();
			newElement.attr("class", "staticIpElement");
			newElement.find(".fixedIp.id").val(list[i].Id);
			newElement.find(".fixedIp.text").removeAttr("disabled").val(list[i].Ip);
			newElement.find(".fixedIp.value").removeAttr("disabled").val(list[i].Mask == 32 ? 0 : list[i].Mask);
			newElement.find(".subnet").html(list[i].Subnet);
			$(NewClientEndpointPanel_style + "#staticIpList").append(newElement);
		}
		//обновление индекса статических адресов
		UpdateStaticIpForModal();
	}
}

//обновление арендуемых Ip
function UpdateRentIp(list) {
	var html = "";
	if (list != undefined && list != null) {
		for (var i = 0; i < list.length; i++) {
			if (i > 0) {
				html += ", ";
			}
			//если ip отмечен флагом, значит он просрочен - красный
			html += "<span " + (list[i].Item2 ? "style=color:#E60000;font-weight:bold;" : "") +
				">" + list[i].Item1 + "</span>";
		}
	}
	$("#rentIpList").html(html);
}

//-------------------------------------------------------| точка подключения |-------------------------------------------------------------------//

//Проверка использования точки подключения в заказе
function CheckForEndpointUsing(val) {
	var result = val == undefined ? $(".newClientEndpointPanel").hasClass("hid") : val;
	if (result) {
		$(".newClientEndpointPanel").removeClass("hid");
		if ($(".oldClientEndpointPanel").hasClass("hid")) {
			$(".oldClientEndpointPanel").removeClass("hid");
		}
	} else {
		if (!$(".newClientEndpointPanel").hasClass("hid")) {
			$(".newClientEndpointPanel").addClass("hid");
		}
		if (!$(".oldClientEndpointPanel").hasClass("hid")) {
			$(".oldClientEndpointPanel").addClass("hid");
		}
	}
}

//Проверка использования точки подключения в заказе
function EndpointIsUsedGet() {
	var value = $(ModalEdit + "#UseNewEndpoint:checked");
	if (value.length > 0) {
		return true;
	} else {
		return false;
	}
}

//Проверка использования точки подключения в заказе
function EndpointIsUsedSet(val) {
	$(ModalEdit + "#UseNewEndpoint").prop('checked', val);
	CheckForEndpointUsing(!EndpointIsUsedGet());
}

//проверка на формирование нового подлючения в заказе
function CheckForNewEndpointUsing(obj) {
	var endpointTempVal = $(ModalEdit + "[name='OrderEndpointId']").val();
	var endpointTempValLast = $(ModalEdit + "[name='order.EndPoint.Id']").val();
	var endpointTempValExists = $(ModalEdit + "[name='order.EndPoint.Id'] option[value='" + endpointTempVal + "']").length > 0;
	endpointTempVal = endpointTempVal == endpointTempValLast || !endpointTempValExists ? endpointTempVal : 0;
	var endpointVal = parseInt($(obj).val());
	endpointVal = (String(endpointVal) == "NaN" ? endpointTempVal : endpointVal);
	if ((endpointVal == null || endpointVal == "") && endpointTempVal == 0) {
		$(obj).val("Новая точка подключения");
		CleanEndPoint();
	} else {
		UpdateEndpoint(endpointVal == undefined || endpointVal == null ? endpointTempVal : endpointVal, $(ModalEdit + "[name='order.Id']").val());
	}
}

//очистка блока точки подключения
function CleanEndPoint() {
	var clone = $("body").data(NewClientEndpointPanel_clone);
	$(ModalEdit + "[name='endpoint.Id']").val(0);
	if (clone.length > 0) {
		$(NewClientEndpointPanel_style).html(clone.html());
		StartEvents(ModalCreate);
	}
}

//обновление блока точки подключения
function updateEndpointForm(data) {
	//если данные по точке подключения существуют
	if (data != null) {
		//заполнение полей
		$(ModalEdit + "[name='endpoint.Id']").val(data.Id);

		$(NewClientEndpointPanel_style + "[name='connection.StaticIp']").val(data.Ip);
		$(NewClientEndpointPanel_style + ".fixedIp").html(data.Ip);
		if (data.Ip != undefined && data.Ip != "" && data.Ip != null) {
			$(".removeFixedIp").removeClass("hid");
			if (!$(".createFixedIp").hasClass("hid")) {
				$(".createFixedIp").addClass("hid");
			}
		} else {
			$(".createFixedIp").removeClass("hid");
			if (!$(".removeFixedIp").hasClass("hid")) {
				$(".removeFixedIp").addClass("hid");
			}
		}
		$(ModalEdit + "[name='order.EndPoint.Id']").attr("lastChangesDisable", "true");
		$(ModalEdit + "[name='order.EndPoint.Id']").attr("lastPoint", data.Id);
		$(ModalEdit + "[name='order.EndPoint.Id']").attr("lastSwitch", data.Switch);
		$(ModalEdit + "[name='order.EndPoint.Id']").attr("lastPort", data.Port);

		$(NewClientEndpointPanel_style + "[name='connection.Pool']").val(data.Pool);
		$(NewClientEndpointPanel_style + "[name='connection.Switch']").val(data.Switch);
		$(NewClientEndpointPanel_style + "[name='connection.Switch']").change();
		$(NewClientEndpointPanel_style + "[name='connection.Port']").val(data.Port);
		$(NewClientEndpointPanel_style + "[name='connection_Port']").val(data.Port);
		$(NewClientEndpointPanel_style + "[name='connection.PackageId']").val(data.PackageId);
		$(NewClientEndpointPanel_style + "[name='order.ConnectionAddress']").val(data.ConnectionAddress);

		$(ModalEdit + "[name='order.EndPoint.Id']").attr("lastChangesDisable", "false");


		//обновление списка статических ip
		updateStaticIpListForModal(data.StaticIpList);
		UpdateRentIp(data.LeaseList);
		//заполнение чекбокса
		if (data.Monitoring) {
			$(NewClientEndpointPanel_style + "[name='connection.Monitoring']").attr("checked", "checked");
		} else {
			$(NewClientEndpointPanel_style + "[name='connection.Monitoring']").removeAttr("checked");
		}
	} else {
		//очистка блока точки подключения
		CleanEndPoint();
	}
}

//запрос на обновление точки подключения
function UpdateEndpoint(id, order) {
	if (id != null && id != undefined && id != 0) {
		$.ajax({
			url: cli.getParam("baseurl") + "Client/UpdateEndpoint?id=" + id + "&order=" + order,
			type: 'POST',
			dataType: "json",
			success: function(data) {
				updateEndpointForm(data);
			},
			error: function(data) {
				CleanEndPoint();
			},
			statusCode: {
				404: function() {
					CleanEndPoint();
				}
			}
		});
	} else {
		//очистка блока точки подключения
		CleanEndPoint();
	}
}

//-------------------------------------------------------| услуги |-------------------------------------------------------------------//

//добавление услуги в заказ
function addOrderService(obj, updateList) {
	//клонирование скрытого шаблона услуги
	var newItem = $("#OrderServicesList .hid").clone();
	if (newItem == undefined || newItem == null) {
		console.log("Не удалось клонировать скрытый шаблон услуги. Добавление услуги в заказе не возможно. (Правте HTML)");
	}
	newItem.removeClass("hid");
	//заполнение шаблона
	if (obj != undefined && obj != null) {
		newItem.find(".serviceDescription input#servId").val(obj.Id);
		newItem.find(".serviceDescription input#servDesc").val(obj.Description);
		newItem.find(".serviceCost input").val(obj.Cost);
		if (obj.IsPeriodic) {
			newItem.find(".serviceIsPeriodic input").attr("checked", "checked");
		} else {
			newItem.find(".serviceIsPeriodic input").removeAttr("checked");
		}
	}
	//добавление шаблона на форму
	$("#OrderServicesList").append(newItem);
	//показываем блок с таблицей услуг, если в нем есть элементы
	if ($("#OrderServicesList tr").length > 1) {
		if ($(".addServiceBlock").hasClass("hid")) {
			$(".addServiceBlock").removeClass("hid");
		}
	} else {
		if (!$(".addServiceBlock").hasClass("hid")) {
			$(".addServiceBlock").addClass("hid");
		}
	}
	//обновляем список заказов, если есть необходимость
	if (updateList != undefined && updateList === true) {
		UpdateOrderServicesList(null);
	} else {
		$("#OrderServicesList tr").each(function() {
			if (!$(this).hasClass("hid")) {
				$(this).find(".serviceDescription input#servId").removeAttr("disabled");
				$(this).find(".serviceDescription input#servDesc").removeAttr("disabled");
				$(this).find(".serviceCost input").removeAttr("disabled");
			}
		});
	}
}

//удаление услуги из заказа 
function removeOrderService(obj) {
	//удаление элемента
	$(obj).parents("tr").remove();
	//показываем блок с таблицей услуг, если в нем есть элементы
	if ($("#OrderServicesList tr").length <= 1) {
		if (!$(".addServiceBlock").hasClass("hid")) {
			$(".addServiceBlock").addClass("hid");
		}
	}
	//обновляем список заказов
	UpdateOrderServicesList(null);
}

//очистка списка услуг в заказе
function CleanOrderServicesList() {
	var servListTemp = $("#OrderServicesList tr");
	for (var i = 0; i < servListTemp.length; i++) {
		if (!$(servListTemp[i]).hasClass("hid")) {
			servListTemp[i].remove();
		}
	}
}

//Получение адреса заказа, для редактирования
function GetOrderConnectionAddress(id) {
	var addressEditorId = $("#ModelForUpdateConnectionAddress [name='orderId']");
	var addressEditorAddress = $("#ModelForUpdateConnectionAddress [name='newAddress']");
	addressEditorId.val("0");
	addressEditorAddress.val("");
	if (id != null && id != undefined && id != 0) {
		$.ajax({
			url: cli.getParam("baseurl") + "Client/GetConnectionAddress?id=" + id,
			type: 'POST',
			dataType: "json",
			success: function(data) {
				addressEditorId.val(id);
				addressEditorAddress.val(data);
			}
		});
	} else {
		//очистка значений
		addressEditorId.val("0");
		addressEditorAddress.val("");
	}
}

//обновление списка услуг в заказе
function UpdateOrderServicesList(data) {
	var index = 0;
	//если даннных по заказу нет
	if (data == null || data == undefined) {
		//обновляем индекс
		$("#OrderServicesList tr").each(function() {
			if (!$(this).hasClass("hid")) {
				$(this).find(".serviceDescription input#servId").attr("name", "order.OrderServices[" + index + "].Id").removeAttr("disabled");
				$(this).find(".serviceDescription input#servDesc").attr("name", "order.OrderServices[" + index + "].Description").attr("clone", index).removeAttr("disabled");
				$(this).find(".serviceCost input").attr("name", "order.OrderServices[" + index + "].Cost").attr("clone", index).removeAttr("disabled");
				$(this).find(".serviceIsPeriodic input").attr("name", "order.OrderServices[" + index + "].IsPeriodic");
				index++;
			}
		});
	}
	//если даннные по заказу есть
	else {
		//чистим список с услугами (сохраняя только шаблон)
		var servListTemp = $("#OrderServicesList tr");
		for (var i = 0; i < servListTemp.length; i++) {
			if (!$(servListTemp[i]).hasClass("hid")) {
				servListTemp[i].remove();
			}
		}
		//добавляем в список элементы
		for (var i = 0; i < data.length; i++) {
			addOrderService(data[i]);
		}
		//обновляем индексы элементов списка услуг
		$("#OrderServicesList tr").each(function() {
			if (!$(this).hasClass("hid")) {
				$(this).find(".serviceDescription input#servId").attr("name", "order.OrderServices[" + index + "].Id");
				$(this).find(".serviceDescription input#servDesc").attr("name", "order.OrderServices[" + index + "].Description");
				$(this).find(".serviceCost input").attr("name", "order.OrderServices[" + index + "].Cost");
				$(this).find(".serviceIsPeriodic input").attr("name", "order.OrderServices[" + index + "].IsPeriodic");
				index++;
			}
		});
	}
}

//-------------------------------------------------------| заказ |-------------------------------------------------------------------//

//обновление формы заказа
function updateOrderForm(data) {
	//если данные для заполнения есть
	if (data != null) {
		//заполняем поля формы
		$(ModalEdit + "[name='order.Number']").val(data.Number);
		$(ModalEdit + "[name='order.BeginDate']").val(data.BeginDate);
		$(ModalEdit + "[name='order.EndDate']").val(data.EndDate);
		$(ModalEdit + "[name='endpoint.Id']").val(data.EndPoint);
		$(ModalEdit + "[name='OrderEndpointId']").val(data.EndPoint);

		//если точка подключения отсутствует, вызов события клика мыши, скрывающее форму и изменяющее вид чекбокса
		if (data.EndPoint != 0) {
			$(ModalEdit + ".createFixedIp").attr("onclick", "createFixedIp(" + data.EndPoint + ")");
			// показываем новую точку подключения 
			EndpointIsUsedSet(false);
		} else {
			if (data.OrderServices.length > 0) {
				// скрываем новую точку подключения 
				EndpointIsUsedSet(true);
			} else {
				// показываем новую точку подключения 
				EndpointIsUsedSet(false);
			}
		}
		//обновлени списка услуг заказа
		UpdateOrderServicesList(data.OrderServices);
		//обновляем точку подключения, если она задана
		$(ClientEndpointPanel_style + "[name='order.EndPoint.Id']").html("");
		if (data.ClientEndpoints != null && data.ClientEndpoints != undefined) {
			var html = "<option>Новая точка подключения</option>";
			for (var i = 0; i < data.ClientEndpoints.length; i++) {
				html += "<option value='" + data.ClientEndpoints[i] + "'>" + data.ClientEndpoints[i] + "</option>";
			}
			$(ClientEndpointPanel_style + "[name='order.EndPoint.Id']").html(html);
			if ($(ClientEndpointPanel_style + "[name='order.EndPoint.Id'] option[value='" + data.EndPoint + "']").length === 0) {
				$(ClientEndpointPanel_style + "[name='order.EndPoint.Id']").val("Новая точка подключения");
			} else {
				$(ClientEndpointPanel_style + "[name='order.EndPoint.Id']").val(data.EndPoint);
			}

			$(ClientEndpointPanel_style + "[name='order.EndPoint.Id']").change();
		}
	}
	//если данных нет
	else {
		//чистим форму
		$(ModalEdit + "[name='order.Number']").val("");
		$(ModalEdit + "[name='order.BeginDate']").val("");
		$(ModalEdit + "[name='order.EndDate']").val("");
		// скрываем  новую точку подключения 
		EndpointIsUsedSet(true);
		//очистка списка услуг заказа
		CleanOrderServicesList();
		//очистка точки подключения
		CleanEndPoint();
	}
}

//Запрос на обновление заказа
function UpdateOrder(id, clientId) {
	if ((id != null && id != undefined && id != 0) || (id == 0 && clientId != 0)) {
		$.ajax({
			url: cli.getParam("baseurl") + "Client/UpdateOrder?id=" + id + "&clientId=" + clientId,
			type: 'POST',
			dataType: "json",
			success: function(data) {
				updateOrderForm(data);
			},
			error: function(data) {
				updateOrderForm(null);
			},
			statusCode: {
				404: function() {
					updateOrderForm(null);
				}
			}
		});
	} else {
		//обновление формы заказа
		updateOrderForm(null);
	}
}

//-------------------------------------------------------| вспомогательное |-------------------------------------------------------------------//
function checkLastEndpointState() {
	if ($(ModalEdit + "[name='order.EndPoint.Id']").attr("lastChangesDisable") == "true") {
		return;
	}
	var lastPointVal = $(ModalEdit + "[name='order.EndPoint.Id']").attr("lastPoint");
	var lastSwitchVal = $(ModalEdit + "[name='order.EndPoint.Id']").attr("lastSwitch");
	var lastPortVal = $(ModalEdit + "[name='order.EndPoint.Id']").attr("lastPort");

	var currentPointVal = $(ModalEdit + "[name='order.EndPoint.Id']").val();
	var currentSwitchVal = $(ModalEdit + "[name='connection.Switch']").val();
	var currentPortVal = $(ModalEdit + "[name='connection.Port']").val();

	if ((lastSwitchVal != currentSwitchVal || lastPortVal != currentPortVal) && (((lastPointVal == undefined || currentPointVal == undefined) && lastPointVal === currentPointVal) || lastPointVal == currentPointVal)) {
		$(ModalEdit + "[name='order.EndPoint.Id']").val("Новая точка подключения");
	}
}

//обновление формы закрытия заказа 
function updateModelForOrderClose(id, date) {
	$("#ModelForOrderRemove [name='orderId']").val(id);
	$("#ModelForOrderRemove [name='orderCloseDate']").val(date);
}


//действия при загрузке страницей с формой заказов
$(function() {
	//открытие/закрытие информации о заказе по клику на заголовок заказа
	$(".orderTitle").click(function() {
		if ($(this).hasClass("entypo-right-open-mini")) {
			$(this).removeClass("entypo-right-open-mini");
			$(this).addClass("entypo-down-open-mini");
		} else {
			$(this).removeClass("entypo-down-open-mini");
			$(this).addClass("entypo-right-open-mini");
		}
		var obody = $(this).parents(".orderColumn").find(".orderBody");
		if (obody.length > 0) {
			if (obody.hasClass("hid")) {
				obody.removeClass("hid");
			} else {
				obody.addClass("hid");
			}
		}
	});
	//валидация значения на форме сервисов
	$("#ModelForActivateService [name='endDate']").change(function() {
		if ($(this).val() != "") {
			$("#ModelForActivateService .error.red").remove();
			if (Date.parse($(this).val()) <= Date.parse($(this).attr('now'))) {
				$('<span class="error red">Дата не может дыть меньше завтрашнего дня!</span>').insertAfter(this);
				$(this).val("");
				return;
			}
		}
	});
});