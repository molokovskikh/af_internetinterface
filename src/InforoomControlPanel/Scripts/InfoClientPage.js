﻿//// | СКРИПТ ДЛЯ СТРАНИЦЫ РЕДАКТИРОВАНИЯ КЛИЕНТА | ===========>>

function ClientPage() {
	current = this;
	//отобразить сообщение (успех)
	this.ShowMessageSuccess = function(message) {
		$('.server-message').html("");
		$('.server-message').prepend('<div class="col-md-12 alert alert-success">' + message + '</div>');
		scrollTo(".server-message", 100);

	}
	//отобразить сообщение (ошибка)
	this.ShowMessageError = function(message) {
		$('.server-message').html("");
		$('.server-message').prepend('<div class="col-md-12 alert alert-danger">' + message + '</div>');
		scrollTo(".server-message", 100);
	}
	/////////////////////////////////////////////////| Названия стилей-маркеров
	//основной блок
	var mainBlock = "blockJsLockControll";
	//скрытый дочерний блок с данными представления
	var defaultBlock = "defaultBlock";
	//скрытый дочерний блок с данными редактирования
	var editBlock = "editBlock";
	//пустой блок
	var emptyBlock = "emptyBlock";
	//стиль css ошибки
	var errorClass = "error";
	//стиль активации блока
	var activeLock = "activeLock";
	//кнопка активации редактирования блока
	var lockButton = "lockButton";
	//кнопка отмены редактирования блока
	var unlockButton = "unlockButton";

	this.addCalendar = function() {
		var calendar = $('.activeLock [data-provide="datepicker-inline"]').datepicker({
			startView: 2,
			clearBtn: true,
			language: "ru",
			todayHighlight: true
		});
	}

	this.StaticEventsUpdate = function(thisMain) {
		$(thisMain).find("#SwitchDropDown").unbind("change").change(function() {
			GetBusyPorts();
		});

	}


	this.LockBlock = function(thisMain) {
		var activeBlock = $("." + mainBlock + "." + activeLock);
		if (activeBlock.length == 0) {

			$(thisMain).find("." + emptyBlock).html("");
			cloneItem = $(thisMain).find("." + editBlock).children().clone();
			$(thisMain).find("." + emptyBlock).append(cloneItem);
			$(thisMain).removeClass(activeLock).addClass(activeLock);
			current.addCalendar();

			clientPage.UpdateEvents(thisMain);
			$(thisMain).addClass(activeLock);
			changeVisibilityUpdateEach();
		} else {
			var title = $(activeBlock).attr("title");
			current.ShowMessageError("Необходимо завершить редактирование блока " + (title != null && title != undefined ? "'" + title + "'" : ""));
		}
	}
	this.UnlockBlock = function(thisMain) {
		var errorItems = $("." + mainBlock + " ." + errorClass);
		if (errorItems.length > 0) {
			window.location.href = window.location.href;
			return;
		}
		$(thisMain).find("." + emptyBlock).html("");
		cloneItem = $(thisMain).find("." + defaultBlock).children().clone(true);
		$(thisMain).removeClass(activeLock);
		$(thisMain).find("." + emptyBlock).append(cloneItem);
		clientPage.UpdateEvents(thisMain);
		changeVisibilityUpdateEach();
	}

	//обновить пустой блок
	this.UpdateEvents = function(thisMain) {
		$(thisMain).find("." + lockButton).unbind("click");
		$(thisMain).find("." + unlockButton).unbind("click");

		$(thisMain).find("." + lockButton).click(function() {
			thisMainItem = $(this).parents("." + mainBlock);
			clientPage.LockBlock(thisMainItem);
		});
		$(thisMain).find("." + unlockButton).click(function() {
			console.log(thisMain);
			thisMainItem = $(this).parents("." + mainBlock);
			clientPage.UnlockBlock(thisMainItem);
		});
		current.StaticEventsUpdate(thisMain);
		updateStaticIpEvents();
	}
	//обновить пустой блок
	this.UpdateBlock = function(thisMain) {
		var blockEmpty = $(thisMain).find("." + emptyBlock);
		var blocksToCheck = $(thisMain).find("." + editBlock);
		var errorClassElements = $(blocksToCheck).find("." + errorClass);
		if (errorClassElements.length > 0) {
			$(thisMain).find("." + emptyBlock).html("");
			cloneItem = $(thisMain).find("." + editBlock).children().clone();
			$(thisMain).find("." + emptyBlock).append(cloneItem);
			$(thisMain).removeClass(activeLock).addClass(activeLock);
			current.addCalendar();

		} else {
			$(thisMain).find("." + emptyBlock).html("");
			cloneItem = $(thisMain).find("." + defaultBlock).children().clone(true);
			$(thisMain).find("." + emptyBlock).append(cloneItem);
		}
		current.UpdateEvents(thisMain);
	}

	//заполнить пустые блоки (после загрузки страницы)
	this.ShowBlocks = function() {
		$("." + mainBlock).each(function() {
			current.UpdateBlock(this);
		});
	}
}


function changeVisibilityFromCookies(blockName) {
	try {
		var panelBodyToHide = $("#" + blockName).find(".panel-body");
		if (panelBodyToHide.length > 0) {
			if ($(panelBodyToHide).find(".error").length > 0) {
				$(panelBodyToHide).removeClass("hid");
			} else {
				var cookieValue = $.cookie(blockName);
				if (cookieValue != null && cookieValue == "true") {
					if (!$(panelBodyToHide).hasClass("hid")) {
						$(panelBodyToHide).addClass("hid");
					}
				} else {
					if ($(panelBodyToHide).hasClass("hid")) {
						$(panelBodyToHide).removeClass("hid");
					}
				}
			}
		}
	} catch (e) {

	}
}

function changeVisibility(blockName) {
	try {
		var panelBodyToHide = $("#" + blockName).find(".panel-body");
		if (panelBodyToHide.length > 0) {
			if ($(panelBodyToHide).find(".error").length > 0) {
				$(panelBodyToHide).removeClass("hid");
			} else {
				if ($(panelBodyToHide).hasClass("hid")) {
					$(panelBodyToHide).removeClass("hid");
					$.cookie(blockName, "false", { expires: 365, path: "/" });
				} else {
					$(panelBodyToHide).addClass("hid");
					$.cookie(blockName, "true", { expires: 365, path: "/" });
				}
			}

		}
	} catch (e) {

	}
}

function changeVisibilityUpdateEach() {
	$(".emptyBlock").each(function() {
		changeVisibilityFromCookies($(this).attr("id"))
	});
}


function clientContactDelete(button) {
	var trList = $("#emptyBlock_contacts table tr");
	for (var i = 0; i < trList.length; i++) {
		var delItem = $(trList[i]).find("#" + $(button).attr("id"));
		if (delItem.length > 0) {
			$(trList[i]).find('[name*="ContactFormatString"]').val("");
			$(trList[i]).addClass("hid");
			break;
		}
	}
}

function clientContactCopy() {

	var newRow = $("#emptyBlock_contacts table tr:last");
	if (newRow.length == 0) {
		newRow = $("#emptyBlock_contacts table tr.hid");
	}
	if (newRow.length > 0) {
		var lastRow = $(newRow).clone();
		$(lastRow).find("input, select").val("");
		$(lastRow).find('[name*=".Id"]').remove();
		var indexNum = 0;
		for (var i = 0; i < $("#emptyBlock_contacts table tr").length; i++) {
			var str = '[name *="client.Contacts[' + i + ']"]';
			if ($(lastRow).find(str).length > 0) {
				indexNum = i;
			}
		}
		indexNum++;
		$(lastRow).find('.btn.btn-red').attr("id", "contactDel" + indexNum);
		$(lastRow).find('[name*="ContactFormatString"]').attr("name", "client.Contacts[" + indexNum + "].ContactFormatString").removeAttr("id");
		$(lastRow).find('[name*="Type"]').attr("name", "client.Contacts[" + indexNum + "].Type").removeAttr("id");
		$(lastRow).find('[name*="ContactName"]').attr("name", "client.Contacts[" + indexNum + "].ContactName").removeAttr("id");

		$(lastRow).removeClass("hid");
		$("#emptyBlock_contacts table tbody").append(lastRow);
	}
}


function showWriteOff(year, month) {
	var sDate = ".writeOffTable tr.wr-group[year='" + year + "'][month='" + month + "']";
	if (month == 0) {
		sDate = ".writeOffTable tr.wr-group[year='" + year + "']";
	}
	var foundElements = $(sDate);

	var show = false;
	if (foundElements.length > 0 && foundElements.hasClass("hid")) {
		show = true;
	}

	$(".writeOffTable tr.wr-group").addClass("hid");

	if (show) {
		foundElements.removeClass("hid");
	} else {
		foundElements.addClass("hid");
	}
}

function showWriteOffByYear(year) {
	showWriteOff(year, 0);
}

function showWriteOffByMonth(year, month) {
	showWriteOff(year, month);
}

function writeoffDeleteShow(id) {
	writeoffDelete(id, "false");
}

function userwriteoffDeleteShow(id) {
	writeoffDelete(id, "true");
}

function writeoffDelete(id, user) {
	var tr = $(".writeOffTable tr[writeOff='" + id + "']");
	var date = tr.find(".wdate").html();
	var sum = tr.find(".wsum").html();
	$("#writeoffToDelete").val(id);
	$("#writeoffType").val(user);

	$("#writeOffDeleteMessage").html("Вы действительно хотите удалить списание от <br/> <strong>" + date + "</strong> на сумму: <strong>" + sum + "</strong> руб. ?");
}

function paymentCancel(id) {
	$("#paymentsCancelId").val(id);
	$("#paymentDeleteMessage").html("Почему Вам необходимо отменить платеж <strong>№" + id + "</strong> ?");
}

function paymentMove(id) {
	$("#paymentMoveId").val(id);
	$("#paymentMoveMessage").html("Укажите кому и зачем Вам необходимо перевести платеж <strong>№" + id + "</strong> ?");
}

function getPaymentReciver() {
	if ($("#clientReciverId").length > 0) {
		if ($("#clientReciverId").val() != null && $("#clientReciverId").val() != "") {

			$.ajax({
				url: cli.getParam("baseurl") + "Client/getClientName?id=" + $("#clientReciverId").val(),
				type: 'POST',
				dataType: "json",
				success: function(data) {
					$("#clientReciverMessage").html(data);
				},
				error: function(data) {
					$("#clientReciverMessage").html("Ответ не был получен");
				},
				statusCode: {
					404: function() {
						$("#clientReciverMessage").html("Ответ не был получен");
					}
				}
			});

		} else {
			$("#clientReciverMessage").html("");
		}
	}
}

function updatePort(_this) {
	if ($(_this).attr("href") == "" || $(_this).attr("href") == null) {
		var portVal = $(_this).find("span").html();
		$("#endpoint_Port").val(portVal);
		$("#endpoint_PortVal").val(portVal);
	}
}

function createFixedIp(endpointId) {
	var errorText = "Ошибка при присвоении фиксированного IP !";
	$.ajax({
		url: cli.getParam("baseurl") + "Client/GetStaticIp?id=" + endpointId,
		type: 'POST',
		dataType: "json",
		success: function(data) {
			if (data == "" || data == null) {
				$(".fixedIp").html(errorText);
				return;
			}
			$(".fixedIp").html(data);
			$("#fixedIp").val(data);
			$(".removeFixedIp").removeClass("hid");
			$(".createFixedIp").addClass("hid");
		},
		error: function(data) {
			$(".fixedIp").html(errorText);
			$("#fixedIp").val("");
			$(".removeFixedIp").addClass("hid");
			$(".createFixedIp").removeClass("hid");
		},
		statusCode: {
			404: function() {
				$(".fixedIp").html(errorText);
				$("#fixedIp").val("");
				$(".removeFixedIp").addClass("hid");
				$(".createFixedIp").removeClass("hid");
			}
		}
	});
}

function removeFixedIp() {
	$(".fixedIp").html("");
	$("#fixedIp").val("");
	$(".removeFixedIp").addClass("hid");
	$(".createFixedIp").removeClass("hid");
}

function updatePortsState(data) {
	var linkA = $("[name='clientUrlExampleA']");
	var linkB = $("[name='clientUrlExampleB']");
	if (linkA.length > 0 && linkB.length > 0) {
		var portTags = $("#switchPorts .port");
		portTags.each(function() {
			var portTagItem = this;
			$(portTagItem).removeAttr("href");
			$(portTagItem).attr("title", "свободный порт");
			if (data == null) {
				$(portTagItem).addClass("free");
			} else {
				used = true;
				for (var i = 0; i < data.length; i++) {
					if ($(portTagItem).html().indexOf("<span>" + data[i].endpoint + "</span>") != -1) {
						//Физ.лицо, Юр лицо
						$(portTagItem).attr("href", (data[i].type == 0 ? linkA.val() : linkB.val()) + "/" + data[i].client);
						$(portTagItem).addClass("client");
						$(portTagItem).attr("title", "занятый порт");
						used = false;
					}
				}
				if (used) {
					$(portTagItem).addClass("free");
				}
			}
		});
	}
}

function GetBusyPorts() {
	if ($("#SwitchDropDown").length > 0) {
		$("#switchPorts .port").removeClass("free").removeClass("client");
		if ($("#SwitchDropDown").val() != null && $("#SwitchDropDown").val() != "") {
			$.ajax({
				url: cli.getParam("baseurl") + "Client/getBusyPorts?id=" + $("#SwitchDropDown").val(),
				type: 'POST',
				dataType: "json",
				success: function(data) {
					updatePortsState(data);
				},
				error: function(data) {
					updatePortsState(null);
				},
				statusCode: {
					404: function() {
						updatePortsState(null);
					}
				}
			});

		} else {
			updatePortsState(null);
		}
	}
}

var timeoutIteration = 0;

//Функция, которая пингует эндпоинт и отображает ответ
function updateEndpointStatus(id, htmlElement, timeout) {
	if ($(htmlElement).hasClass("ajaxRun") == false) {
		$(htmlElement).addClass("ajaxRun");

		if (!timeout)
			timeout = 5000;
		try {
			$.ajax({
				url: cli.getParam("baseurl") + "Client/PingEndpoint?Id=" + id,
				type: 'POST',
				dataType: "json",
				success: function(data) {
					$(htmlElement).removeClass("ajaxRun");
					$(htmlElement).html(data);
					setTimeout(updateEndpointStatus.bind(this, id, htmlElement, timeout), timeout);
				},
				error: function(data) {
					$(htmlElement).removeClass("ajaxRun");
					$(htmlElement).html("<b class='undefined'>Не запустить проверку коммутатора. Пробую снова.</b>");
					setTimeout(updateEndpointStatus.bind(this, id, htmlElement, timeout), timeout);
				}
			});
		} catch (e) {
			console.log(e);
			setTimeout(updateEndpointStatus.bind(this, id, htmlElement, timeout), timeout);
		}
	} else {
		setTimeout(updateEndpointStatus.bind(this, id, htmlElement, timeout), timeout);
	}
}


//добавление статического Ip
function deleteStaticIp(element) {
	$(element).parents(".staticIpElement").remove();
	$(".staticAddressError").remove();
	UpdateStaticIp();
}

//удаление статического Ip
function addStaticIp(element) {
	var cleanSubnet = $(element).parents(".ipStaticList").find(".staticIpElement");
	var pattern = new RegExp("^([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})$");
	$(".staticAddressError").remove();
	for (var i = 0; i < cleanSubnet.length; i++) {
		if ($(cleanSubnet[i]).find(".subnet").html() == "" || pattern.test($(cleanSubnet[i]).find(".fixedIp.text").val()) == false) {
			$(element).parents(".ipStaticList").append('<div class="error staticAddressError">Статический адрес введен неверно!<div class="msg"></div><div class="icon"></div></div>');
			return;
		}
	}

	var newElement = $(".staticIpRaw.hid").clone().first();
	newElement.attr("class", "staticIpElement");
	newElement.find(".fixedIp.text").removeAttr("disabled");
	newElement.find(".fixedIp.value").removeAttr("disabled");
	$("#ipStaticTable tbody").append(newElement);
	UpdateStaticIp();
}

//обновление имен статических Ip
function UpdateStaticIp() {
	$(".ipStaticList").each(
		function() {
			var staticIpList = $(this).find(".staticIpElement");
			for (var i = 0; i < staticIpList.length; i++) {
				$(staticIpList[i]).find("input.fixedIp.text").attr("name", "staticAddress[" + i + "].Ip");
				$(staticIpList[i]).find("input.fixedIp.value").attr("name", "staticAddress[" + i + "].Mask");
			}
		}
	);
	updateStaticIpEvents();
}

function updateStaticIpEvents() {
	setValid("input.fixedIp.text");
	setValidMask("input.fixedIp.value");
}

function setValid(elem) {
	$(elem).unbind("change");
	var pattern = new RegExp("^([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})$");
	$(elem).change(function() {
		$(elem).parent().find(".error").remove();
		if (!pattern.test($(this).val())) {
			$(this).parent().append('<div class="error">IP адрес указан в неверном формате<div class="msg"></div><div class="icon"></div></div>');
		}
	});
}

function setValidMask(elem) {
	$(elem).unbind("change");
	$(elem).change(function() {
		$(this).parent().find(".error").remove();
		if ($(this).val() > 30) {
			$(this).parent().append('<div class="error">Маска не может быть больше 30<div class="msg"></div><div class="icon"></div></div>');
			return;
		}
		if ($(this).val() < 8) {
			$(this).parent().append('<div class="error">Маска не может быть меньше 8<div class="msg"></div><div class="icon"></div></div>');
			return;
		}
		updateSubnateVal($(this).parents(".staticIpElement").find(".subnet"), $(this).val());
	});
}

//обновление ~"подсети" по маске
function updateSubnateVal(htmlElement, mask) {
	if ($(htmlElement).hasClass("ajaxRun") == false) {
		$(htmlElement).addClass("ajaxRun");
		$.ajax({
			url: cli.getParam("baseurl") + "Client/GetSubnet?mask=" + mask,
			type: 'POST',
			dataType: "json",
			success: function(data) {
				$(htmlElement).removeClass("ajaxRun");
				$(htmlElement).html(data);
			},
			error: function(data) {
				$(htmlElement).removeClass("ajaxRun");
				$(htmlElement).html("");
			}
		});
	}
}

function endpointDelete(id) {
	$("#endpointToRemoveId").val(id);
	$("#endpointDeleteMessage").html("Вы действительно хотите удалить соединение <strong>№" + id + "</strong> ?");
}

var clientPage = new ClientPage();
//вызов функций при загрузке, дополнительные действия
$(function() {
	clientPage.ShowBlocks();
	changeVisibilityUpdateEach();
	$("div.Client.InfoPhysical").removeClass("hid");

	$("#clientReciverId").change(function() {
		getPaymentReciver();
	});

	updateStaticIpEvents();

	$("#switchPorts .port.free").attr("title", "свободный порт");
	$("#switchPorts .port.client").attr("title", "занятый порт");

	//скроллинг к нужной таблице 
	var scrollToElement = $("#currentSubViewName");
	if (scrollToElement.length > 0) {
		valNameScroll = scrollToElement.val();
		valNameScroll = "[id ~='emptyBlock" + valNameScroll.toLowerCase() + "']";
		var objToScroll = $(valNameScroll);
		if (objToScroll.length > 0)
			scrollTo(valNameScroll, 1);
	}
	$(".switchstatus").each(function() {
		var id = $("[name='endpointId']").val();
		//	$(this).insertAfter("<div class='switchStatusUpdater'></div>");
		updateEndpointStatus(id, ".switchstatus");
	});
});