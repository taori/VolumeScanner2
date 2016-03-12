using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using AutoMapper.Internal;
using Caliburn.Micro;
using VolumeScanner2.Caliburn;
using VolumeScanner2.Extensions;
using VolumeScanner2.Framework.Attributes;

namespace VolumeScanner2.Helpers
{
	public static class ValidationHelper
	{
		public static HashSet<string> ValidateProperty(object propertyOwner, object value, string propertyName)
		{
			var results = new List<ValidationResult>();

			var validationContext = new ValidationContext(propertyOwner);
			validationContext.MemberName = propertyName;
			Validator.TryValidateProperty(value, validationContext, results);

			var validationInstance = propertyOwner as ISupportValidation;
			var instanceType = propertyOwner.GetType();
			var errorList = new HashSet<string>();
			errorList.AddRange(results.Select(s => s.ErrorMessage));

			if (validationInstance != null)
			{
				var validationMap = GetValidationMap(instanceType);
				if (validationMap.Contains(propertyName))
				{
					validationInstance.OnPropertyValidating(instanceType.GetProperty(propertyName).GetValue(propertyOwner), propertyName, errorList);
				}
			}

			return errorList;
		}

		public static Dictionary<string, HashSet<string>> ValidateInstance(object instance)
		{
			var errorContainer = new Dictionary<string, HashSet<string>>();

			var validationContext = new ValidationContext(instance);
			var validationResults = new List<ValidationResult>();
			Validator.TryValidateObject(instance, validationContext, validationResults);

			var validationInstance = instance as ISupportValidation;
			if (validationInstance != null)
			{
				var instanceType = instance.GetType();
				var validationMap = GetValidationMap(instanceType);

				foreach (var propertyName in validationMap)
				{
					var localErrors = new HashSet<string>();
					validationInstance.OnPropertyValidating(instanceType.GetProperty(propertyName).GetValue(instance), propertyName, localErrors);
					if (localErrors.Count > 0)
					{
						HashSet<string> memberErrorSection;
						memberErrorSection = GetMemberErrorSection(errorContainer, propertyName);
						memberErrorSection.AddRange(localErrors);
					}
				}
			}

			foreach (var validation in validationResults)
			{
				foreach (var memberName in validation.MemberNames)
				{
					HashSet<string> memberErrorSection;
					memberErrorSection = GetMemberErrorSection(errorContainer, memberName);

					memberErrorSection.Add(validation.ErrorMessage);
				}
			}

			return errorContainer;
		}

		private static HashSet<string> GetMemberErrorSection(Dictionary<string, HashSet<string>> errorContainer, string memberName)
		{
			HashSet<string> memberErrorSection;
			if (!errorContainer.TryGetValue(memberName, out memberErrorSection))
			{
				memberErrorSection = new HashSet<string>();
				errorContainer.Add(memberName, memberErrorSection);
			}

			return memberErrorSection;
		}

		private static readonly Dictionary<Type, HashSet<string>> ValidationMaps = new Dictionary<Type, HashSet<string>>();
		private static HashSet<string> GetValidationMap(Type type)
		{
			HashSet<string> set;
			if (!ValidationMaps.TryGetValue(type, out set))
			{
				set = new HashSet<string>();
				foreach (var info in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty))
				{
					set.Add(info.Name);
				}
				ValidationMaps.Add(type, set);
			}

			return set;
		}

		internal static void AddErrors(System.Collections.Generic.IDictionary<string, HashSet<string>> errors, string propertyName, HashSet<string> validationResults)
		{
			if (errors.ContainsKey(propertyName))
			{
				errors[propertyName] = validationResults;
			}
			else
			{
				errors.Add(propertyName, validationResults);
			}
		}

		internal static void ClearErrors(System.Collections.Generic.IDictionary<string, HashSet<string>> errors, ISupportValidation isv)
		{
			var keys = errors.Keys.ToList();

			errors.Clear();

			Execute.OnUIThread(() =>
			{
				foreach (var key in keys)
				{
					isv.RaiseErrorsChanged(key);
				}
			});
		}

		internal static void ClearErrors(System.Collections.Generic.IDictionary<string, HashSet<string>> errors, ISupportValidation isv, string propertyName)
		{
			errors.Remove(propertyName);

			Execute.OnUIThread(() =>
			{
				isv.RaiseErrorsChanged(propertyName);
			});
		}

		internal static IEnumerable GetErrors(System.Collections.Generic.IDictionary<string, HashSet<string>> errors, string propertyName)
		{
			if (propertyName == null)
				yield break;

			if (!errors.ContainsKey(propertyName))
				yield break;

			foreach (var result in errors[propertyName])
			{
				yield return result;
			}
		}

		private static readonly Dictionary<Type, HashSet<string>> ModificationPropertyDictionary = new Dictionary<Type, HashSet<string>>();

		internal static bool IsRelevantForModification(Type type, string propertyName)
		{
			HashSet<string> typeMap;
			if (!ModificationPropertyDictionary.TryGetValue(type, out typeMap))
			{
				typeMap = new HashSet<string>();

				foreach (var propertyInfo in type.GetProperties())
				{
					if (propertyInfo.IsDefined(typeof(IsModifiedTrackingAttribute), true))
					{
						typeMap.Add(propertyInfo.Name);
					}
				}

				ModificationPropertyDictionary.Add(type, typeMap);
			}

			return typeMap.Contains(propertyName);
		}
	}
}