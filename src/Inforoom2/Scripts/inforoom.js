/**
 * Главный объект клиента
 */
function Inforoom() {
	/**
     * @var Открытые окна
     */
	this.windows = [];

	/**
     * @var Параметры клиента. Пополняются параметрами с сервера.
     */
	this.params = {};
	this.templates = {};

	/**
     * Контсруктор
     */
	this.initialize = function() {
		window.cli = this;
		console.log(this);

		var paramDivs = $(".JavascriptParams div").get();
		for (var i in paramDivs) {
			var div = paramDivs[i];
			this.params[$(div).attr("id")] = div.innerHTML;
		}
		$(".JavascriptParams").remove();
		console.log("Params from server", this.params);

		var templateDivs = $(".HtmlTemplates > div").get();
		for (var i in templateDivs) {
			var div = templateDivs[i];
			this.templates[$(div).attr("id")] = div.outerHTML;
		}
		$(".HtmlTemplates").remove();
		console.log("Templates added", this.templates);

		if (this.getParam("CallMeBack") == "1")
			this.callMeBackWindow();
		$(".header .call").on("click", this.callMeBackWindow.bind(this));

		$(".error .msg").on("mouseover", function() {
			$(this).fadeOut(800);
		});
		$(".error .icon").on("mouseover", function() {
			$(this).parent().find(".msg").fadeIn();
		});
		$('input').attr('autocomplete', 'off');
		this.checkCity();
		this.showMessages();
	}

	this.callMeBackWindow = function() {
		var wnd = this.createWindow("Обратный звонок", this.getTemplate("CallMeBackWindow"));
		wnd.block();
	}
	/**
     * Создает окно
     * @returns {Window} Объект окна
     */
	this.createWindow = function(name, content) {
		var window = new Window(name, content);
		this.windows.push(window);
		window.render(document.body);
		return window;
	}

	/**
     * Получает элемент по селектору
     * 
     * @param {String} selector Строка селектор
     * @returns {HTMLElement} элемент
     */
	this.css = function(selector) {
		return $(selector).get(0);
	}

	/**
     * Получает набор элементов по селектору
     * 
     * @param {String} selector Строка селектор
     * @returns {HTMLElement[]} Массив HTML элементов
     */
	this.cssAll = function(selector) {
		return $(selector).toArray();
	}

	this.post = function(query, data, callback) {

	}

	this.get = function(query, data, callback) {

	}

	this.setCookie = function(name, value, options) {
		if (value == null) {
			this.setCookie(name, "", { expires: -1 });
			return;
		}
		options = options || {};
		if (!options.path)
			options.path = '/';

		var expires = options.expires;

		if (typeof expires == "number" && expires) {
			var d = new Date();
			d.setTime(d.getTime() + expires * 1000);
			expires = options.expires = d;
		}
		if (expires && expires.toUTCString) {
			options.expires = expires.toUTCString();
		}
		value = encodeURIComponent(value);
		var updatedCookie = name + "=" + value;

		for (var propName in options) {
			updatedCookie += "; " + propName;
			var propValue = options[propName];
			if (propValue !== true) {
				updatedCookie += "=" + propValue;
			}
		}
		document.cookie = updatedCookie;
	}

	this.getCookie = function(name, eraseFlag) {
		var matches = document.cookie.match(new RegExp(
			"(?:^|; )" + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + "=([^;]*)"
		));
		var ret = matches ? (matches[1]) : undefined;
		if (eraseFlag)
			this.setCookie(name, null);
		if (ret) {
			ret = decodeURIComponent(ret);
		}
		return ret;
	}
	this.getTemplate = function(name) {
		return this.templates[name] ? this.templates[name].toHTML() : null;
	}
	this.getParam = function(name) {
		return this.params[name];
	}

	this.setParam = function(name, value) {
		this.params[name] = value;
	}

	/**
	* Отображает диалоговое окно, с сообщением о подтверждении пользовательского действия
	*
	* @param {String} message Сообщение, отоброжаемое пользователю
	* @param {Function} callback Функция обратного вызова, которая будет исполнена, если пользователь согласен
	* @param {String} title Название окна - по умолчанию "Вы уверены?"
	* @returns {Window} элемент окна
	*/
	this.areYouSure = function ( message, callback, title) {
		if (title == null)
			title = "Вы уверены?";
		var str = "<div class='whiteblocktext'>" + message + "</div>";
		var html = str.toHTML();
		$(html).height(100);
		var window = this.createWindow(title,html);
		window.add2Buttons();
		$(window.getOkButton()).on("click", function () {
			window.remove();
			callback();
		});
		$(window.getCancelButton()).on("click", window.remove.bind(window));
		return window;
	}

	this.showMessages = function() {
		var msg = this.getCookie("SuccessMessage", true);
		if (msg)
			this.showSuccess(msg);
	
		var msg2 = this.getCookie("ErrorMessage", true);
		if(msg2)
			this.showError(msg2);
	}

	this.showError = function(msg) {
		var div = this.showSuccess(msg);
		$(div).addClass("error");
		return div;
	}

	this.showSuccess = function (msg) {
		var div = this.getTemplate("notification");
		$(div).find(".message").append(msg);
		$(".errorContainer").append(div);
		$(div).find(".hide").on("click", function() {
			$(div).remove();
		});
		return div;
	}

	this.checkCity = function () {
		//показать выбор городов
		$('.city .name').on("click", function(e) {
			$('.city .cities').show();
			e.stopPropagation();
		});
		//убрать выбор городов
		$('body').on("click", function () {
			$('.city .cities').hide();
		});
		$('.cities a').on("click", function () {
			$('.city .name').html($(this).html());
			cli.setCookie("userCity", this.innerHTML);
			window.location.reload();
		});
		var city = this.getCookie("userCity");
		if (city == null)
			this.showCityWindow();
	};

	this.showCityWindow = function() {
		var wnd = this.createWindow("Выберите город", this.getTemplate("CityWindow"));
		wnd.block();
		//ok button event
		$(wnd.getElement()).find('.click.ok').on("click", function() {
			var city = $(wnd.getElement()).find(".UserCity").html();
			wnd.remove();
			cli.setCookie("userCity", city);
		});

		//cancel button event
		$(wnd.getElement()).find(".click.cancel").on("click", function() {
			var content = cli.getTemplate("SelectCityWindow");
			wnd.pushContent(content);
			$(wnd.getElement()).find('.cities a').on("click", function() {
				cli.setCookie("userCity", this.innerHTML);
				window.location.reload();
			});
			$(wnd.getElement()).find('.click.cancel').on("click", wnd.popContent.bind(wnd));
		});
	}

	this.initialize();
}


window.onload = function() {
	cli = new Inforoom();
}