var ListItemPositionChanger = function () {
	var _this = this;
	this.selectorParentId= "input[name='ParentId']";
	this.selectorListItem = ".ListItem";
	this.selectorListItemId = ".ListItem input[name='Id']";
	this.selectorListItemButtonUp = "span[name='IndexButtonUp']";
	this.selectorListItemButtonDown = "span[name='IndexButtonDown']";
	this.selectorListItemAjaxUpdateUrl = "input[name='ListItemAjaxUpdateUrl']";

	this.UpdateListItemPositions = function (urlForAjaxUpdate) {
		if ($(_this.selectorListItem).hasClass("ajaxRun") === false) {
			var ajaxArray = new Array();
			$(_this.selectorListItemId).each(function () {
				ajaxArray.push(parseInt($(this).val()));
			//	ajaxArray.unshift(parseInt($(this).val()));
			});
			$(_this.selectorListItem).addClass("ajaxRun");
			var parent = $(_this.selectorParentId);
			var parentId = 0;
			if (parent.length != 0) {
				parentId = parseInt($(parent).val());
			}
			try {
				$.ajax({
					url: urlForAjaxUpdate,
					dataType: "json",
					data: JSON.stringify({ "idList": ajaxArray, "parentId": parentId }),
					contentType: 'application/json; charset=utf-8',
					type: "POST",
					success: function (data) {
						$(_this.selectorListItem).removeClass("ajaxRun");
						if (data !== "") {
							window.confirm(data + " Необходимо обновить страницу.", undefined, undefined, location.href);
						}
					},
					error: function () {
						$(_this.selectorListItem).removeClass("ajaxRun");
						window.confirm("При изменении порядка элементов списка произошла ошибка! Необходимо обновить страницу.", undefined, undefined, location.href);
					}
				});
			} catch (e) {
				$(_this.selectorListItem).removeClass("ajaxRun");
			}
		}
	};
	
	$.fn.moveUp = function () {
		$.each(this, function () {
			$(this).after($(this).prev());
		});
	};

	$.fn.moveDown = function () {
		$.each(this, function () {
			$(this).before($(this).next());
		});
	};

	this.UpdateEventForButtonUp = function () {
		$(_this.selectorListItemButtonUp).each(function () {
			$(this).parent().unbind("click").click(function () {
				$(this).parents(_this.selectorListItem).moveUp();
				_this.UpdateListItemPositions($(_this.selectorListItemAjaxUpdateUrl).val());
			});
		});
	};
	this.UpdateEventForButtonDown = function () {
		$(_this.selectorListItemButtonDown).each(function () {
			$(this).parent().unbind("click").click(function () {
				$(this).parents(_this.selectorListItem).moveDown();
				_this.UpdateListItemPositions($(_this.selectorListItemAjaxUpdateUrl).val());
			});
		});
	};
	this.OnLoad = function () {
		_this.UpdateEventForButtonUp();
		_this.UpdateEventForButtonDown();
	};
}