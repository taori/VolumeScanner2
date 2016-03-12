using VolumeScanner2.Mef;

namespace VolumeScanner2.Interfaces
{
	[PartCreationPolicy(true)]
	[InheritedExport(typeof(IShell))]
	public interface IShell
	{

	}
}