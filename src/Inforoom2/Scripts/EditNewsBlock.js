console.log("EditNewsBlock.js");

$(".NewsVersionCheckbox").on("click", valueChanged);

function valueChanged() {
	if ($('.NewsVersionCheckbox').is(":checked")) {
		document.getElementById("newsBlock_Body").hidden = false;
		document.getElementById("newsBlock_Body_Label").hidden = false;


	} else {
		document.getElementById("newsBlock_Body").hidden = true;
		document.getElementById("newsBlock_Body_Label").hidden = true;
	}
};