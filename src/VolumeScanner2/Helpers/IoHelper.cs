using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public static IEnumerable<ReadOnlyMemory<char>> GetAllDirectoriesRecursive(ReadOnlyMemory<char> scanPath)
		{
			ReadOnlyMemory<char>[] directories;
			try
			{
				directories = SafeGetDirectories(scanPath);
			}
			catch (UnauthorizedAccessException e)
			{
				yield break;
			}

			foreach (var item in directories)
			{
				yield return item;
				foreach (var recursionItem in GetAllDirectoriesRecursive(item))
				{
					yield return recursionItem;
				}
			}
		}

		public static IEnumerable<ReadOnlyMemory<char>> GetAllFilesRecursive(ReadOnlyMemory<char> scanPath)
		{
			ReadOnlyMemory<char>[] directories;
			try
			{
				directories = SafeGetDirectories(scanPath);
			}
			catch (UnauthorizedAccessException e)
			{
				yield break;
			}

			foreach (var item in directories)
			{
				foreach (var recursionItem in GetAllFilesRecursive(item))
				{
					yield return recursionItem;
				}
			}

			foreach (var item in SafeGetFiles(scanPath))
			{
				yield return item;
			}
		}

		private static ReadOnlyMemory<char>[] SafeGetFiles(ReadOnlyMemory<char> scanPath)
		{
			return ZetaLongPaths.ZlpIOHelper.GetFiles(scanPath.ToString(), SearchOption.TopDirectoryOnly).Select(s => s.FullName.AsMemory()).ToArray();
		}

		private static ReadOnlyMemory<char>[] SafeGetDirectories(ReadOnlyMemory<char> scanPath)
		{
			var directories = ZetaLongPaths.ZlpIOHelper.GetDirectories(scanPath.ToString()).Select(s => s.FullName.AsMemory()).ToArray();

			return directories;
		}
	}
}
