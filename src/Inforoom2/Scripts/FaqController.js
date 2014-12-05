console.log("FaqController.js");
var toggledAnswers = [];

//Отобразить/скрыть кнопки
$(".question").on("click", function () {
		ToggleAnswer(this);
});


//Отобразить/скрыть форму
$(".turn").on("click", ShowQuestionForm);
//$(".HideQuestionForm").on("click", HideQuestionForm);


function ShowQuestionForm() {
	var known = $(".know ");
	if ($(".turn").text().indexOf("Развернуть") > -1) {
		known.attr("class", "know");
		$(".turn").html("Свернуть <div class=\"upward\"></div>");
		$(".upward").attr("class", "upward active");
	} else {
		known.attr("class", "know hidden");
		$(".turn").html("Развернуть <div class=\"upward\"></div>");
		$(".upward").attr("class", "upward");
	}

}
/*
function HideQuestionForm() {
//	$(".turn").show();
	$(".QuestionForm").hide();
	$(".upward active").attr("class", "upward");
}*/

function ToggleAnswer(button) {
	var q = $(button);
	var elem = $(button).find(".answer");
	var pt = $(button).find(".pointer");
	var index = toggledAnswers.indexOf(button);
	console.log("toggling answer", elem);
	if (index != -1) {
		pt.attr("class", "pointer");
		elem.attr("class", "answer hidden");
		q.attr("class", "question");
		toggledAnswers.splice(index, 1);
		return;
	}
	pt.attr("class", "pointer active");
	elem.attr("class", "answer");
	q.attr("class", "question active");
	toggledAnswers.push(button);
}
