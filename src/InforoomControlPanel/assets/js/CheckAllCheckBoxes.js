CheckAllCheckBoxesWithPartId = function(obj){
	$('input[id*=part]').prop('checked', $(obj).attr('allChb') == null);
	if ($(obj).attr('allChb') != null) $(obj).removeAttr('allChb');
	else $(obj).attr('allChb', 'true');
}