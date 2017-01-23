//// | СКРИПТ ДЛЯ СТРАНИЦЫ РЕДАКТИРОВАНИЯ КЛИЕНТА | ===========>>

function ClientPage() {
    current = this;
    //отобразить сообщение (успех)
    this.ShowMessageSuccess = function(message) {
        $(".server-message").html("");
        $(".server-message").prepend('<div class="col-md-12 alert alert-success">' + message + "</div>");
        scrollTo(".server-message", 100);

    };
    //отобразить сообщение (ошибка)
    this.ShowMessageError = function(message) {
        $(".server-message").html("");
        $(".server-message").prepend('<div class="col-md-12 alert alert-danger errorFixed">' + message + '<button type="button" class="close" data-dismiss="alert" aria-label="Close"><span aria-hidden="true">&times;</span></button></div>');
        //	scrollTo(".server-message", 100);
        //$('.alert-danger.errorFixed').click(function() {
        //	$(this).remove();
        //});
    };
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
    };
    this.StaticEventsUpdate = function(thisMain) {
        $(thisMain).find("#SwitchDropDown").unbind("change").change(function() {
            GetBusyPorts();
        });
    };
    this.LockBlock = function(thisMain) {
        var activeBlock = $("." + mainBlock + "." + activeLock);
        if (activeBlock.length == 0) {

            $(thisMain).find("." + emptyBlock).html("");
            cloneItem = $(thisMain).find("." + editBlock).children().clone();
            $(thisMain).find("." + emptyBlock).append(cloneItem);
            current.TempNamesRemove(thisMain);
            $(thisMain).removeClass(activeLock).addClass(activeLock);
            current.addCalendar();

            clientPage.UpdateEvents(thisMain);
            $(thisMain).addClass(activeLock);
            changeVisibilityUpdateEach();
        } else {
            var title = $(activeBlock).attr("title");
            current.ShowMessageError("Необходимо завершить редактирование блока " + (title != null && title != undefined ? "'" + title + "'" : ""));
        }
    };
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
        current.TempNamesRemove(thisMain);
        clientPage.UpdateEvents(thisMain);
        changeVisibilityUpdateEach();
    };

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
    };
    //обновить пустой блок
    this.UpdateBlock = function(thisMain) {
        var blockEmpty = $(thisMain).find("." + emptyBlock);
        var blocksToCheck = $(thisMain).find("." + editBlock);
        var errorClassElements = $(blocksToCheck).find("." + errorClass);
        if (errorClassElements.length > 0) {
            $(thisMain).find("." + emptyBlock).html("");
            cloneItem = $(thisMain).find("." + editBlock).children().clone();
            $(thisMain).find("." + emptyBlock).append(cloneItem);
            current.TempNamesRemove(thisMain);

            $(thisMain).removeClass(activeLock).addClass(activeLock);
            current.addCalendar();

        } else {
            $(thisMain).find("." + emptyBlock).html("");
            cloneItem = $(thisMain).find("." + defaultBlock).children().clone(true);
            $(thisMain).find("." + emptyBlock).append(cloneItem);
            current.TempNamesRemove(thisMain);
        }
        current.UpdateEvents(thisMain);
    };

    //заполнить пустые блоки (после загрузки страницы)
    this.ShowBlocks = function() {
        current.TempNamesAdd(current.thisMain);
        $("." + mainBlock).each(function() {
            current.UpdateBlock(this);
        });
    };
    this.TempNamesAdd = function() {
        $("." + editBlock + " [name], ." + defaultBlock + " [name]").each(function() {
            $(this).attr("tempName", $(this).attr("name"));
            $(this).removeAttr("name");
        });
    };
    this.TempNamesRemove = function(thisMain) {
        $(thisMain).find("." + emptyBlock + " [tempName]").each(function() {
            $(this).attr("name", $(this).attr("tempName"));
            $(this).removeAttr("tempName");
        });
    };
}


function changeVisibilityFromCookies(blockName) {
    changeVisibility(blockName, true);
}

function changeVisibility(blockName, fromCookies) {
    try {
        var buttonIconStyleA = "entypo-right-open";
        var buttonIconStyleB = "entypo-down-open";
        var abutton = $("#" + blockName).find(".panel-title.bold a.c-pointer");
        abutton.removeClass(buttonIconStyleA).removeClass(buttonIconStyleB);

        var panelBodyToHide = $("#" + blockName).find(".panel-body");
        if (panelBodyToHide.length > 0) {
            if ($(panelBodyToHide).find(".error").length > 0) {
                abutton.addClass(buttonIconStyleB);
                abutton.addClass("sdsd1");
                $(panelBodyToHide).removeClass("hid");
            } else {
                abutton.addClass("sdsd2");
                if (fromCookies == undefined) {
                    //записываем кукисы
                    if ($(panelBodyToHide).hasClass("hid")) {
                        abutton.addClass(buttonIconStyleB);
                        $(panelBodyToHide).removeClass("hid");
                        $.cookie(blockName, "false", { expires: 365, path: "/" });
                    } else {
                        abutton.addClass(buttonIconStyleA);
                        $(panelBodyToHide).addClass("hid");
                        $.cookie(blockName, "true", { expires: 365, path: "/" });
                    }
                } else {
                    //заполняем из кукисов
                    var cookieValue = $.cookie(blockName);
                    if (cookieValue != null && cookieValue == "true") {
                        abutton.addClass(buttonIconStyleA);
                        if (!$(panelBodyToHide).hasClass("hid")) {
                            $(panelBodyToHide).addClass("hid");
                        }
                    } else {
                        abutton.addClass(buttonIconStyleB);
                        if ($(panelBodyToHide).hasClass("hid")) {
                            $(panelBodyToHide).removeClass("hid");
                        }
                    }
                }
            }
        }
    } catch (e) {

    }
}

function changeVisibilityUpdateEach() {
    $(".emptyBlock").each(function() {
        changeVisibilityFromCookies($(this).attr("id"));
    });
}


function clientContactValidation() {
    var contactsList = $("#emptyBlock_contacts").find('[name*=".ContactFormatString"].enabled');
    var contactsListType = $("#emptyBlock_contacts").find('[name*=".Type"].enabled');
    var contactsListAjax = new Array();
    var contactsListTypeAjax = new Array();
    if (contactsListType.length !== contactsList.length) {
        console.log("Ошибка в формировании списка контактов для валидирования.");
        return;
    }
    for (var i = 0; i < contactsList.length; i++) {
        contactsListAjax.push(String($(contactsList[i]).val()));
        contactsListTypeAjax.push(parseInt($(contactsListType[i]).val()));
    }
    $("#ContactValidationMessage").html("");
    $("#ContactValidationMessage").removeClass("error");
    if (contactsListAjax.length > 0) {
        $.ajax({
            url: cli.getParam("baseurl") + "Client/GetContactValidation",
            type: "POST",
            dataType: "json",
            data: JSON.stringify({ "contacts": contactsListAjax, "types": contactsListTypeAjax }),
            contentType: "application/json; charset=utf-8",
            success: function(data) {
                if (data != "") {
                    $("#ContactValidationMessage").addClass("error");
                    $("#ContactValidationMessage").html(data);
                } else {
                    $("#ClientContactsEditorForm [name*='.ContactName']").each(function() {
                        if ($(this).val() === "" && $("#editBlock_contacts [name = '" + $(this).attr("name") + "']").val() === "") {
                            $(this).attr("noName", $(this).attr("name"));
                            $(this).removeAttr("name");
                        }
                    });
                    $("#ClientContactsEditorForm [noName*='.ContactName']").each(function() {
                        if ($(this).val() !== "") {
                            $(this).attr("name", $(this).attr("noName"));
                            $(this).removeAttr("noName");
                        }
                    });
                    $("#ClientContactsEditorForm").submit();
                }
            },
            error: function(data) {
                $("#clientReciverMessage").html("Ответ не был получен. Контакты не проверены.");
            },
            statusCode: {
                404: function() {
                    $("#clientReciverMessage").html("Ответ не был получен. Контакты не проверены.");
                }
            }
        });
    } else {
        $("#clientReciverMessage").html("");
    }
}

function clientContactDelete(button) {
    var trList = $("#emptyBlock_contacts table tr");
    if ($("#editBlock_contacts #contactsTable .contactItem").length == 1) return;
    for (var i = 0; i < trList.length; i++) {
        var delItem = $(trList[i]).find("#" + $(button).attr("id"));
        if (delItem.length > 0) {
            $(trList[i]).addClass("hid");
            $(trList[i]).find('[name*=".ContactFormatString"]').val("");
            $(trList[i]).find('[name*=".ContactFormatString"]').removeClass("enabled");
            $(trList[i]).find('[name*=".Type"]').removeClass("enabled");
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
        $(lastRow).find(".btn.btn-red").attr("id", "contactDel" + indexNum);
        $(lastRow).find('[name*="ContactFormatString"]').attr("name", "client.Contacts[" + indexNum + "].ContactFormatString").removeAttr("id");
        $(lastRow).find('[name*="Type"]').attr("name", "client.Contacts[" + indexNum + "].Type").removeAttr("id");
        $(lastRow).find('[name*="Client"]').attr("name", "client.Contacts[" + indexNum + "].Client.Id").removeAttr("id");
        $(lastRow).find('[name*=".Date"]').attr("name", "client.Contacts[" + indexNum + "].Date").removeAttr("id");
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

function updatePort(_this) {
    if ($(_this).attr("href") == "" || $(_this).attr("href") == null) {
        var portVal = $(_this).find("span").html();
        $("#endpoint_Port").val(portVal);
        $("#endpoint_PortVal").val(portVal);
        $(".portInfoData").html(portVal);
    }
}

function updatePortGrid(portCount) {
    var columns = 24;
    portCount = parseInt(portCount);
    var itemsInRow = portCount == undefined || portCount == null || portCount < 24 ? 1 : (Math.round(portCount / columns) + ((portCount > columns) && ((portCount % columns) > 0) ? 1 : 0));
    var currentPort = 1;
    var html = "";
    for (j = 0; j < itemsInRow; j++) {
        html += "<tr>";
        for (i = 0; i < columns; i++) {
            if ((1 + portCount) > currentPort) {
                html += '<td><a class="port free" target="_blank" onclick="updatePort(this)"><span>' + currentPort + "</span></a></td>";
                currentPort++;
            }
        }
        html += "</tr>";
        if ((portCount + 1) <= currentPort) {
            break;
        }
    }
    $("#switchPorts tbody").html(html);
}

function createFixedIp(endpointId) {
    if (endpointId == 0) {
        var orderItem = $("[name='order.EndPoint.Id']");
        endpointId = orderItem.length > 0 ? orderItem.val() : 0;
    }
    var errorText = "Ошибка при присвоении фиксированного IP !";
    $(".fixedIp").html("");
    $.ajax({
        url: cli.getParam("baseurl") + "Client/GetStaticIp?id=" + endpointId,
        type: "POST",
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

function GetPortConnectionState(id) {
    $(".endpointStateStatus" + id).html("<img src='" + $("[name='imagePathOfProcess']").val() + "' class='PortConnectionStateWait'/>");
    $.ajax({
        url: cli.getParam("baseurl") + "AdminOpen/ClientEndpointGetInfoShort?id=" + id,
        type: "POST",
        dataType: "json",
        success: function(data) {
            var ip = false;
            var state = false;

            if (data != undefined && data != null && data.port != undefined && data.port != null) {
                state = data.port;
            }
            if (data != undefined && data != null && data.lease != undefined) {
                ip = data.lease != null;
            }
            var html = "<div class='PortConnectionState'><div class='ip " + (ip ? "isGreen" : "isRed") + "'>IP</div><div class='state " + (state ? "isGreen" : "isRed") + "'>порт</div></div>";
            $(".endpointStateStatus" + id).html(html);
        },
        error: function() {
            $(".endpointStateStatus" + id).html("<div class='PortConnectionState'>Состояние порта не установлено</div>");
        },
        statusCode: {
            404: function() {
                $(".endpointStateStatus" + id).html("<div class='PortConnectionState'>Состояние порта не установлено</div>");
            }
        }
    });
}

function GetCableConnectionState(id) {
    $(".endpointCableStatus" + id).html("<img src='" + $("[name='imagePathOfProcess']").val() + "' class='PortConnectionStateWait'/>");
    $.ajax({
        url: cli.getParam("baseurl") + "AdminOpen/ClientEndpointGetCableState?id=" + id,
        type: "POST",
        dataType: "json",
        success: function(data) {
            var state = "Состояние порта не установлено";

            if (data != undefined && data != null && data.state != undefined && data.state != null) {
                state = data.state;
            }
            var html = "<div class='PortConnectionState'><div class='state " + (state == "" ? "isGreen" : "isRed") + "'>" + (state == "" ? "Проблем не обнаружено" : state) + "</div></div>";
            $(".endpointCableStatus" + id).html(html);
        },
        error: function() {
            $(".endpointCableStatus" + id).html("<div class='PortConnectionState'>Состояние порта не установлено</div>");
        },
        statusCode: {
            404: function() {
                $(".endpointCableStatus" + id).html("<div class='PortConnectionState'>Состояние порта не установлено</div>");
            }
        }
    });
}

function updatePortsState(data, noPortUpdate) {
    var linkA = $("[name='clientUrlExampleA']");
    var linkB = $("[name='clientUrlExampleB']");
	if (noPortUpdate == undefined) {
		$("#endpoint_PortVal").val("0");
		$("#endpoint_Port").val("0");
	}
	var currentSwitchId = $("#SwitchDropDown").val();
	if (currentSwitchId == null || currentSwitchId == undefined || parseInt(currentSwitchId) == 'NaN') {
		currentSwitchId = $("#SwitchDropDown").attr("pastvalue");
	}
	var oldPortVal = $("#endpoint_Port").attr("oldvalue");
	var oldSwitchVal = $("#endpoint_Port").attr("oldswitch");
	console.log(oldPortVal);
	console.log(oldSwitchVal);
	console.log(currentSwitchId);
	if (oldPortVal != undefined && oldPortVal !== '' && oldSwitchVal != undefined && oldSwitchVal !== '' && oldSwitchVal === currentSwitchId) {
		$("#endpoint_PortVal").val(oldPortVal);
		$("#endpoint_Port").val(oldPortVal);
	}

    if (linkA.length > 0 && linkB.length > 0) {
    	var portCount = $("#SwitchDropDown [value='" + currentSwitchId + "']").attr("maxports");
        updatePortGrid(portCount);
        var portTags = $("#switchPorts .port");
        portTags.each(function() {
            var portTagItem = this;
            $(portTagItem).removeAttr("href");
            $(portTagItem).attr("title", "свободный порт");
            var switchComment = $("#SwitchComment");
            if (data == null || data.Comment == null) {
                switchComment.html("");

            } else {
                switchComment.html(data.Comment);
            }
            if (data == null || data.Ports == null) {
                $(portTagItem).addClass("free");
            } else {
                used = true;
                for (var i = 0; i < data.Ports.length; i++) {
                    if ($(portTagItem).html().indexOf("<span>" + data.Ports[i].endpoint + "</span>") != -1) {
                        //Физ.лицо, Юр лицо
                        $(portTagItem).attr("href", (data.Ports[i].type == 0 ? linkA.val() : linkB.val()) + "/" + data.Ports[i].client);
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

function GetBusyPorts(noPortUpdate) {
	if ($("#SwitchDropDown").length > 0) {
		var switchId = $("#SwitchDropDown").val();
		switchId = switchId == undefined || switchId === 0 || switchId === '' ? $("#SwitchDropDown").attr("pastvalue") : switchId;
        $("#switchPorts .port").removeClass("free").removeClass("client");
        if (switchId != null && switchId !== "") {
            $.ajax({
            	url: cli.getParam("baseurl") + "Client/getBusyPorts?id=" + switchId,
                type: "POST",
                dataType: "json",
                success: function (data) {
                	if (noPortUpdate == undefined || noPortUpdate == true) {
                		updatePortsState(data);
	                } else {
                		updatePortsState(data, true);
	                }
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
	if (id == undefined) {
		return;
	}
    if ($(htmlElement).hasClass("ajaxRun") == false) {
        $(htmlElement).addClass("ajaxRun");
        if (!timeout)
            timeout = 5000;
        try {
            $.ajax({
                url: cli.getParam("baseurl") + "AdminOpen/PingEndpoint?Id=" + id,
                type: "POST",
                dataType: "json",
                success: function(data) {
                    $(htmlElement).removeClass("ajaxRun");
                    $(htmlElement).html(data);
	                if (data.indexOf("ничего не ответил") != -1) {
	                	$("[sw='" + $(htmlElement).attr("swId") + "']").removeClass("red").addClass("red");
	                } else {
	                	$("[sw='" + $(htmlElement).attr("swId") + "']").removeClass("red");
	                }
                },
                error: function(data) {
                    $(htmlElement).removeClass("ajaxRun");
                    $(htmlElement).html("<b class='undefined'>Не запустить проверку коммутатора. Пробую снова.</b>");
                    $("[sw='" + $(htmlElement).attr("swId") + "']").removeClass("red").addClass("red");
                }
            });
        } catch (e) {
        	$(htmlElement).removeClass("ajaxRun");
        	$("[sw='" + $(htmlElement).attr("swId") + "']").removeClass("red").addClass("red");
        }
    }
}


//удаление статического Ip
function deleteStaticIp(element) {
    $(element).parents(".staticIpElement").remove();
    $(".staticAddressError").remove();
    UpdateStaticIp();
}

//добавление статического Ip
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
        } else {
            var valOfMsk = $(this).parents(".staticIpElement").find(".value").val();
            if (valOfMsk == 0) {
                updateSubnateVal($(this).parents(".staticIpElement").find(".subnet"), valOfMsk);
            }
        }
    });
}

function setValidMask(elem) {
    $(elem).unbind("change");
    $(elem).change(function() {
        var minValue = 8;

        if ($(this).val() == "0") {
            $(this).attr("min", "0");
        } else {
            $(this).attr("min", minValue);
        }

        $(this).parent().find(".error").remove();
        if ($(this).val() > 30) {
            $(this).parent().append('<div class="error">Маска не может быть больше 30<div class="msg"></div><div class="icon"></div></div>');
            return;
        }
        if ($(this).val() < minValue && $(this).val() != "0") {
            $(this).parent().append('<div class="error">Маска не может быть меньше ' + minValue + '<div class="msg"></div><div class="icon"></div></div>');
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
            url: cli.getParam("baseurl") + "Client/GetSubnet?mask=" + (mask == "0" ? "32" : mask),
            type: "POST",
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


//удаление точки подключения
function deleteEndpointProof(id) {
    $("#ModelForEndpointProofDelete [name='endpointId']").val(id);
    $("#ModelForEndpointProofDelete #endpointId").html(id);
}

var clientPage = new ClientPage();
//вызов функций при загрузке, дополнительные действия
$(function() {
    clientPage.ShowBlocks();
    changeVisibilityUpdateEach();
    $("div.Client.InfoPhysical").removeClass("hid");
    $("div.Client.InfoLegal").removeClass("hid");
    $("[name='newUserAppeal']").val("");

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
        var id = 0;
        if ($("[name='endpointId']").length > 0) {
            id = $(this).parent().find("[name='endpointId']").val();
        } else {
            id = $("[name='endpointId']").val();
        }
        if (id !== 0) {
        	$(this).css("cursor", "pointer");
        	$(this).css("font-weight", "bold");
	        $(this).click(function() {
		        updateEndpointStatus(id, this);
	        });
	        updateEndpointStatus(id, this);
	    }
    });
    phantomFor();
   // GetBusyPorts();
});