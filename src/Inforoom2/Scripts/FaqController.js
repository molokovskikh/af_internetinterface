console.log("FaqController.js");
var toggledAnswers = [];

//Отобразить/скрыть кнопки
$(".question").on("click", function () {
		ToggleAnswer(this);
	});

//Отобразить/скрыть форму
$(".turn").on("click", ShowQuestionForm);
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
	var elem = $(button).find(".answer");
	var pt = $(button).find(".pointer");
	var index = toggledAnswers.indexOf(button);
	console.log("toggling answer", elem);
	if (index != -1) {
		pt.attr("class", "pointer");
		elem.attr("class", "answer hidden");
		//elem.css("display", "none");
		toggledAnswers.splice(index, 1);
		return;
	}
	pt.attr("class", "pointer active");
	elem.attr("class", "answer");
	//elem.css("display", "block");
	toggledAnswers.push(button);
}
