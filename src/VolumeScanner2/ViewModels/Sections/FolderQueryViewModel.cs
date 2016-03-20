using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Security.Policy;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Caliburn.Micro;
using VolumeScanner2.Caliburn;
using VolumeScanner2.Extensions;
using VolumeScanner2.Framework;
using VolumeScanner2.Helpers;
using VolumeScanner2.Interfaces;
using VolumeScanner2.Model;
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

			FileQueryCache cache = new FileQueryCache();

			cache.Create(filePaths, progress, cancellationToken);

			var rootFileInfo = new ZlpFileInfo(scanPath);
			var rootNode = new FileInformationViewModel(rootFileInfo, cache);

			FileInformationExpander.Expand(rootNode);

			return rootNode;
		}

		private void AddNodesRecursive(FileInformationViewModel currentNode, FileQueryCache cache, CancellationToken cancellationToken, Action progress)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var children = new Collection<FileInformationViewModel>();

			if (currentNode.Type == FileInformationType.Directory)
			{
				var directoryInfo =  new ZlpDirectoryInfo(currentNode.FullPath);

				foreach (var fileInfo in directoryInfo.GetFiles())
				{
					progress();
					
					long size;
					size = cache.FileSizes.TryGetValue(fileInfo.FullName, out size) ? size : 0;
					var localFile = new FileInformationViewModel(fileInfo, cache);

					children.Add(localFile);
					localFile.Size = new ByteSize(size);
				}

				foreach (var fileSystemInfo in directoryInfo.GetDirectories())
				{
					cancellationToken.ThrowIfCancellationRequested();
					try
					{
						var subNode = new FileInformationViewModel(new ZlpFileInfo(fileSystemInfo.FullName), cache);
						children.Add(subNode);
						AddNodesRecursive(subNode, cache, cancellationToken, progress);
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

				currentNode.Children.AddRange(currentNode.Children.OrderByDescending(d => d.Size));
				currentNode.Size = new ByteSize(children.Sum(s => s.Size.Bytes));
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

	internal class FileInformationExpander
	{
		public static void Expand(FileInformationViewModel refNode)
		{
			Expand(refNode, 0);
		}

		private static void Expand(FileInformationViewModel refNode, int depth)
		{
			// easiest way to make sure we always create enough nodes to provide expandability on childnodes
			if (depth >= 2)
				return;

		}
	}

	public class FileQueryCache
	{
		public readonly Dictionary<string, long> FileSizes = new Dictionary<string, long>();
		public readonly Dictionary<string, ZlpFileInfo> FileInformations = new Dictionary<string, ZlpFileInfo>();
		public readonly Dictionary<string, List<string>> PathRegister = new Dictionary<string, List<string>>();
		
		public void Create(List<string> filePaths, IProgressController progress, CancellationToken cancellationToken)
		{
			var fileCount = filePaths.Count;
			progress.Minimum = 0;
			progress.Maximum = fileCount;

			TimeSpan displayDelay = TimeSpan.FromMilliseconds(100);

			progress.SetTitle("1 / 3 - Dateien werden abgefragt.");
			IterateFiles(filePaths, progress, cancellationToken, fileCount, displayDelay, path =>
			{
				FileInformations.Add(path, new ZlpFileInfo(path));
			});

			progress.SetTitle("2 / 3 - Dateigrößen werden abgefragt.");
			IterateFiles(filePaths, progress, cancellationToken, fileCount, displayDelay, path =>
			{
				FileSizes.Add(path, FileInformations[path].Length);
			});

			progress.SetTitle("3 / 3 - Ordnergrößen werden berechnet.");
			IterateFiles(filePaths, progress, cancellationToken, fileCount, displayDelay, path =>
			{
				RegisterPaths(path);
			});
		}

		private void IterateFiles(List<string> filePaths, IProgressController progress, CancellationToken cancellationToken, int fileCount, TimeSpan displayDelay, Action<string> callback)
		{
			for (int index = 0; index < fileCount; index++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				progress.SetMessage($"Datei: {index} / {fileCount}", displayDelay);
				progress.SetProgress(index, displayDelay);
				var filePath = filePaths[index];
				if (!ZlpIOHelper.FileExists(filePath))
					continue;

				callback(filePath);
			}
		}

		private void RegisterPaths(string filePath)
		{
			var splittedDirectory = filePath.Split(Path.DirectorySeparatorChar);

			for (int i = 0; i < splittedDirectory.Length; i++)
			{
				var mergedPath = string.Join(Path.DirectorySeparatorChar.ToString(), splittedDirectory.Take(i + 1));
				List<string> paths;
				if (!PathRegister.TryGetValue(mergedPath, out paths))
				{
					paths = new List<string>();
					PathRegister.Add(mergedPath, paths);
				}

				paths.Add(filePath);
			}
		}
	}
}