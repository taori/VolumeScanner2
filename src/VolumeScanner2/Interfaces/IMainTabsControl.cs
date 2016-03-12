using Caliburn.Micro;
using VolumeScanner2.Mef;

namespace VolumeScanner2.Interfaces
{
	[PartCreationPolicy(true)]
	[InheritedExport(typeof(IMainTabsControl))]
	public interface IMainTabsControl : IScreen
	{
		int Order { get; }
	}
}