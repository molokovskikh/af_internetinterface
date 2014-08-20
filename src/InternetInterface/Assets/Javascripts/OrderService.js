function addOrderService(orderId) {
	var index = getIndex(0, "#addOrderServiceLink");
	var id = "orderService" + index;
	var name = "order.OrderServices[" + index + "]";
	var html = "<tr id='" + id + "'>" +
		"<td>" +
			"<input type='text' name='" + name + ".Description' value=''>" +
	"</td>" +
		"<td>" +
			"<input type='text' name='" + name + ".Cost' value='' class='valid'>" +
	"</td>" +
		"<td>" +
			"<input type='checkbox' name='" + name + ".IsPeriodic'>" +
	"</td>" +
		"<td>" +
			"<a href='javascript:' onclick=deleteOrderService('" + id + "')>Удалить</a>" +
	"</td>" +
		"</tr>";
	jQuery("#OrderServiceTable" + orderId).append(html);
}

function deleteOrderService(serviceId) {
	$('#' + serviceId).remove();
}
function getIndex(begin, selector) {
	var index = parseInt(jQuery(selector).data('index'));
	if (!index)
		index = begin;
	jQuery(selector).data('index', ++index);
	return index;
}