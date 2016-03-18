using System;
using System.Threading.Tasks;

namespace VolumeScanner2.Interfaces
{
	public interface IProgressController
	{
		double Minimum { get; set; }
		double Maximum { get; set; }
		bool IsCanceled { get; }
		bool IsOpen { get; }
		event EventHandler Closed;
		event EventHandler Canceled;
		Task CloseAsync();
		void SetTitle(string title);
		void SetMessage(string message, TimeSpan minDelay = default(TimeSpan));
		void SetProgress(double progress, TimeSpan minDelay = default(TimeSpan));
		void SetCancelable(bool cancelable);
		void SetIndeterminate();
	}
}