namespace VolumeScanner2.Framework.Converters
{
	public class BooleanConverterInverted : BooleanConverter<bool>
	{
		public BooleanConverterInverted() : base(false, true)
		{
		}
	}
}