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

		public async Task CloseAsync()
		{
			if (_controller.IsOpen)
				await _controller.CloseAsync();
		}

		public void SetTitle(string title)
		{
			_controller.SetTitle(title);
		}

		private DateTime _lastMessage = DateTime.MinValue;
		public void SetMessage(string message, TimeSpan minDelay = default(TimeSpan))
		{
			if (_lastMessage + minDelay < DateTime.Now)
			{
				_lastMessage = DateTime.Now;
				_controller.SetMessage(message);
			}
		}

		private DateTime _lastProgress = DateTime.MinValue;
		public void SetProgress(double progress, TimeSpan minDelay = default(TimeSpan))
		{
			if (_lastProgress + minDelay < DateTime.Now)
			{
				_lastProgress = DateTime.Now;
				_controller.SetProgress(progress);
			}
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