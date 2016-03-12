using System;
using System.IO;
using System.Threading.Tasks;

namespace VolumeScanner2.Helpers
{
	public static class IoHelper
	{
		private static readonly string RoamingRootName = "ImmoCrawler";

		public enum SpecialFolder
		{
			None,
			Environment,
			Exports
		}

		public static string GetRoamingRoot(SpecialFolder subFolder = SpecialFolder.None)
		{
			var sf = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			if (subFolder == SpecialFolder.None)
				return Path.Combine(sf, RoamingRootName);
			return Path.Combine(sf, RoamingRootName, subFolder.ToString());
		}

		public static string GetFileName(SpecialFolder folder, string fileName)
		{
			return Path.Combine(GetRoamingRoot(folder), fileName);
		}

		public static Task EnsureDirectoryAsync(string path)
		{
			return Task.Run(() =>
			{
				var dir = Path.GetDirectoryName(path);
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
			});
		}
	}
}
