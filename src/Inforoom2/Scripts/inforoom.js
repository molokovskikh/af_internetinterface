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

	/**
     * Контсруктор
     */
	this.initialize = function() {
		console.log(this);

		var msg = this.getCookie("SuccessMessage", true);
		if (msg)
			alert(msg);
		var city = this.getCookie("userCity");
		if (city == null) {
			this.showRegionDialog();
		}
	}

	/**
     * Создает окно
     * @returns {Window} Объект окна
     */
	this.createWindow = function(name, content) {
		var window = new Window();
		this.windows.push(window);
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
		return ret;o
	}

	this.getParam = function(name) {
		return this.params[name];
	}

	this.setParam = function(name, value) {
		this.params[name] = value;
	}

	this.areYouSure = function(element, message, callback) {
		element.event("click", function(e) {
			var window = this.createWindow("Вы уверены?", message);
			window.css(".ok").event("click", callback);
		}.bind(this));
	}

	

	this.showRegionDialog = function() {
		var buttonsConfig = [
		{
			text: "Верно",
			click: function () {
				$(this).dialog("close");
				var e = document.getElementById("viewBagCity");
				cli.setCookie("userCity", e.innerHTML);
			}
		},
		{
			text: "Нет, я выберу город из списка",
			click: function () {
				var dialogBody = $('#dialogMessage');
				dialogBody.html($('#citiesList'));
				$('#citiesList').show();

			}
		}
		];
		$("#dialogMessage").show();
		$("#dialogMessage").dialog({
			modal: true,
			draggable: true,
			resizable: false,
			position: ['center', 'top'],
			show: 'blind',
			hide: 'blind',
			width: 400,
			dialogClass: 'ui-dialog',
			buttons: buttonsConfig
		});
	};

	this.onCityChanged = function() {
		var e = document.getElementById("city");
		var city = e.options[e.selectedIndex].text;
		this.setCookie("userCity", city);
		window.location.reload();
	}


	this.initialize();
}

cli = new Inforoom();