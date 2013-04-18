function DeleteWriteOff(path, writeOffInfo) {
	YesNoDialog("Удаление списания", "Вы действительно хотите удалить данное списание? (" + writeOffInfo + ")", function () {
		window.location = path;
	});
}