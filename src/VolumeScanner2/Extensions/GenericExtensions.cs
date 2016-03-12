using System.Collections;
using System.Collections.Generic;

namespace VolumeScanner2.Extensions
{
	public static class GenericExtensions
	{
		public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
		{
			lock (source)
			{
				foreach (var item in items)
				{
					source.Add(item);
				}
			}
		}

		public static bool In<T>(this T source, params T[] options)
		{
			var comparer = EqualityComparer<T>.Default;
			foreach (var option in options)
			{
				if (comparer.Equals(option, source))
				{
					return true;
				}
			}

			return false;
		}
	}
}