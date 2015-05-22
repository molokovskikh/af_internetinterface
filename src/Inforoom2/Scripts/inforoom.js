/**
 * Главный объект клиента
 */
function Inforoom() {
	/**
     * @var Открытые окна
     */
	this.windows = [];

	/**
     * @var Параметры клиентского приложения. Пополняются параметрами с сервера.
     */
	this.params = {};
	/**
	 * @var Шаблоны клиентского приложения. Присылает сервер.
	 */
	this.templates = {};

	/**
     * Конструктор
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
		this.initInputs();
		this.checkCity();
		this.showMessages();
	}

	/**
	 * Приводит инпуты на странице к форме, которую задумал дизайнер
	 */
	this.initInputs = function() {
		$(".error .msg").on("mouseover", function () {
			$(this).fadeOut(800);
		});
		$(".error .msg").on("click", function () {
			$(this).parent().find("input, textarea").focus();
		});
		$(".error .icon").on("mouseover", function () {
			$(this).parent().find(".msg").fadeIn();
		});
		$('input').attr('autocomplete', 'off');
	}

	/**
     * Создает окно, в котором отображается создание заявки на обратный звонок
     * @returns {Window} Объект окна
     */
	this.callMeBackWindow = function() {
		var wnd = this.createWindow("Обратный звонок", this.getTemplate("CallMeBackWindow"));
		wnd.block();
		return wnd;
	}
	/**
     * Создает окно
     * @returns {Window} Объект окна
     */
	this.createWindow = function(name, content) {
		var window = new InforoomWindow(name, content);
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

	/**
	 * Создает куки. Кодирует его в Base64 (это баг IE)
	 *
	 * @param {String} name Имя куки
	 * @param {String} value Значение куки
	 * @param {Object} options Объект, поля которого являются дополнительными параметрами (Например expires, domain)
	 */
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
		value = Base64.encode(value);
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

	/**
     * Получает куку
     * 
     * @param {String} name Имя куки
     * @param {Boolean} eraseFlag Удалять ли куку, после получения
     * @returns {String} Значение куки или undefined
     */
	this.getCookie = function(name, eraseFlag) {
		var matches = document.cookie.match(new RegExp(
			"(?:^|; )" + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + "=([^;]*)"
		));
		var ret = matches ? (matches[1]) : undefined;
		if (eraseFlag)
			this.setCookie(name, null);
		if (ret) {
			ret = Base64.decode(ret);
		}
		return ret;
	}

	/**
     * Получает HTML шаблон, переданный сервером.
     * 
     * @param {String} name Имя шаблона
     * @returns {String} Код шаблона
     */
	this.getTemplate = function(name) {
		return this.templates[name] ? this.templates[name].toHTML() : null;
	}

	/**
     * Получает параметр клиентского приложенияю. Не переносятся на следующую страницу.
	 * Если необходимо, чтобы параметр жил между страницами, необходимо использовать куки.
	 *
     * @param {String} name Имя параметра
     * @returns {String} Значение параметра
     */
	this.getParam = function(name) {
		return this.params[name];
	}

	/**
     * Назначает параметр клиентского приложенияю. Не переносятся на следующую страницу.
	 * Если необходимо, чтобы параметр жил между страницами, необходимо использовать куки.
	 *
     * @param {String} name Имя параметра
     * @param {String} value Значение
     */
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

	/**
	* Отображает сообщение переданные сервером клиентскому приложению
	*/
	this.showMessages = function() {
		var msg = this.getCookie("SuccessMessage", true);
		if (msg)
			this.showSuccess(msg);
	
		var msg2 = this.getCookie("ErrorMessage", true);
		if(msg2)
			this.showError(msg2);
	}

	/**
	* Отображает сообщение об ошибке
	* @param {String} msg Текст сообщения
	*/
	this.showError = function(msg) {
		var div = this.showSuccess(msg);
		$(div).addClass("error");
		return div;
	}

	/**
	* Отображает сообщение об успешном выполнении операции
	* @param {String} msg Текст сообщения
	*/
	this.showSuccess = function (msg) {
		var div = this.getTemplate("notification");
		$(div).find(".message").append(msg);
		$(".errorContainer").append(div);
		$(div).find(".hide").on("click", function() {
			$(div).remove();
		});
		return div;
	}

	/**
	* Проверяет состояние пользователя и отображает страницу выбора региона, если это необходимо
	*/
	this.checkCity = function () {
		//показать выбор городов
		$('.city .name, .city .arrow').on("click", function(e) {
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

	/**
	* Отображает страницу выбора региона
	*/
	this.showCityWindow = function() {
		var wnd = this.createWindow("Выберите город", this.getTemplate("CityWindow"));
		wnd.block();
		//ok button event
		$(wnd.getElement()).find('.click.ok').on("click", function() {
			var city = $(wnd.getElement()).find(".UserCity").html();
			wnd.remove();
			cli.setCookie("userCity", city);
		});

		//обработка крестика - выбор первого города
		$(wnd.getElement()).find('.close').on("click", function () {
			var cities = $(wnd.getElement()).find('.cities a');
			cli.setCookie("userCity", cities.get(0).innerHTML);
			window.location.reload();
		});

		//обработка
		$(wnd.getElement()).find('.cities a').on("click", function () {
			cli.setCookie("userCity", this.innerHTML);
			window.location.reload();
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

	/**
	* Получение текущей даты (присылается с сервера)
	*/
	this.getCurrentDate = function() {
		var date = new Date();
		var timestamp = this.getParam("Timestamp");
		date.setTime(timestamp * 1000);
		return date;
	};

	//Запускаем конструктор
	this.initialize();
} 

Date.prototype.getDaysInMonth = function() {
	return new Date(this.getYear(), this.getMonth()+1, 0).getDate();
}

var Base64 = {
	_keyStr: "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=",
	//метод для кодировки в base64 на javascript 
	encode: function (input) {
		var output = "";
		var chr1, chr2, chr3, enc1, enc2, enc3, enc4;
		var i = 0
		input = Base64._utf8_encode(input);
		while (i < input.length) {
			chr1 = input.charCodeAt(i++);
			chr2 = input.charCodeAt(i++);
			chr3 = input.charCodeAt(i++);
			enc1 = chr1 >> 2;
			enc2 = ((chr1 & 3) << 4) | (chr2 >> 4);
			enc3 = ((chr2 & 15) << 2) | (chr3 >> 6);
			enc4 = chr3 & 63;
			if (isNaN(chr2)) {
				enc3 = enc4 = 64;
			} else if (isNaN(chr3)) {
				enc4 = 64;
			}
			output = output +
			 this._keyStr.charAt(enc1) + this._keyStr.charAt(enc2) +
			 this._keyStr.charAt(enc3) + this._keyStr.charAt(enc4);
		}
		return output;
	},

	//метод для раскодировки из base64 
	decode: function (input) {
		var output = "";
		var chr1, chr2, chr3;
		var enc1, enc2, enc3, enc4;
		var i = 0;
		input = input.replace(/[^A-Za-z0-9\+\/\=]/g, "");
		while (i < input.length) {
			enc1 = this._keyStr.indexOf(input.charAt(i++));
			enc2 = this._keyStr.indexOf(input.charAt(i++));
			enc3 = this._keyStr.indexOf(input.charAt(i++));
			enc4 = this._keyStr.indexOf(input.charAt(i++));
			chr1 = (enc1 << 2) | (enc2 >> 4);
			chr2 = ((enc2 & 15) << 4) | (enc3 >> 2);
			chr3 = ((enc3 & 3) << 6) | enc4;
			output = output + String.fromCharCode(chr1);
			if (enc3 != 64) {
				output = output + String.fromCharCode(chr2);
			}
			if (enc4 != 64) {
				output = output + String.fromCharCode(chr3);
			}
		}
		output = Base64._utf8_decode(output);
		return output;
	},
	// метод для кодировки в utf8 
	_utf8_encode: function (string) {
		string = string.replace(/\r\n/g, "\n");
		var utftext = "";
		for (var n = 0; n < string.length; n++) {
			var c = string.charCodeAt(n);
			if (c < 128) {
				utftext += String.fromCharCode(c);
			} else if ((c > 127) && (c < 2048)) {
				utftext += String.fromCharCode((c >> 6) | 192);
				utftext += String.fromCharCode((c & 63) | 128);
			} else {
				utftext += String.fromCharCode((c >> 12) | 224);
				utftext += String.fromCharCode(((c >> 6) & 63) | 128);
				utftext += String.fromCharCode((c & 63) | 128);
			}
		}
		return utftext;

	},

	//метод для раскодировки из urf8 
	_utf8_decode: function (utftext) {
		var string = "";
		var i = 0;
		var c = c1 = c2 = 0;
		while (i < utftext.length) {
			c = utftext.charCodeAt(i);
			if (c < 128) {
				string += String.fromCharCode(c);
				i++;
			} else if ((c > 191) && (c < 224)) {
				c2 = utftext.charCodeAt(i + 1);
				string += String.fromCharCode(((c & 31) << 6) | (c2 & 63));
				i += 2;
			} else {
				c2 = utftext.charCodeAt(i + 1);
				c3 = utftext.charCodeAt(i + 2);
				string += String.fromCharCode(((c & 15) << 12) | ((c2 & 63) << 6) | (c3 & 63));
				i += 3;
			}
		}
		return string;
	}
}

	cli = new Inforoom(); 
