/**
 * Главный объект клиента
 */
function ControlPanel() {
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


		this.initializeInputHelpers();
		this.showMessages();
	}

	this.initializeInputHelpers = function() {
		var refreshDatepicker = function() {
			var input = $(this).parent().find("input[type='hidden']").get(0);
			var timepicker = $(this).parent().find(".timepicker").val();
			var datepicker = $(this).parent().find(".datepicker").val();

			if (input) {
				$(input).val(datepicker + " " + timepicker);
			}
		}
		console.log("init datepickers");
		$(".datepicker, .timepicker").each(function() {
			var oldEvent = $(this).get(0).onchange;
			$(this).get(0).onchange = function() {
				refreshDatepicker();
				if (oldEvent)
					oldEvent();
			}
			refreshDatepicker.call(this);
		});


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
	this.getTemplate = function(name) {
		return this.templates[name] ? this.templates[name].toHTML() : null;
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

	this.showMessages = function() {
		var msg = this.getCookie("SuccessMessage", true);
		if (msg) {
			$('.server-message').prepend('<div class="col-md-12 alert alert-success">' + msg + '</div>');
		} else {
			msg = this.getCookie("ErrorMessage", true);
			if (msg)
				$('.server-message').prepend('<div class="col-md-12 alert alert-danger">' + msg + '<button type="button" class="close" data-dismiss="alert" aria-label="Close"><span aria-hidden="true">&times;</span></button></div>');
		}
	}

	this.checkCity = function() {
		//показать выбор городов
		$('.city .name').on("click", function(e) {
			$('.city .cities').show();
			e.stopPropagation();
		});
		//убрать выбор городов
		$('body').on("click", function() {
			$('.city .cities').hide();
		});
		$('.cities a').on("click", function() {
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
		$(wnd.getElement()).find('.button.ok').on("click", function() {
			var city = $(wnd.getElement()).find(".UserCity").html();
			wnd.remove();
			cli.setCookie("userCity", city);
		});

		//cancel button event
		$(wnd.getElement()).find(".button.cancel").on("click", function() {
			var content = cli.getTemplate("SelectCityWindow");
			wnd.pushContent(content);
			$(wnd.getElement()).find('.cities a').on("click", function() {
				cli.setCookie("userCity", this.innerHTML);
				window.location.reload();
			});
			$(wnd.getElement()).find('.button.cancel').on("click", wnd.popContent.bind(wnd));
		});
	}

	this.initialize();
}

var Base64 = {
	_keyStr: "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=",
	//метод для кодировки в base64 на javascript 
	encode: function(input) {
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
	decode: function(input) {
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
	_utf8_encode: function(string) {
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
	_utf8_decode: function(utftext) {
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

cli = new ControlPanel();

function scrollTo(element, speed) {
	speed = speed != null ? speed : 300;
	$('html, body').animate({
		scrollTop: $(element).offset().top
	}, speed);
}

function scrollToPoint(point, speed) {
	speed = speed != null ? speed : 300;
	$('html, body').animate({
		scrollTop: point
	}, speed);
}

var maxObj_;
var $scrollingDiv;

function scrollElOnScroll(objToScroll) {
	$scrollingDiv = $(objToScroll);
	var objForException = $(".none");
	$(window).scroll(function() {
		$scrollingDiv
			.animate({
				"marginTop": $(window).scrollTop() + "px"
			}, 50);
	});
}

function scrollPan_add(text_bool) {
	var sc = $("#scroll_top");
	maxObj_ = $(".breadcrumb");
	if (maxObj_.length == 0 || $(maxObj_).offset() == null) {
		return;
	}
	if (sc.length == 0 && $(document).height() > $(window).height()) {
		$("body").append("<div id='scroll_top' class='top' current_y='0'><a class='scroll_block'><span class='scroll_img' >" +
			"</span>" + ((text_bool == true) ? "<span class='scroll_text' >наверх</span>" : "") + "</a></div>");

		$("#scroll_top").css("margin-top", "0 px");
		$("#scroll_top").css("margin-left", $(".sidebar-menu").width() + "px");

		if (text_bool == false) {
			$("#scroll_top").css("min-width", "50px");
			$("#scroll_top").css("width", "50px");
			$(".scroll_block").css("width", "50px");
		}

		$("#scroll_top").click(function() {
			if ($("#scroll_top").attr("current_y") == 0 && $(window).scrollTop() == 0) {
				var point = 0;
				if ($("footer.main").length > 0) {
					point = $("footer.main").offset().top;
				}
				if ($(".main-menu").length > 0 && point < $(".main-menu").height()) {
					point = $(".main-menu").height();
				}
				scrollToPoint(point);
			}
			if (($("#scroll_top").attr("current_y") > 0 && $("#scroll_top").hasClass("top")) || $("#scroll_top").hasClass("top") == false) {
				if ($("#scroll_top").hasClass("top")) {
					var point_y = $("#scroll_top").attr("current_y");
					scrollToPoint(point_y);
					$("#scroll_top").attr("current_y", "0");
					$("#scroll_top").removeClass("top");
					if (text_bool == true) {
						$(".scroll_text").html("наверх");
					}
				} else {
					var point_y = $("#scroll_top").attr("current_y");
					scrollToPoint(point_y);
					$("#scroll_top").addClass("moves");
					$("#scroll_top").attr("current_y", $(window).scrollTop());
					$("#scroll_top").addClass("top");
					if (text_bool == true) {
						$(".scroll_text").html("вниз");
					}
				}

			}
		});

		$(window).scroll(function(d) {
			if ($(maxObj_).offset().top < $(window).scrollTop()) {
				$("#scroll_top").removeClass("top");
				if (text_bool == true) {
					$(".scroll_text").html("наверх");
				}
				if ($("#scroll_top").hasClass("moves") == false) {
					$("#scroll_top").attr("current_y", "0");
				}
			} else {

				$("#scroll_top").removeClass("moves");
				$("#scroll_top").addClass("top");
				if (text_bool == true) {
					$(".scroll_text").html("вниз");
				}
			}
		});

		scrollElOnScroll($("#scroll_top"));
	} else {
		setTimeout(scrollPan_add(false), 2000);
	}

}

$(function() {

	var cookieValue = $.cookie("ShowLargeMenu");
	if (cookieValue != null && cookieValue == "true") {
		$(".sidebar-menu-inner .sidebar-collapse-icon .entypo-menu").click();
	}
	$(".sidebar-menu-inner .sidebar-collapse-icon .entypo-menu").click(function() {
		var cookieValue = $.cookie("ShowLargeMenu");
		if (cookieValue != null && cookieValue == "true")
			$.cookie("ShowLargeMenu", "false", { expires: 365, path: "/" });
		else
			$.cookie("ShowLargeMenu", "true", { expires: 365, path: "/" });
	});
	setTimeout(scrollPan_add(false), 2000);

});