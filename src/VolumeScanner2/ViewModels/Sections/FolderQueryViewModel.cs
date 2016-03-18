using System;
using System.Collections.Generic;
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
using VolumeScanner2.Helpers;
using VolumeScanner2.Interfaces;
using VolumeScanner2.Resources;
using ZetaLongPaths;
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

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);

			if (close)
			{
				this.ExceptionMessages = null;
				this.Source = null;
			}
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
							var vm = await Task.Run(() => BuildRootNode(ScanPath, cts.Token, progressController), cts.Token);

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
		
		private FileInformationViewModel BuildRootNode(string scanPath, CancellationToken cancellationToken, IProgressController progress)
		{
			var filePaths = IoHelper.GetAllFilesRecursive(scanPath).ToList();

			progress.Minimum = 0;
			progress.Maximum = filePaths.Count;
			progress.SetTitle("Dateigröße wird abgerufen.");
			
			var fileSizes = new Dictionary<string, long>();
			var fileCount = filePaths.Count;

			TimeSpan displayDelay = TimeSpan.FromMilliseconds(200);

			for (int index = 0; index < fileCount; index++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				progress.SetMessage($"Datei: {index} / {fileCount}", displayDelay);
				progress.SetProgress(index, displayDelay);
				var filePath = filePaths[index];
				if (!ZlpIOHelper.FileExists(filePath))
					continue;
					
				fileSizes.Add(filePath, new ZlpFileInfo(filePath).Length);
			}
			

			var rootFileInfo = new ZlpFileInfo(scanPath);
			var rootNode = new FileInformationViewModel(rootFileInfo);

			int currentProgress = 0;
			progress.SetTitle("Dateibaum wird aufgebaut.");
			progress.SetProgress(0);
			progress.Maximum = filePaths.Count;

			var progressDelay = TimeSpan.FromMilliseconds(500);
			AddNodesRecursive(rootNode, fileSizes, cancellationToken, () =>
			{
				progress.SetProgress(currentProgress++, progressDelay);
				progress.SetMessage($"Datei {currentProgress} / {filePaths.Count}", progressDelay);
			});

			return rootNode;
		}

		private void AddNodesRecursive(FileInformationViewModel currentNode, Dictionary<string, long> fileSizes, CancellationToken cancellationToken, Action progress)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (currentNode.Type == FileInformationType.Directory)
			{
				var directoryInfo =  new ZlpDirectoryInfo(currentNode.FileInfo.FullName);

				foreach (var fileInfo in directoryInfo.GetFiles())
				{
					progress();
					
					long size;
					size = fileSizes.TryGetValue(fileInfo.FullName, out size) ? size : 0;
					var localFile = new FileInformationViewModel(fileInfo);

					currentNode.Children.Add(localFile);
					localFile.Size = new ByteSize(size);
				}

				foreach (var fileSystemInfo in directoryInfo.GetDirectories())
				{
					cancellationToken.ThrowIfCancellationRequested();
					try
					{
						var subNode = new FileInformationViewModel(new ZlpFileInfo(fileSystemInfo.FullName));
						currentNode.Children.Add(subNode);
						AddNodesRecursive(subNode, fileSizes, cancellationToken, progress);

						var currentOrder = currentNode.Children.ToArray();

						currentNode.Children.Clear();
						currentNode.Children.AddRange(currentOrder.OrderByDescending(d => d.Size));
					}
					catch (UnauthorizedAccessException e)
					{
						ExceptionMessages.Add($"Zugriff verweigert: {fileSystemInfo.FullName}");
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

				currentNode.Size = new ByteSize(currentNode.Children.Sum(s => s.Size.Bytes));
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