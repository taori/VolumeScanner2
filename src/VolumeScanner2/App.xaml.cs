using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using VolumeScanner2.Resources;

namespace VolumeScanner2
{
	/// <summary>
	/// Interaktionslogik für "App.xaml"
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			this.ShutdownMode = ShutdownMode.OnMainWindowClose;
			
			DispatcherUnhandledException += OnDispatcherUnhandledException;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
			TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
			Dispatcher.UnhandledException += DispatcherOnUnhandledException;

			var allTypes = typeof(App).Assembly.GetTypes();
			var filteredTypes = allTypes.Where(d =>
				typeof(MetroWindow).IsAssignableFrom(d)
				&& typeof(MetroWindow) != d
				&& !d.IsAbstract
			);

			foreach (var type in filteredTypes)
			{
				var defaultStyle = this.Resources["MetroWindowDefault"];
				this.Resources.Add(type, defaultStyle);
			}

			base.OnStartup(e);
		}

		private void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
		{
			args.Handled = true;
			var w = MainWindow as MetroWindow;
			w.ShowMessageAsync(GenericResources.Title_Exception, args.Exception.Message + Environment.NewLine + args.Exception.StackTrace);
		}

		private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
		{
			args.SetObserved();
			var w = MainWindow as MetroWindow;
			w.ShowMessageAsync(GenericResources.Title_Exception, args.Exception.Message + Environment.NewLine + args.Exception.StackTrace);
		}

		private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
		{
			var w = MainWindow as MetroWindow;
			w.ShowMessageAsync(GenericResources.Title_Exception, args.ExceptionObject.ToString());
		}

		private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
		{
			args.Handled = true;
			var w = MainWindow as MetroWindow;
			w.ShowMessageAsync(GenericResources.Title_Exception, args.Exception.Message + Environment.NewLine + args.Exception.StackTrace);
		}
	}
}
