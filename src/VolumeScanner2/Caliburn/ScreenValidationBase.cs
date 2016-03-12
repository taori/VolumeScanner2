using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Amusoft.EventManagement;
using Caliburn.Micro;
using VolumeScanner2.Helpers;

namespace VolumeScanner2.Caliburn
{
	public abstract class ScreenValidationBase : ScreenValidationImpl
	{
		public override void OnPropertyValidating(object value, string propertyName, HashSet<string> validationResults)
		{
		}

		public override void OnPropertyValidated(string propertyName)
		{
		}
	}

	public abstract class ScreenValidationImpl : Screen, ISupportValidation, ISupportEditTracking, ICloneable
	{
		protected readonly IDictionary<string, HashSet<string>> Errors = new ConcurrentDictionary<string, HashSet<string>>();

		protected readonly IDictionary<string, CancellationTokenSource> ValidationCancellationTokenSources = new Dictionary<string, CancellationTokenSource>();

		public bool HasErrors => Errors.Keys.Any();

		[field: NonSerialized]
		private readonly WeakEvent<DataErrorsChangedEventArgs> _errorsChanged = new WeakEvent<DataErrorsChangedEventArgs>();

		event EventHandler<DataErrorsChangedEventArgs> INotifyDataErrorInfo.ErrorsChanged
		{
			add { _errorsChanged.Add(value); }
			remove { _errorsChanged.Remove(value); }
		}

		public void RaiseErrorsChanged(string propertyName)
		{
			_errorsChanged.Raise(this, new DataErrorsChangedEventArgs(propertyName));
		}

		public IEnumerable GetErrors(string propertyName)
		{
			return ValidationHelper.GetErrors(Errors, propertyName);
		}

		protected bool SetValue<T>(ref T backingField, T value, string propertyName, bool validate = true, bool notify = true)
		{
			if (object.Equals(backingField, value))
				return false;

			backingField = value;
			if (validate)
				ValidateProperty(value, propertyName);
			if (notify)
				NotifyOfPropertyChange(propertyName);
			if (IsRelevantForModification(propertyName))
				IsModified = true;

			return true;
		}

		protected virtual bool IsRelevantForModification(string propertyName)
		{
			return ValidationHelper.IsRelevantForModification(this.GetType(), propertyName);
		}

		public bool IsValid(bool raiseErrorsChanged = false)
		{
			var allErrors = ValidationHelper.ValidateInstance(this);

			if (raiseErrorsChanged)
			{
				foreach (var memberErrors in allErrors)
				{
					AddErrors(memberErrors.Key, memberErrors.Value);
					RaiseErrorsChanged(memberErrors.Key);
				}

				return allErrors.Keys.Count == 0;
			}
			else
			{
				return allErrors.Keys.Count == 0;
			}
		}
		protected bool ValidateProperty(object value, string propertyName)
		{
			var validationResults = ValidationHelper.ValidateProperty(this, value, propertyName);
			ISupportValidation validation = this;
			validation.OnPropertyValidating(value, propertyName, validationResults);
			if (validationResults.Count > 0)
			{
				AddErrors(propertyName, validationResults);
			}
			else
			{
				ClearErrors(propertyName);
			}

			RaiseErrorsChanged(propertyName);

			validation.OnPropertyValidated(propertyName);

			return !Errors.ContainsKey(propertyName) || Errors[propertyName].Count == 0;
		}

		public abstract void OnPropertyValidating(object value, string propertyName, HashSet<string> validationResults);

		public abstract void OnPropertyValidated(string propertyName);

		public void AddErrors(string propertyName, HashSet<string> validationResults)
		{
			ValidationHelper.AddErrors(Errors, propertyName, validationResults);
		}

		public void ClearErrors()
		{
			ValidationHelper.ClearErrors(Errors, this);
		}

		public void ClearErrors(string propertyName)
		{
			ValidationHelper.ClearErrors(Errors, this, propertyName);
		}

		private bool _isModified;
		public bool IsModified
		{
			get { return _isModified; }
			set
			{
				if (_isModified == value)
					return;
				_isModified = value;
				_isModifiedChanged.Raise(this, value);
				NotifyOfPropertyChange(nameof(IsModified));
			}
		}

		private readonly WeakEvent<bool> _isModifiedChanged = new WeakEvent<bool>();

		public event EventHandler<bool> IsModifiedChanged
		{
			add { _isModifiedChanged.Add(value); }
			remove { _isModifiedChanged.Remove(value); }
		}

		public void ClearEditState()
		{
			OnClearEditState();
			IsModified = false;
		}

		protected virtual void OnClearEditState()
		{
		}

		public object Clone()
		{
			return this.MemberwiseClone();
		}
	}
}