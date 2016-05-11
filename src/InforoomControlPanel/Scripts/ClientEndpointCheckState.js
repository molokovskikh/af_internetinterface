var containterCss = ".ClientEndpointInfoBlock";
var errorContainer = '<h4 class="media-heading">{0}</h4>';

function OnClientEndPointStart(clientEndPointId) {
	$(containterCss).html("");
	GetBaseInfoInto(clientEndPointId, GetBlockInfoInto, GetError);
}

function GetError() {
	var html = tmpl('templateError', String.format(errorContainer, "Не удалось получить ответ от коммутатора"));
	$(".ClientEndpointInfoBlock").append(html);
}

function GetBlockInfoInto(data) {
	if (data != null) {
		GetHtmlForSwitch_BaseInfo(data);
		switch (data.point.clientSwitchType) {
		case 1: //Catalyst
			return GetHtmlForSwitch_Catalyst(data);
		case 2: //Linksys
			return GetHtmlForSwitch_Linksys(data);
		case 3: //Dlink
			return GetHtmlForSwitch_Dlink(data);
		case 4: //Huawei
			return GetHtmlForSwitch_Huawei(data);
		default:
			return GetHtmlForSwitch_NotDetected(data);
		}
	}
}

function ShowLoadingBlock() {
	var html = tmpl('templateHideLoadingBlock');
	$(".ClientEndpointInfoBlock").html(html);
}

function HideLoadingBlock() {
	$(".ClientEndpointInfoBlock").html("");
}


function GetBaseInfoInto(id, func, error) {
	ShowLoadingBlock();
	$.ajax({
		url: cli.getParam("baseurl") + "AdminOpen/ClientEndpointGetInfo?id=" + id,
		type: 'POST',
		dataType: "json",
		success: function(data) {
			HideLoadingBlock();
			func(data);
		},
		error: function() {
			HideLoadingBlock();
			error();
		},
		statusCode: {
			404: function() {
				HideLoadingBlock();
				error();
			}
		}
	});
}

function GetHtmlForSwitch_BaseInfo(data) {
	data.url = {};
	data.url.InfoPhysical = $("[name='UrlInfoPhysical']").val();
	data.url.InfoLegal = $("[name='UrlInfoLegal']").val();
	data.url.EditSwitch = $("[name='UrlEditSwitch']").val();
	var html = tmpl('templateBaseInfo', data);
	$(".ClientEndpointInfoBlock").append(html);
}

function GetHtmlForSwitch_Catalyst(data) {
	var html = tmpl('templateCatalystInfo', data);
	$(".ClientEndpointInfoBlock").append(html);
};

function GetHtmlForSwitch_Linksys(data) {
	var html = tmpl('templateLinksysInfo', data);
	$(".ClientEndpointInfoBlock").append(html);

};

function GetHtmlForSwitch_Dlink(data) {
	var html = tmpl('templateDlinkInfo', data);
	$(".ClientEndpointInfoBlock").append(html);

};

function GetHtmlForSwitch_Huawei(data) {
	var html = tmpl('templateHuaweiInfo', data);
	$(".ClientEndpointInfoBlock").append(html);

};

(function() {
	var cache = {};

	this.tmpl = function tmpl(str, data) {
		// Выяснить, мы получаем шаблон или нам нужно его загрузить
		// обязательно закешировать результат
		var fn = !/\W/.test(str) ?
			cache[str] = cache[str] ||
			tmpl(document.getElementById(str).innerHTML) :

			// Сгенерировать (и закешировать) функцию, 
			// которая будет служить генератором шаблонов
			new Function("obj",
				"var p=[],print=function(){p.push.apply(p,arguments);};" +

				// Сделать данные доступными локально при помощи with(){}
				"with(obj){p.push('" +

				// Превратить шаблон в чистый JavaScript
				str
				.replace(/[\r\t\n]/g, " ")
				.split("<%").join("\t")
				.replace(/((^|%>)[^\t]*)'/g, "$1\r")
				.replace(/\t=(.*?)%>/g, "',$1,'")
				.split("\t").join("');")
				.split("%>").join("p.push('")
				.split("\r").join("\\'")
				+ "');}return p.join('');");

		// простейший карринг(термин функ. прог. - прим. пер.)
		// для пользователя
		return data ? fn(data) : fn;
	};
})();