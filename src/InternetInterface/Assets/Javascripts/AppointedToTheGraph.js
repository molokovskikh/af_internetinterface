function fillData() {
	var req = {
		date: $('#graph_date').val(),
		clientId: $('#clientId').val(),
		selectedBrigadId: $("#brigadSelect option:selected").val()
	};
	$.post("GetGraph", req, function (data) {
		DrawTable(data);
	});
}

function DrawTable(data) {
	$('#graph_table').remove();
	var str = '<table class="table" id="graph_table"> <thead> <tr>';
	str += '<tbody> ';
	for (var i = 0; i < data.intervals.length; i++) {
		str += '<tr id="tr_' + i + '"></tr>';
	}
	$('#graph_div').append(str + '</tr> </thead> </table>');
	for (var i = 0; i < data.intervals.length; i++) {
		var tds = '';
		tds += '<td>' + data.intervals[i].InStringFormat + '</td>';
		tds += '<td id="tdi_' + i + '"> <input type="radio" name="graph_button" value="' + i + '">';
		if (data.intervals[i].Busy) {
			tds += '<a class="graph_link" href="../Search/Redirect?filter.ClientCode=' + data.intervals[i].Request.clientId + '">' + data.intervals[i].Request.clientId + '</a>';
			tds += '<a href="#" onclick="Delete(this);" value=""> <img class="deletePointControl" src="../images/onebit_32.png"> </a>';
			tds += '<input type=hidden class="intervalValue" value="' + data.intervals[i].Request.Id + '">';
			tds += '<input type=hidden class="brigadValue" value="' + this.selectedBrigadId + '">';
			if (data.intervals[i].Request.isReserved) tds += 'Резерв';
		}
		tds += '</td>';
		$('#tr_' + i).append(tds);
	}
}

$(function () {
	writeMessage("");
	$('#graph_date').datepicker({
		inline: true
	}).change(function () { fillData(); });
	$('#brigadSelect').change(function () {
		fillData();
	});
	Init();
});

function Init() {
	var req = {};
	req.clientId = $('#clientId').val();
	req.graph_date = $('#graph_date').val();
	$.post("InitGraph", req, function (data) {
		var optStr = '';
		$(data.brigads).each(function () {
			optStr += '<option value=' + this.Id + '>' + this.Name + '</option>';
		});
		$('#brigadSelect').append(optStr);
		DrawTable(data);
	});
}

function Reserv() {
	var req = {};
	var check_field = $('input:radio[name="graph_button"]:checked');
	req.graph_button = check_field.val();
	req.graph_date = $('#graph_date').val();
	req.brigadId = $("#brigadSelect option:selected").val();
	req.clientId = $('#clientId').val();
	$.post("ReservGraph", req, function (data) {
		writeMessage(data, "notice");
		var appended_field = check_field.parent();
		appended_field.empty();
		appended_field.append('Резерв');
		fillData();
	});
}

function Delete(elem) {
	var req = {};
	req.clientId = $('#clientId').val();
	req.interval = $(elem).parent().children('input.intervalValue:first').val();
	$.post("DeleteGraph", req, function (data) {
		window.location.href = '../Search/Redirect?filter.ClientCode=' + $('#clientId').val();
	});
}

function Save() {
	var req = {};
	req.clientId = $('#clientId').val();
	req.brigadId = $("#brigadSelect option:selected").val();
	var check_field = $('input:radio[name="graph_button"]:checked');
	req.graph_button = check_field.val();
	req.graph_date = $('#graph_date').val();
	$.post("SaveGraph", req, function (data) {
		if (data) {
			writeMessage("Заявка включена в расписание", "notice");
			window.location.href = '../Search/Redirect?filter.ClientCode=' + $('#clientId').val();
		}
		else {
			writeMessage("Не выбран интервал времени", "error");
		}
	});
}

function writeMessage(text, clazz) {
	$('#message_graph_block').css('display', 'none');
	$('#message_graph').empty();
	if (text != "") {
		$('#message_graph_block').css('display', 'block');
		$('#message_graph').attr('class', 'message ' + clazz);
		$('#message_graph').append(text);
	}
}