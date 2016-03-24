using System;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using VolumeScanner2.Interfaces;
using VolumeScanner2.Proxies;
using VolumeScanner2.Resources;

namespace VolumeScanner2.Extensions
{
	public enum YesNoAbortState
	{
		Yes,
		No,
		Abort
	}

	public static class INotifyPropertyChangedExtensions
	{
		public static async Task<bool> ConfirmAsync(this INotifyPropertyChangedEx source, string title, string message, string yesText = null, string noText = null)
		{
			var window = App.Current.MainWindow as MetroWindow;
			var dialogSettings = GetDefaultDialogSettings();
			dialogSettings.NegativeButtonText = noText ?? GenericResources.Option_No;
			dialogSettings.AffirmativeButtonText = yesText ?? GenericResources.Option_Yes;
			dialogSettings.DefaultButtonFocus = MessageDialogResult.Affirmative;

			var result = await window.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, dialogSettings).ConfigureAwait(false);
			return result == MessageDialogResult.Affirmative;
		}

		public static async Task<YesNoAbortState> ConfirmWithCancelAsync(this INotifyPropertyChangedEx source, string title, string message, string yesText = null, string noText = null, string cancelText = null)
		{
			var window = App.Current.MainWindow as MetroWindow;
			var dialogSettings = GetDefaultDialogSettings();
			dialogSettings.NegativeButtonText = cancelText ?? GenericResources.Option_Abort;
			dialogSettings.AffirmativeButtonText = yesText ?? GenericResources.Option_Yes;
			dialogSettings.FirstAuxiliaryButtonText = noText ?? GenericResources.Option_No;
			dialogSettings.DefaultButtonFocus = MessageDialogResult.FirstAuxiliary;

			var result = await window.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, dialogSettings).ConfigureAwait(false);
			switch (result)
			{
				case MessageDialogResult.Negative:
					return YesNoAbortState.Abort;
				case MessageDialogResult.Affirmative:
					return YesNoAbortState.Yes;
				case MessageDialogResult.FirstAuxiliary:
					return YesNoAbortState.No;
				case MessageDialogResult.SecondAuxiliary:
					return YesNoAbortState.No;
			}

			return YesNoAbortState.Abort;
		}

		public static async Task ShowMessageAsync(this INotifyPropertyChangedEx source, string title, string message, string affirmativeText = null)
		{
			var window = App.Current.MainWindow as MetroWindow;
			var dialogSettings = GetDefaultDialogSettings();
			dialogSettings.AffirmativeButtonText = affirmativeText ?? GenericResources.Option_Ok;

			await window.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, dialogSettings).ConfigureAwait(false);
		}

		public static async Task<IProgressController> ShowProgressAsync(this INotifyPropertyChangedEx source, string title, string message, CancellationTokenSource cts = null, bool closeOnCancel= true)
		{
			var window = App.Current.MainWindow as MetroWindow;
			var dialogSettings = GetDefaultDialogSettings();

			var controller = await window.ShowProgressAsync(title, message, cts != null, dialogSettings);
			var controllerProxy = new ProgressControllerProxy(controller);

			EventHandler cancelHandler = null;
			cancelHandler = delegate
			{
				controllerProxy.Canceled -= cancelHandler;
				try
				{
					cts?.Cancel();
				}
				catch (ObjectDisposedException e) {}
				finally
				{
					if (controllerProxy.IsOpen && closeOnCancel)
						controllerProxy.CloseAsync();
				}

			};
			controllerProxy.Canceled += cancelHandler;

			return controllerProxy;
		}

		private static MetroDialogSettings GetDefaultDialogSettings()
		{
			var dialogSettings = new MetroDialogSettings();
			dialogSettings.AnimateHide = false;
			dialogSettings.AnimateShow = false;
			return dialogSettings;
		}
	}
}