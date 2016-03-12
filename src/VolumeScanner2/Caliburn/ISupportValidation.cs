using System.Collections.Generic;
using System.ComponentModel;
using Caliburn.Micro;

namespace VolumeScanner2.Caliburn
{
	public interface ISupportValidation : INotifyPropertyChangedEx, INotifyDataErrorInfo
	{
		void RaiseErrorsChanged(string propertyName);
		void OnPropertyValidating(object value, string propertyName, HashSet<string> validationResults);
		void OnPropertyValidated(string propertyName);
	}
}