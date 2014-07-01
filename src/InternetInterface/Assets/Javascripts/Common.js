function YesNoDialog(headerText, dialogText, successFunction) {
	var buttonsOpts = {};
	buttonsOpts["Да"] = function () {
		successFunction();
		$(this).dialog("close");
	};
	buttonsOpts["Нет"] = function () {
		$(this).dialog("close");
	};

	$('<div></div>').appendTo('body')
	.html('<div class="shureDeleteAdress">' + dialogText + '</div>')
	.dialog({
		modal: true, title: headerText, zIndex: 10000, autoOpen: true,
		width: '350', resizable: false,
		buttons: buttonsOpts,
		close: function (event, ui) {
			$(this).remove();
		}
	});
}

$('button[data-reveal-id]').live('click', function (e) {
	e.preventDefault();
	var modalLocation = $(this).attr('data-reveal-id');
	$('#' + modalLocation).reveal($(this).data());
});
