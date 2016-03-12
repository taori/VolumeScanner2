using System;

namespace VolumeScanner2.Caliburn
{
	public interface ISupportEditTracking
	{
		bool IsModified { get; }
		event EventHandler<bool> IsModifiedChanged;
		void ClearEditState();
	}
}