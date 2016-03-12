using System.ComponentModel.DataAnnotations;

namespace VolumeScanner2.Framework
{
	public class ContainsStringAttribute : ValidationAttribute
	{
		public ContainsStringAttribute(string needle)
		{
			Needle = needle;
		}

		public string Needle { get; set; }

		public override string FormatErrorMessage(string name)
		{
			return $"Das Feld \"{name}\" muss den Text \"{Needle}\" enthalten.";
		}

		public override bool IsValid(object value)
		{
			var s = value as string;
			if (s == null)
				return false;

			return s.Contains(Needle);
		}
	}
}