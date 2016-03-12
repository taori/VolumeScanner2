using System;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Caliburn.Micro;
using VolumeScanner.BusinessObjects;
using VolumeScanner2.Caliburn;
using VolumeScanner2.Extensions;
using VolumeScanner2.Framework;
using VolumeScanner2.Interfaces;
using VolumeScanner2.Resources;
using Action = System.Action;

namespace VolumeScanner2.ViewModels.Sections
{
	public class FolderQueryViewModel : ScreenValidationBase, IMainTabsControl
	{
		public int Order { get; set; }

		public FolderQueryViewModel()
		{
			IsScanning = false;
		}

		public void CancelScan()
		{
			_ctsCurrentScan?.Cancel();
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (!Execute.InDesignMode)
				ScanPathExecute();
		}

		private BindableCollection<string> _exceptionMessages = new BindableCollection<string>();

		public BindableCollection<string> ExceptionMessages
		{
			get { return _exceptionMessages; }
			set { SetValue(ref _exceptionMessages, value, nameof(ExceptionMessages)); }
		}

		private CancellationTokenSource _ctsCurrentScan;

		private async void ScanPathExecute()
		{
			ScanPath = "Kein Pfad ausgewählt.";

			using (var dialog = new FolderBrowserDialog())
			{
				ExceptionMessages.Clear();
				dialog.ShowNewFolderButton = false;
				dialog.SelectedPath = ApplicationSettings.Default.LastQueryPath;

				if (dialog.ShowDialog() == DialogResult.OK)
				{
					ScanPath = dialog.SelectedPath;

					if (ScanPath == null)
						return;

					ApplicationSettings.Default.LastQueryPath = ScanPath;
					ApplicationSettings.Default.Save();

					IsScanning = true;

					DisplayName = dialog.SelectedPath.Length <= 30 ? dialog.SelectedPath : dialog.SelectedPath.Substring(0, 30) + " ...";

					using (var cts = new CancellationTokenSource())
					{
						var progressController = await this.ShowProgressAsync("Ordnergrößen werden abgerufen.", "Pfad wird analysiert.", cts);
						_ctsCurrentScan = cts;

						try
						{
							var vm = await Task.Run(() => BuildNode(ScanPath, cts.Token, progressController));

							Source = vm;
						}
						catch (OperationCanceledException e)
						{
							await progressController.CloseAsync();
							await this.ShowMessageAsync(WindowResources.TitleInformation, "Suche abgebrochen");
						}
						catch (Exception e)
						{
							await progressController.CloseAsync();
							await this.ShowMessageAsync(WindowResources.TitleException, e.Message);
						}
						finally
						{
							await progressController.CloseAsync();
							_ctsCurrentScan = null;
						}
					}

					IsScanning = false;
				}
				else
				{
					this.TryClose();
				}
			}
		}

		private FileInformationViewModel BuildNode(string scanPath, CancellationToken cancellationToken, IProgressController progress)
		{
			var filePaths = Directory.GetFiles(scanPath, "*", SearchOption.AllDirectories);
			var directories = Directory.GetDirectories(scanPath, "*", SearchOption.AllDirectories);
			var fileCount = filePaths.Length;
			var directoryCount = directories.Length;
			var maxCount = fileCount + directoryCount;

			progress.Minimum = 1;
			progress.Maximum = maxCount;

			var rootFileInfo = new FileInfo(scanPath);
			var rootNode = new FileInformationViewModel(rootFileInfo);

			var currentProgress = 1;
			progress.SetTitle("Erstellen der Knoten");
			progress.SetMessage($"Anzahl Dateien: {maxCount}");
			AddNodesRecursive(rootNode, cancellationToken, () =>
			{
				progress.SetProgress(currentProgress++);
			});

			return rootNode;
		}

		private static bool failedSystemCheck = false;

		private void AddNodesRecursive(FileInformationViewModel node, CancellationToken cancellationToken, Action progress)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (node.Type == FileInformationType.Directory)
			{
				var directoryInfo = new DirectoryInfo(node.FileInfo.FullName);
				FileSystemInfo[] fileInfos = directoryInfo.GetFileSystemInfos();

				foreach (FileSystemInfo fileSystemInfo in fileInfos)
				{
					progress();

					if (failedSystemCheck || fileSystemInfo.Attributes.HasFlag(FileAttributes.System))
						continue;

					cancellationToken.ThrowIfCancellationRequested();
					try
					{
						var subNode = new FileInformationViewModel(new FileInfo(fileSystemInfo.FullName));
						node.Children.Add(subNode);
						AddNodesRecursive(subNode, cancellationToken, progress);

						var currentOrder = node.Children.ToArray();

						node.Children.Clear();
						node.Children.AddRange(currentOrder.OrderByDescending(d => d.Size));
					}
					catch (UnauthorizedAccessException e)
					{
						failedSystemCheck = true;
					}
					catch (PathTooLongException e)
					{
						ExceptionMessages.Add($"Pfad zu lange: {fileSystemInfo.FullName}");
					}
					catch (Exception e)
					{
						ExceptionMessages.Add(e.Message);
					}
				}

				node.Size = new ByteSize(node.Children.Sum(s => s.Size.Bytes));
			}
			else
			{
				node.Size = new ByteSize(node.FileInfo.Length);
			}
		}

		private string _scanPath;

		public string ScanPath
		{
			get { return _scanPath; }
			set { SetValue(ref _scanPath, value, nameof(ScanPath)); }
		}

		private FileInformationViewModel _source;

		public FileInformationViewModel Source
		{
			get { return _source; }
			set { SetValue(ref _source, value, nameof(Source)); }
		}

		private bool _isScanning;

		public bool IsScanning
		{
			get { return _isScanning; }
			set { SetValue(ref _isScanning, value, nameof(IsScanning)); }
		}
	}
}