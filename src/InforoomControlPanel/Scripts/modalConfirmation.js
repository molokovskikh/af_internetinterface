//переписал 'submit', из-за того, что писать тесты неудобно
//html в 'modalsubmitation.cshtml'
var submit = function (message, buttonsubmit, buttonDecline, href) {
	submit._cssModalDialog = "#messageConfirmDialog";
	submit._cssHref = "#messageConfirmationLink";
	submit._cssDecline = "#messageConfirmationExit";
	submit._cssText = "#messageConfirmationText";
	submit.Message = message;
	submit.Buttonsubmit = buttonsubmit;
	submit.ButtonDecline = buttonDecline;

	submit.Href = function (href) {
		//ссылка
		$(submit._cssHref).attr("href", href);
		//сообщение
		$(submit._cssText).html(submit.Message);
		//кнопки
		$(submit._cssHref).html(submit.Buttonsubmit == undefined
			? $(submit._cssHref).attr("default-text") : submit.Buttonsubmit);
		$(submit._cssDecline).html(submit.ButtonDecline == undefined
			? $(submit._cssDecline).attr("default-text") : submit.ButtonDecline);
		//вывод окна
		$(submit._cssModalDialog).modal("show");
	};
	if (href != undefined) {
		submit.Href(href);
	}
} 
window.submit = submit;
//для всех ссылок, в которых есть 'submit', предотвращаем переход по ссылке.
$(function () {
	$("a[onclick*='submit(']").on('click', function (e) {
		e.preventDefault();
		submit.Href($(this).attr("href"));
	});
});