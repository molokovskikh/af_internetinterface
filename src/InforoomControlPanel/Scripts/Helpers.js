﻿/**
 * Allyst ™ - Universal Integration Service.
 * 
 * CONFIDENTIAL NOTICE:
 * 
 * All information contained herein is, and remains
 * the property of Allyst Incorporated and its suppliers,
 * if any.  The intellectual and technical concepts contained
 * herein are proprietary to Allyst Incorporated
 * and its suppliers and may be covered by U.S. and Foreign Patents,
 * patents in process, and are protected by trade secret or copyright law.
 * Dissemination of this information or reproduction of this material
 * is strictly forbidden unless prior written permission is obtained
 * from Allyst Incorporated.
 *
 * Copyright (C) 2013 Allyst Incorporated. *
 */
/**
 * В данном файле описаны вспомогательные функции окружения. То есть то, что расширяет окружение.
 * @author Sarychev Alexei
 */

/**
 * Приводит первую букву строги в верхний регистр
 */
String.prototype.ucFirst = function () {
	var str = this;
	if (str.length) {
		str = str.charAt(0).toUpperCase() + str.slice(1);
	}
	return str;
};
/**
 * Приводит первую букву строги в нижний регистр
 */
String.prototype.lcFirst = function () {
	var str = this;
	if (str.length) {
		str = str.charAt(0).toLowerCase() + str.slice(1);
	}
	return str;
};

/**
 * Превращает строку в HTML элемент.
 * 
 * @returns {HTMLElement} HTML элемент
 */
String.prototype.toHTML = function () {
	var div = document.createElement('div');
	div.innerHTML = this;
	if (div.children.length > 1)
		return div;
	else
		return div.children[0];
}
/*
 * Анимирует объект клон JqueryAnimate
 * 
 */
HTMLElement.prototype.animate = function () {
	var obj = $(this);
	return obj.animate.apply(obj, arguments);
}

/**
 * Синоним querySelector
 * @returns {HTMLElement} html элемент
 */
HTMLElement.prototype.selector = function () {
	return this.querySelector.apply(this, arguments);
}
/**
 * Производит поиск по селектору в родительских элементах
 * 
 * @param {String} str Селектор
 * @returns {HTMLElement} html элемент
 */
HTMLElement.prototype.selectorParent = function (str) {
	return $(this).parent(str).get(0);
}

/**
 * Синоним querySelectorAll
 * @returns {HTMLElement[]} Массив html элементов
 */
HTMLElement.prototype.selectorAll = function () {
	return this.querySelectorAll.apply(this, arguments);
}

/**
 * Удаление элементов из DOM
 */
HTMLElement.prototype.remove = function () {
	if (!this.parentNode) {
		console.warn('No parent node', this);
		return;
	}
	this.parentNode.removeChild(this);
}
/**
 * Блокирование доступа к элементу
 */
HTMLElement.prototype.block = function () {
	var pos = this.getAbsolutePosition();
	var size = this.getSize();
	var blocker = a.create('div');
	blocker.style.position = 'absolute';
	blocker.style.top = 0 + 'px';
	blocker.style.left = 0 + 'px';;
	blocker.style.right = 0 + 'px';;
	blocker.style.bottom = 0 + 'px';;
	//    blocker.style.position = 'absolute';
	//    blocker.style.top = pos.top+'px';
	//    blocker.style.left = pos.left+'px';;
	//    blocker.style.width = size.width+'px';;
	//    blocker.style.height = size.height+'px';;
	blocker.style.zIndex = 3000;
	blocker.addClass('ui-widget-overlay');
	blocker.addClass('ui-front');
	blocker.addClass('blocker');
	this.blocker = blocker;
	//document.body.appendChild(blocker);
	this.appendChild(blocker);
	console.log(this)
}
/**
 * Разблокирование доступа к элементу
 */
HTMLElement.prototype.unblock = function () {
	this.blocker.remove();
	this.blocker = undefined;
}

/**
 * Получение относительной позиции элемента
 * @return  {Object} {top:t, left:l} позиция элемента
 */
HTMLElement.prototype.getPosition = function () {
	var pos = $(this).position();
	return pos;
}

/**
 * Получение абсолютной позиции элемента
 * @return  {Object} {top:t, left:l} позиция элемента
 */
HTMLElement.prototype.getAbsolutePosition = function () {
	return $(this).offset();
}

/**
 * Получение размеров HTML элемента
 * @return {Object} {width:w,height:h} размеры элемента
 */
HTMLElement.prototype.getSize = function () {
	var w = $(this).width();
	var h = $(this).height();
	var obj = { width: w, height: h };
	return obj;
}

/**
 * Получение размеров HTML элемента с учетом границ и паддинга
 * @return {Object} {width:w,height:h} размеры элемента
 */
HTMLElement.prototype.getInnerSize = function () {
	var w = $(this).innerWidth();
	var h = $(this).innerHeight();
	var obj = { width: w, height: h };
	return obj;
}

/**
 * Получение размеров HTML элемента с учетом границ и паддинга и маргина
 * @return {Object} {width:w,height:h} размеры элемента
 */
HTMLElement.prototype.getOuterSize = function () {
	var w = $(this).outerWidth(true);
	var h = $(this).outerHeight(true);
	var obj = { width: w, height: h };
	return obj;
}

/**
 * Вставка элемента после заданного элемента
 * 
 * @param {HTMLElement} node Элемент, который надо вставить
 * @param {HTMLElement} referenceNode Элемент, после которго надо вставить
 */
HTMLElement.prototype.insertAfter = function (node, referenceNode) {
	this.insertBefore(node, referenceNode.nextSibling);
}
/**
 * Вставка элемента после заданного элемента
 * 
 * @param {HTMLElement} referenceNode Элемент, после которго надо вставить
 */
HTMLElement.prototype.appendAfter = function (node) {
	node.parentNode.insertAfter(this, node);
}
/**
 * Вставка элемента после заданного элемента
 * 
 * @param {HTMLElement} referenceNode Элемент, после которго надо вставить
 */
HTMLElement.prototype.prependBefore = function (node) {
	node.parentNode.insertBefore(this, node);
}

/**
 * Добавление события, которое должно отработать только один раз.
 * 
 * @param {String} name -  Название события
 * @param {Function} callback - Функция обратного вызова, при вызове события
 */
HTMLElement.prototype.addOneListener = function (name, callback, bubbles) {
	var self = this;
	var func = function () {
		callback();
		self.removeEventListener(name, func, bubbles);
	}
	self.addEventListener(name, func, bubbles);
}
HTMLElement.prototype.event = HTMLElement.prototype.addEventListener;

/**
 * Замена старого элемента новым.
 * 
 * @param {HTMLElement} node Новый элемент
 * @param {HTMLElement} referenceNode Старый элемент
 */
HTMLElement.prototype.switchChild = function (node, referenceNode) {
	this.insertBefore(node, referenceNode);
	this.removeChild(referenceNode);
}
/**
 * Замена элемента новым.
 * 
 * @param {HTMLElement} node Новый элемент
 */
HTMLElement.prototype.replaceWith = function (node) {
	return this.parentNode.switchChild(node, this);
}

/**
 * Добавление класса класса
 * @param {String} name Имя класса
 */
HTMLElement.prototype.addClass = function (name) {
	var str = this.getAttribute('class');
	str = str ? str : '';
	var arr = str.split(' ');
	if (arr.indexOf(name) != -1)
		arr.splice(arr.indexOf(name), 1);
	arr.push(name);
	var newstr = arr.join(' ', arr);
	this.setAttribute('class', newstr);
}

/**
 * Удаление класса
 * @param {String} name Имя класса
 */
HTMLElement.prototype.removeClass = function (name) {
	var str = this.getAttribute('class');
	var arr = str.split(' ');
	if (arr.indexOf(name) != -1)
		arr.splice(arr.indexOf(name), 1);
	var newstr = arr.join(' ', arr);
	this.setAttribute('class', newstr);
}
/**
 * Вставка текущего HTML элемента в другой. 
 * Тоже самое, что и append, но с инверсией.
 * 
 * @param {HTMLElement} el Родительский элекмет
 */
HTMLElement.prototype.appendTo = function (el) {
	return el.appendChild(this);
}
/**
 * Получение реальных стилей элемента
 * 
 * @return {Object} Стиль объекта
 */
HTMLElement.prototype.getStyle = function (el) {
	var res = window.getComputedStyle(this);
	return res;
}

/**
 * Подтверждение при нажатии на элемент
 * 
 * @param {Function} callback - Функция обратного вызова после изменения блока
 */
HTMLElement.prototype.confirm = function (callback) {
	var def = new Deferred();
	this.parentNode.style.position = 'relative';
	var span = null;
	var self = this;
	document.addEventListener('click', function () {
		if (span) {
			span.remove();
			span = null;
		}
	}, true)
	this.addEventListener('click', function (e) {
		//console.log('time')
		setTimeout(function () {
			var pos = self.getPosition();
			var size = self.getOuterSize();
			span = a.create('span');
			span.appendAfter(self);
			span.addClass('button');
			span.addClass('red');
			span.addClass('confirm');
			span.style.position = 'absolute';
			span.innerHTML = 'confirm';
			span.style.left = (pos.left - span.getOuterSize().width - parseInt(self.getStyle().marginRight) + size.width) + 'px';
			span.style.top = (pos.top) + 'px';
			span.style.opacity = 0;
			span.style.display = 'block';
			//console.log('animate');
			span.animate({ opacity: 1 }, 400, function () {
				span.event('click', def.resolve);
			});
		}, 1);
	}.bind(this));
	def.done(callback);
}
/**
 * Возможность менять текстблока пользователем
 * @param {Function} callback - Функция обратного вызова после изменения блока
 */
HTMLElement.prototype.edit = function (callback, options) {
	if (HTMLElement.prototype.editingElement == this)
		return;
	if (!options)
		options = {};
	console.log('edit', this);
	if (this.parentNode.getStyle().position == 'static') {
		console.log(this.parentNode.getStyle().position, ' making abs');
		var staticPosFlag = true;
		this.parentNode.style.position = 'relative';
	}
	if (typeof callback != 'function')
		callback = function () { };
	setTimeout(function () {
		if (HTMLElement.prototype.editingElement)
			HTMLElement.prototype.editingElement.stopEdit();
		HTMLElement.prototype.editingElement = this;
		this.startname = this.innerHTML;

		var input = a.create('input');
		input.type = 'text';

		input.value = this.innerHTML;
		//раковая модель - паддинг и бордер не расширяет инпут.текст
		var expansion = 8;
		var width = this.getSize().width + expansion;
		var height = this.getSize().height + expansion;
		input.style.width = width + 'px';
		input.style.height = height + 'px';
		input.style.padding = '3px';
		input.style.position = 'absolute';
		input.style.top = (this.getPosition().top - expansion / 2) + 'px';
		input.style.left = (this.getPosition().left - expansion / 2) + 'px';
		input.style.fontFamily = this.getStyle().fontFamily;
		input.style.fontSize = this.getStyle().fontSize;
		input.style.fontWeight = this.getStyle().fontWeight;
		input.style.lineHeight = this.getStyle().lineHeight;
		input.className = this.className;
		input.addClass('nameChange');

		input.appendAfter(this);
		input.focus();
		input.setSelectionRange(0, input.value.length);
		this.input = input;
		var noHide = false;
		input.addEventListener('mousedown', function (e) {
			noHide = true;
			console.log(e);
			e.stopPropagation();
		})
		this.docFunc = function (e) {
			setTimeout(function () {
				console.log(noHide);
				if (noHide) {
					noHide = false;
					return;
				}
				this.stopEdit();
				if (staticPosFlag)
					this.parentNode.style.position = 'static';
				callback();
			}.bind(this), 1);
		}.bind(this);
		document.body.event('click', this.docFunc, true);
		input.addEventListener('keydown', function (e) {
			setTimeout(function () {
				this.innerHTML = input.value
				var newWidth = this.getSize().width + expansion;
				var w = width > newWidth ? width : newWidth;
				input.style.width = w + 'px';
				if (width > newWidth)
					this.innerHTML = this.startname;
				if (options.direction == 'left') {
					var pos = this.getPosition();
					var size = this.getOuterSize();
					var margin = parseInt(this.getStyle().marginRight);
					input.style.top = (pos.top - expansion / 2) + 'px';
					input.style.left = (-input.getOuterSize().width + pos.left + size.width - margin + expansion / 2) + 'px';
				}
			}.bind(this), 1);
		}.bind(this));
		input.addEventListener('keyup', function (e) {
			if (e.keyCode == 13) {
				this.stopEdit();
				this.innerHTML = input.value;
				if (staticPosFlag)
					this.parentNode.style.position = 'static';
				callback();
			}
		}.bind(this), true);

	}.bind(this), 1);
}
/**
 * Отмена возможности менять пользователем
 * 
 */
HTMLElement.prototype.stopEdit = function () {
	console.log('stopEdit', this);
	//this.input.replaceWith(this);
	this.input.remove();
	this.innerHTML = this.startname;
	HTMLElement.prototype.editingElement = null;
	document.body.removeEventListener('click', this.docFunc, true);
}

/**
 * Глобальная реактивная мышь
 * x,y - позиция относительно края экрана
 * px,py - позиция относительно документа
 */
mouse = { x: 0, y: 0, px: 0, py: 0 };
window.addEventListener('mousemove', function (e) {
	mouse.x = e.clientX;
	mouse.y = e.clientY;
	mouse.px = e.pageX;
	mouse.py = e.pageY;
}, true)
window.addEventListener('dragover', function (e) {
	mouse.x = e.clientX;
	mouse.y = e.clientY;
	mouse.px = e.pageX;
	mouse.py = e.pageY;
}, true)

/**
*   Функция классического наследования. 
*   @param func Конструктор от которого необходимо наследовать
*/
Function.prototype.extending = function (func) {
	//Проверяем, возможно уже наследовались.
	if (this.hasInterface(func))
		return;

	//Создается пустой объект, чтобы он стал прототипом и там можно было хранить методы ребенка, то есть текущей функции
	//В то время как родитель будет выше и его методы не будут изменяться, так как он тоже хранит методы в прототипе
	var F = function () { };
	F.prototype = func.prototype;
	var f = new F();
	//Наследуем от пустого объекта
	this.prototype = f;
	//Правильная ссылка на себя (иначе конструктором будет F = function(){ })
	this.prototype.constructor = this;
	//Ближайший родитель
	this.parent = func;

	//Добавляем интерфейсы
	if (!this.interfaces)
		this.interfaces = [];

	//Если у родителя уже есть интерфейсы, то добавляем их
	if (func.interfaces != undefined)
		for (var i = 0; i < func.interfaces.length; i++)
			if (this.interfaces.indexOf(func.interfaces[i]) == -1)
				this.interfaces.push(func.interfaces[i]);
	//Добавляем самого родителя в интерфейс
	this.interfaces.push(func);
}
/**
 * Функция проверки наличия интерфейса у объекта.
 * @param name конструктор интерфейса
 */
Function.prototype.hasInterface = function (name) {
	if (this === name)
		return true;
	else if (this.interfaces == undefined)
		return false
	else if (this.interfaces.indexOf(name) != -1)
		return true;
	else
		return false
}

/**
 * Функция получения имени функции.
 */
Function.prototype.getName = function () {
	var funcNameRegex = /function (.{1,})\(/;
	var results = (funcNameRegex).exec(this.toString());
	return (results && results.length > 1) ? results[1] : "";
}

Function.prototype.callbackMock = function () {

	console.log('Mocking callback ', this, arguments);
	if (arguments[arguments.length - 1])
		arguments[arguments.length - 1]();
}