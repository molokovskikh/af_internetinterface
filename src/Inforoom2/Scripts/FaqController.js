console.log("FaqController.js");
var toggledAnswers = [];

//Отобразить/скрыть кнопки
$(".ShowAnswer").on("click",function () {
		ToggleAnswer(this);
	});

//Отобразить/скрыть форму
$(".ShowQuestionForm").on("click", ShowQuestionForm);
$(".HideQuestionForm").on("click", HideQuestionForm);


function ShowQuestionForm() {
	$(".ShowQuestionForm").hide();
	$(".QuestionForm").show();
}

function HideQuestionForm() {
	$(".ShowQuestionForm").show();
	$(".QuestionForm").hide();
}

function ToggleAnswer(button) {
	var elem = $(button).parents(".question").find(".answer");
	var index = toggledAnswers.indexOf(button);
	console.log("toggling answer", elem);
	if (index != -1) {
		elem.css("display", "none");
		toggledAnswers.splice(index, 1);
		return;
	}
	elem.css("display", "block");
	toggledAnswers.push(button);
}
