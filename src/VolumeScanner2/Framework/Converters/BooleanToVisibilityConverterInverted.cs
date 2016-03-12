using System.Windows;

namespace VolumeScanner2.Framework.Converters
{
	public class BooleanToVisibilityConverterInverted : BooleanConverter<Visibility>
	{
		public BooleanToVisibilityConverterInverted() : base(Visibility.Collapsed, Visibility.Visible)
		{
		}
	}
}