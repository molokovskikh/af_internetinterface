EventEmitter = function () {
	this.events = {};
	this.eventTags = {};
	this.subscribes = {};
	this.stopEventsFlag = false;
	this.svgRouter = null;
}

/**
 * Перенаправление событий с элемента документа на объект
 * 
 * @param {Element} element Визуальный элемент Raphael
 */
EventEmitter.prototype.routeSVGEvents = function (element) {
	this.svgRouter = element;
	element.node.addEventListener('dblclick', this.emitEvent.bind(this, 'dblclick', {}));
	element.node.addEventListener('click', this.emitEvent.bind(this, 'click', {}));
	element.node.addEventListener('mouseover', this.emitEvent.bind(this, 'mouseover', {}));
	element.node.addEventListener('mousedown', this.emitEvent.bind(this, 'mousedown', {}));
	element.node.addEventListener('mouseup', this.emitEvent.bind(this, 'mouseup', {}));
	element.node.addEventListener('mouseout', this.emitEvent.bind(this, 'mouseout', {}));
	$(element.node).on('mouseenter', this.emitEvent.bind(this, 'mouseenter', {}));
	$(element.node).on('mouseleave', this.emitEvent.bind(this, 'mouseleave', {}));
	var self = this;
	element.drag(
            function (dx, dy, x, у, e) { self.emitEvent('drag', {}, e) },
            function (x, y, e) { self.emitEvent('dragstart', {}, e) },
            function (e) { self.emitEvent('dragend', {}, e) });

}
/**
 * Добавление события объекту
 * @param {String} name -  Название события
 * @param {String} tag - Маркировка события (для группирования)
 * @param {Function} callback - Функция обратного вызова, при вызове события
 */
EventEmitter.prototype.addEventListener = function (name, callback, tag) {
	//    if(this.svgRouter)
	//    {
	//        console.log(name);
	//        if(name =='dragstart')
	//            this.svgRouter.drag(function(){},callback,function(){});
	//        else if(name == 'dragend')
	//            this.svgRouter.drag(function(){},function(){},callback);
	//        else if(name == 'drag')
	//            this.svgRouter.drag(callback,function(){},function(){});
	//        else
	//            this.svgRouter.node.addEventListener(name,callback);
	//       
	//        return;
	//    }

	if (this.events[name] == undefined)
		this.events[name] = [];
	this.events[name].push(callback);
	if (tag)
		this._addTag(tag, name, callback);
}
EventEmitter.prototype.event = EventEmitter.prototype.addEventListener;
/**
 * Добавление события, которое должно отработать только один раз.
 * 
 * @param {String} name -  Название события
 * @param {Function} callback - Функция обратного вызова, при вызове события
 * @param {String} tag - Маркировка события (для группирования)
 */
EventEmitter.prototype.addOneListener = function (name, callback, tag) {
	var self = this;
	var func = function () {
		callback();
		self.removeEventListener(name, func);
	}
	this.addEventListener(name, func);
	if (tag)
		this._addTag(tag, name, func);
}
/**
 * Маркирование события для группировки и быстрого удаления
 * 
 * @param {String} tag - Маркировка события (для группирования)
 * @param {String} name -  Название события
 * @param {Function} callback - Функция обратного вызова, при вызове события
 */
EventEmitter.prototype._addTag = function (tag, name, callback) {
	if (!this.eventTags[tag])
		this.eventTags[tag] = {};
	if (!this.eventTags[tag][name])
		this.eventTags[tag][name] = [];
	this.eventTags[tag][name].push(callback);
}


/**
 * Удаление обработчика c события
 * 
 * @param {String} name -  Название события
 * @param {Function} handler - Обработчик события
 */
EventEmitter.prototype.removeEventListener = function (name, handler) {
	var e = this.events[name];
	if (e)
		for (var i = 0; i < e.length; i++)
			if (e[i] === handler) {
				this.events[name].splice(i, 1);
				return true;
			}
	return false;
}
/**
 * Удаление обработчика c события
 * 
 * @param {String} tag - Маркировка события
 */
EventEmitter.prototype.removeEventListenersByTag = function (tag) {
	if (!this.eventTags[tag])
		return false;
	for (var type in this.eventTags[tag])
		for (var i = 0; i < this.eventTags[tag][type].length; i++)
			this.removeEventListener(type, this.eventTags[tag][type][i]);
	this.eventTags[tag] = null;
}

/**
 * Генерация события на объекте.
 * 
 * @param {Event} event - Событие, которое будет сгенерированно.
 * @return {Boolean} - Было ли отмененно стандартное действие за время исполнения события (e.preventDefault).
 */
EventEmitter.prototype.dispatchEvent = function (event) {
	var e = this.events[event.type];
	if (e) {
		var x = [];
		for (var i = 0; i < e.length; i++)
			x.push(e[i]);
		for (var i = 0; i < x.length; i++) {
			var func = x[i];
			//        	try{
			//                console.log(this,func.toSource(),event)
			func.call(this, event);

			//        	}catch(Error ){
			//        		console.log('!!!ERROR!!! Cant dispatch event' + func.toString(true));
			//        		console.log(Error);
			//        	}
		}
	}
	return event.defaultPrevented;
};

/**
 * Генерация быстрого события на объекте. Короткая версия dispatchEvent.
 * 
 * @param {String} name - Название события
 * @param {Object} data - Данные, передаваемые с событием. Они окажутся в e.detail.
 * @param {Event} e - Изначальное событие на котором основанно искусственное событие. На изначальном событии отразятся все манипуляции с искусственным.
 * @return {Boolean} - Было ли отмененно стандартное действие за время исполнения события (e.preventDefault).
 */
EventEmitter.prototype.emitEvent = function (name, data, e) {
	if (this.stopEventsFlag)
		return false;

	//    if(e)
	//       return this.dispatchEvent(e);

	var event = {};//document.createEvent('CustomEvent');
	//  event.initCustomEvent(name, true, true, data);
	var x = 0;
	event.type = name;
	event.stopPropagation = function () {
		if (e)
			e.stopPropagation();
	}
	event.detail = data;
	event.preventDefault = function () {
		if (e)
			e.preventDefault();
		event.defaultPrevented = true;

	}
	var ret = this.dispatchEvent(event);
	//    for(var name in data)
	//        event[name] = data[name];
	//    var ret =  this.dispatchEvent(event);
	//    if(x && e)
	//        e.stopPropagation();
	//    if(event.defaultPrevented && e)
	//        e.preventDefault();
	return ret;
}
/**
 * Генерация быстрого события на объекте. Короткая версия dispatchEvent.
 * 
 * @param {String} name - Название события
 * @param {Object} data - Данные, передаваемые с событием. Они окажутся в e.detail.
 * @param {Event} e - Изначальное событие на котором основанно искусственное событие. На изначальном событии отразятся все манипуляции с искусственным.
 * @return {Boolean} - Было ли отмененно стандартное действие за время исполнения события (e.preventDefault).
 */
EventEmitter.prototype.lateEvent = function (name, data, e) {
	var func = this.emitEvent.bind(this, name, data, e);
	setTimeout(func, 1);
}

/**
 * Остановка генерации событий
 */
EventEmitter.prototype.stopEvents = function () {
	this.stopEventsFlag = true;
}

/**
 * Возобновление генерации событий после остановки
 */
EventEmitter.prototype.continueEvents = function () {
	this.stopEventsFlag = false;
}

EventEmitter.prototype.listen = function (object, event, callback, tag) {
	if (!tag)
		var tagname = 'main';
	else
		var tagname = tag;
	var obj = { object: object, callback: callback, event: event };
	if (!this.subscribes[tagname])
		this.subscribes[tagname] = [];
	this.subscribes[tagname].push(obj);

	object.addEventListener(event, callback, tag);
}

EventEmitter.prototype.unsubscribeObject = function (object, event, tagname) {
	// console.log('Unsubscribing',this,arguments);
	for (var i in this.subscribes) {
		var tag = this.subscribes[i];
		var len = tag.length;
		for (var j = 0; j < len; j++) {
			var data = tag[j];
			if (data.object === object)
				if (!event || (data.event == event))
					if ((tagname && i == tagname) || !tagname) {  // console.log('unsubscribed')
						// console.log('Unsubscribing',this,arguments);
						data.object.removeEventListener(data.event, data.callback);
						tag.splice(j--, 1);

						--len;
					}


		}

	}
}

EventEmitter.prototype.unsubscribeAll = function () {
	// console.log('Unsubscribing all',this);
	var count = 0;
	for (var i in this.subscribes) {
		var tag = this.subscribes[i];
		for (var j = 0; j < tag.length; j++) {
			var data = tag[j];
			count++;
			data.object.removeEventListener(data.event, data.callback);
		}
	}
	//console.log('total ',count);
	this.subscribes = {};
}