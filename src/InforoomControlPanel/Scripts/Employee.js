function CheckEmployeeWithServiceRequest(obj) {
	if ($(obj).attr("phantomChecked") == "false") {
	$("[name='workBegin']").val("");
	$("[name='workEnd']").val("");
	$("[name='workStep']").val("");
	}
}