using System;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using VolumeScanner2.Interfaces;

namespace VolumeScanner2.Proxies
{
	public class ProgressControllerProxy : IProgressController
	{
		private readonly ProgressDialogController _controller;

		public ProgressControllerProxy(ProgressDialogController controller)
		{
			_controller = controller;
		}

		public double Minimum
		{
			get { return _controller.Minimum; }
			set { _controller.Minimum = value; }
		}

		public double Maximum
		{
			get { return _controller.Maximum; }
			set { _controller.Maximum = value; }
		}

		public bool IsCanceled => _controller.IsCanceled;

		public bool IsOpen => _controller.IsOpen;

		public event EventHandler Closed
		{
			add { _controller.Closed += value; }
			remove { _controller.Closed -= value; }
		}

		public event EventHandler Canceled
		{
			add { _controller.Canceled += value; }
			remove { _controller.Canceled -= value; }
		}

		public Task CloseAsync()
		{
			if (_controller.IsOpen)
				return _controller.CloseAsync();
			return Task.CompletedTask;
		}

		public void SetTitle(string title)
		{
			_controller.SetTitle(title);
		}

		public void SetMessage(string message)
		{
			_controller.SetMessage(message);
		}

		public void SetProgress(double progress)
		{
			_controller.SetProgress(progress);
		}

		public void SetCancelable(bool cancelable)
		{
			_controller.SetCancelable(cancelable);
		}

		public void SetIndeterminate()
		{
			_controller.SetIndeterminate();
		}
	}
}