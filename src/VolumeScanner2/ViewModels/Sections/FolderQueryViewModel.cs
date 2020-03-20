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
			ScanPath = ApplicationTranslations.Message_NoPathSelected;

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
					
					DisplayName = dialog.SelectedPath.Length <= 30 ? dialog.SelectedPath : dialog.SelectedPath.Substring(0, 30) + " ...";

					using (var cts = new CancellationTokenSource())
					{
						var progressController = await this.ShowProgressAsync(ApplicationTranslations.Title_QueryingPath, ApplicationTranslations.Message_AnalysisRunning, cts);
						_ctsCurrentScan = cts;

						try
						{
							var vm = await Task.Run(() => BuildRootNode(ScanPath, cts.Token, progressController), cts.Token);

							Source = vm;
						}
						catch (OperationCanceledException e)
						{
							await progressController.CloseAsync();
							await this.ShowMessageAsync(GenericResources.Title_Information, ApplicationTranslations.Message_SearchAborted);
						}
						catch (Exception e)
						{
							await progressController.CloseAsync();
							await this.ShowMessageAsync(GenericResources.Title_Exception, e.Message);
						}
						finally
						{
							await progressController.CloseAsync();
							_ctsCurrentScan = null;
						}
					}
				}
				else
				{
					this.TryClose();
				}
			}
		}

		private FileInformationViewModel BuildRootNode(string scanPath, CancellationToken cancellationToken, IProgressController progress)
		{
			var filePaths = IoHelper.GetAllFilesRecursive(scanPath.AsMemory()).ToList();

			progress.Minimum = 0;
			progress.Maximum = filePaths.Count;
			progress.SetTitle(ApplicationTranslations.Title_AnalysisCacheIsBeingBuilt);

			FileQueryCache cache = new FileQueryCache();

			cache.Create(filePaths, progress, cancellationToken);

			var rootFileInfo = new ZlpFileInfo(scanPath);
			var rootNode = new FileInformationViewModel(rootFileInfo, cache);

			FileInformationExpander.ExpandFolder(rootNode, cache, 0);

			return rootNode;
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
	}

	internal class FileInformationExpander
	{
		public static void ExpandFolder(FileInformationViewModel refNode, FileQueryCache cache, int depth)
		{
			if (refNode.Type == FileInformationType.Directory)
			{
				refNode.Size = cache.GetPathSize(refNode.FullPath);

				if (depth >= 2)
					return;

				var children = new Collection<FileInformationViewModel>();
				var allFiles = ZlpIOHelper.GetFiles(refNode.FullPath);
				var allFolders = ZlpIOHelper.GetDirectories(refNode.FullPath);
				var nextDepth = ++depth;

				foreach (var info in allFiles)
				{
					var node = new FileInformationViewModel(info, cache);
					node.Size = cache.GetPathSize(info.FullName);
					children.Add(node);
				}

				foreach (var info in allFolders)
				{
					var node = new FileInformationViewModel(new ZlpFileInfo(info.FullName), cache);
					ExpandFolder(node, cache, nextDepth);
					children.Add(node);
				}

				refNode.Children.Clear();
				refNode.Children.AddRange(children.OrderByDescending(d => d.Size));
			}
		}
	}

	public class FileQueryCache
	{
		private class MemoryComparer : IEqualityComparer<ReadOnlyMemory<char>>
		{
			/// <inheritdoc />
			public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
			{
				var same = x.GetHashCode() == y.GetHashCode();
				return same;
			}

			/// <inheritdoc />
			public int GetHashCode(ReadOnlyMemory<char> obj)
			{
				return obj.GetHashCode();
			}
		}

		private static readonly MemoryComparer Comparer = new MemoryComparer();
		private readonly Dictionary<ReadOnlyMemory<char>, long> _sizesPerFile = new Dictionary<ReadOnlyMemory<char>, long>(Comparer);
		private readonly Dictionary<ReadOnlyMemory<char>, Collection<ReadOnlyMemory<char>>> _pathRegister = new Dictionary<ReadOnlyMemory<char>, Collection<ReadOnlyMemory<char>>>(Comparer);
		private readonly Dictionary<ReadOnlyMemory<char>, Collection<long>> _sizesPerFolder = new Dictionary<ReadOnlyMemory<char>, Collection<long>>(Comparer);
		private readonly Dictionary<ReadOnlyMemory<char>, ByteSize> _sizeOfItem = new Dictionary<ReadOnlyMemory<char>, ByteSize>(Comparer);
		private HashSet<ReadOnlyMemory<char>> _existingFiles = new HashSet<ReadOnlyMemory<char>>(Comparer);

		public ByteSize GetPathSize(string path)
		{
			ByteSize folderSize;
			return _sizeOfItem.TryGetValue(path.AsMemory(), out folderSize) ? folderSize : ByteSize.MinValue;
		}

		public void Create(List<ReadOnlyMemory<char>> filePaths, IProgressController progress, CancellationToken cancellationToken)
		{
			var fileCount = filePaths.Count;
			progress.Minimum = 0;
			progress.Maximum = fileCount;

			var displayDelay = TimeSpan.FromMilliseconds(250);

			progress.SetTitle(ApplicationTranslations.Message_CollectingFileInformation);
			_existingFiles = new HashSet<ReadOnlyMemory<char>>(filePaths.Where(d => IoHelper.FileExists(d)));
			
			progress.SetTitle(ApplicationTranslations.Message_QueryingFileSizes);
			IterateFiles(filePaths, progress, cancellationToken, fileCount, displayDelay, path =>
			{
				if (_sizesPerFile.ContainsKey(path))
					return;

				_sizesPerFile.Add(path, IoHelper.FileSize(path));
			});

			progress.SetTitle(ApplicationTranslations.Message_CreatingFolderRegister);
			IterateFiles(filePaths, progress, cancellationToken, fileCount, displayDelay, path =>
			{
				RegisterPathMembersAndSizes(path);
			});

			progress.SetTitle(ApplicationTranslations.Message_CalculatingFolderSizes);
			CalculateItemSizes(progress, cancellationToken, displayDelay);
		}

		private void CalculateItemSizes(IProgressController progress, CancellationToken cancellationToken, TimeSpan displayDelay)
		{
			var itemCount = _sizesPerFolder.Count;
			var current = 0;
			progress.Minimum = 0;
			progress.Maximum = itemCount;

			foreach (var pair in _sizesPerFolder)
			{
				cancellationToken.ThrowIfCancellationRequested();
				progress.SetMessage($"{ApplicationTranslations.Token_Folder}: {current} / {itemCount}", displayDelay);
				progress.SetProgress(current, displayDelay);
				_sizeOfItem.Add(pair.Key, new ByteSize(pair.Value.Sum()));
				current++;
			}
		}

		private void IterateFiles(List<ReadOnlyMemory<char>> filePaths, IProgressController progress, CancellationToken cancellationToken, int fileCount, TimeSpan displayDelay, Action<ReadOnlyMemory<char>> callback)
		{
			for (int index = 0; index < fileCount; index++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				progress.SetMessage($"{ApplicationTranslations.Token_File}: {index} / {fileCount}", displayDelay);
				progress.SetProgress(index, displayDelay);
				var filePath = filePaths[index];
				if (!_existingFiles.Contains(filePath))
					continue;

				callback(filePath);
			}
		}

		private void RegisterPathMembersAndSizes(ReadOnlyMemory<char> filePath)
		{
			var splittedDirectory = filePath.ToString().Split(Path.DirectorySeparatorChar);
			var currentFileSize = _sizesPerFile[filePath];

			for (int i = 0; i < splittedDirectory.Length; i++)
			{
				var mergedPath = string.Join(Path.DirectorySeparatorChar.ToString(), splittedDirectory.Take(i + 1)).AsMemory();
				Collection<ReadOnlyMemory<char>> paths;
				if (!_pathRegister.TryGetValue(mergedPath, out paths))
				{
					paths = new Collection<ReadOnlyMemory<char>>();
					_pathRegister.Add(mergedPath, paths);
				}
				paths.Add(filePath);

				Collection<long> folderMemberSizes;
				if (!_sizesPerFolder.TryGetValue(mergedPath, out folderMemberSizes))
				{
					folderMemberSizes = new Collection<long>();
					_sizesPerFolder.Add(mergedPath, folderMemberSizes);
				}

				folderMemberSizes.Add(currentFileSize);
			}
		}
	}
}