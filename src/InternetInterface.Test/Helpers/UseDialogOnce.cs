using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WatiN.Core.DialogHandlers;
using WatiN.Core.Interfaces;

namespace InternetInterface.Test.Helpers
{
	public class UseDialogOnce : IDisposable
	{
		private DialogWatcher dialogWatcher;
		private IDialogHandler dialogHandler;

		public UseDialogOnce(DialogWatcher dialogWatcher, IDialogHandler
			dialogHandler)
		{
			this.dialogWatcher = dialogWatcher;
			this.dialogHandler = dialogHandler;

			if (dialogWatcher != null) {
				dialogWatcher.Add(dialogHandler);
			}
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool managedAndNative)
		{
			dialogWatcher.Remove(dialogHandler);

			dialogWatcher = null;
			dialogHandler = null;
		}
	}
}
