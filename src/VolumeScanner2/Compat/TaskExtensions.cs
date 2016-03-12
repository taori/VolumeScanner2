using System.Threading.Tasks;

namespace VolumeScanner2.Compat
{
	public class TaskExtensions
	{
		public static Task CompletedTask => Task.FromResult(true);
	}
}