using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using VolumeScanner2.Interfaces;
using VolumeScanner2.Resources;
using VolumeScanner2.ViewModels.Sections;

namespace VolumeScanner2.ViewModels.Windows
{
	public class ShellViewModel : Conductor<IMainTabsControl>.Collection.OneActive, IShell
	{
		public ShellViewModel()
		{
			if (Execute.InDesignMode)
			{
				NewFolderQuery();
				NewFolderQuery();
			}
		}

		protected override async void OnInitialize()
		{
			base.OnInitialize();

			await Task.Delay(2000);

			NewFolderQuery();
		}

		public override string DisplayName
		{
			get { return "Volumescanner 2"; }
			set { }
		}

		public void NewFolderQuery()
		{
			var item = new FolderQueryViewModel();
			item.DisplayName = ApplicationTranslations.DialogNewQuery;
			this.Items.Add(item);

			ActivateItem(item);
		}

		public void CloseQuery(FolderQueryViewModel item)
		{
			item.TryClose();
		}
	}
}