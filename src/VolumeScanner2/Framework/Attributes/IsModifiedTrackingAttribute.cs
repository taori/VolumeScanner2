using System;

namespace VolumeScanner2.Framework.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class IsModifiedTrackingAttribute : Attribute
	{
	}
}