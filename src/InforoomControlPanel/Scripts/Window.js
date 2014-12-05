/**
 * Класс окна. В данный момент оборачивает интерфейсы Jquery-Ui
 * 
 * @author Sarychev Alexei
 * @constructor
 */
function Window(title,html)
{
	this.constructor.parent.apply(this,arguments);
	this.title = title;
	this.html = html;
	this.position = {x:null,y:null};
	this.content = null;
	this.contentStack = [];
	this._bindedCloseHandler = null;
   
}
Window.extending(EventEmitter);

/**
 * Отображение окна на экране
 * 
 * @param HTMLElement element DOM элемент, в котором будет отображаться окно
 */
Window.prototype.render = function (element) {

	this.content = cli.getTemplate("WindowLayout");
	$(this.content).find(".title").html(this.title);
	$(this.content).find(".button.close").on("click", this.remove.bind(this));
	element.appendChild(this.content);
	
	if (!this.html)
		var html = document.createElement("div");
	else
		var html = this.html;
	this.getElement().selector('.wrap').appendChild(html);
	this.getElement().addEventListener('mousdown',function(e){e.stopPropagation()});
	this.getElement().addEventListener('click',function(e){e.stopPropagation()});
	this.autofocus();
}

/**
 *  Возвращает HTML элемент окна
 *  
 *  @return HTMLElement Узел окна в документе
 */
Window.prototype.getElement = function()
{
	return this.content;
}

/**
 * Блокировка всего, кроме окна
 * 
 * @return undefined
 */
Window.prototype.block = function()
{
	if(this.blocker)
		return false;
	var div = document.createElement("div");
	div.addClass('blocker');
	div.prependBefore(this.getElement());
	div.event('click',function(e){
		e.stopPropagation();
		this.remove(true);
	}.bind(this));
	this.blocker = div;
}

/**
 *  Удаляет окно
 */
Window.prototype.remove = function(callback)
{
	if(typeof callback != 'function')
		callback = function(){};
	if(this.blocker)
		this.blocker.remove();
	this.getElement().remove();
	this.emitEvent('remove');
	this.removedFlag = true;
	callback();
}

Window.prototype.isRemoved = function()
{
	if(this.removedFlag)
		return true;
	return false;
}

/**
 * Устанаваливает ширину окна
 * 
 * @param {Number} width Ширина
 */
Window.prototype.setWidth = function(width)
{
	this.getElement().style.width  =width+'px';
}

/**
 * Устанаваливает высоту окна
 * 
 * @param {Number} height Высота
 */
Window.prototype.setHeight = function(height)
{
	this.getElement().style.height  =height+'px';
}

/**
 * Устанаваливает название окна
 * 
 * @param {String} title Название окна
 */
Window.prototype.setTitle = function(title)
{
	this.getElement().selector('.title').innerHTML = title;
}

/**
 * Устанаваливает содержимое окна
 * 
 * @param {String} data HTML код контента
 */
Window.prototype.setContent = function(data)
{
	this.getElement().selector('.wrap').firstChild.remove();
	this.getElement().selector('.wrap').appendChild(data);
	this.autofocus();
}

/**
 * Переключает окно на новое содержимое. Старое содержимое сохраняется и моежет быть вызвано,
 * методом popContent();
 * 
 * @param {HTMLElement} data HTML элемент
 */
Window.prototype.pushContent = function(data)
{
	console.log('Push content ',this,data);
	var storage = [];
	var nodes = this.getElement().selector('.wrap > div');
	nodes.remove();
	this.contentStack.push({title: this.title, content : nodes});
	this.getElement().selector('.wrap').appendChild(data);
	this.autofocus();
}

/**
 * Переключает окно на предыдущее содержимое.
 */
Window.prototype.popContent = function()
{
	console.log('Pop content ',this,this.contentStack);
	if(this.contentStack.length == 0)
		throw new Error('Zero content stack length, can\'t pop');
	var data = this.contentStack.pop();
	this.setTitle(data.title);
	this.getElement().selector('.wrap').innerHTML = '';
	this.getElement().selector('.wrap').appendChild(data.content);
}
/**
 * Фокусируется на автофокусных элементах
 */
Window.prototype.autofocus = function()
{
	var input = this.getElement().selector('input[autofocus]');
	if(input)
		input.focus();
}
