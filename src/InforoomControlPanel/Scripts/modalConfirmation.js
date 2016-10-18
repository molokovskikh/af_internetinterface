//переписал 'confirm', из-за того, что писать тесты неудобно
//html в 'modalConfirmation.cshtml'
var confirm = function (message, buttonConfirm, buttonDecline, href) {
	confirm._cssModalDialog = "#messageConfirmDialog";
	confirm._cssHref = "#messageConfirmationLink";
	confirm._cssDecline = "#messageConfirmationExit";
	confirm._cssText = "#messageConfirmationText";
	confirm.Message = message;
	confirm.ButtonConfirm = buttonConfirm;
	confirm.ButtonDecline = buttonDecline;

	confirm.Href = function (href) {
		//ссылка
		$(confirm._cssHref).attr("href", href);
		//сообщение
		$(confirm._cssText).html(confirm.Message);
		//кнопки
		$(confirm._cssHref).html(confirm.ButtonConfirm == undefined
			? $(confirm._cssHref).attr("default-text") : confirm.ButtonConfirm);
		$(confirm._cssDecline).html(confirm.ButtonDecline == undefined
			? $(confirm._cssDecline).attr("default-text") : confirm.ButtonDecline);
		//вывод окна
		$(confirm._cssModalDialog).modal("show");
	};
	if (href != undefined) {
		confirm.Href(href);
	}
}
window.confirm = confirm;
//для всех ссылок, в которых есть 'confirm', предотвращаем переход по ссылке.
$(function() {
	$("a[onclick*='confirm(']").on('click', function(e) {
		e.preventDefault();
		confirm.Href($(this).attr("href"));
	});
});