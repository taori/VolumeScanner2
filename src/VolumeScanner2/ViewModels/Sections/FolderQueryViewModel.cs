﻿using System;
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
			var filePaths = IoHelper.GetAllFilesRecursive(scanPath).ToList();

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
		public readonly Dictionary<string, long> SizesPerFile = new Dictionary<string, long>();
		public readonly Dictionary<string, ZlpFileInfo> FileInformations = new Dictionary<string, ZlpFileInfo>();
		public readonly Dictionary<string, Collection<string>> PathRegister = new Dictionary<string, Collection<string>>();
		public readonly Dictionary<string, Collection<long>> SizesPerFolder = new Dictionary<string, Collection<long>>();
		public readonly Dictionary<string, ByteSize> SizeOfItem = new Dictionary<string, ByteSize>();

		public ByteSize GetPathSize(string path)
		{
			ByteSize folderSize;
			return SizeOfItem.TryGetValue(path, out folderSize) ? folderSize : ByteSize.MinValue;
		}

		public void Create(List<string> filePaths, IProgressController progress, CancellationToken cancellationToken)
		{
			var fileCount = filePaths.Count;
			progress.Minimum = 0;
			progress.Maximum = fileCount;

			TimeSpan displayDelay = TimeSpan.FromMilliseconds(250);

			progress.SetTitle(ApplicationTranslations.Message_CollectingFileInformation);
			IterateFiles(filePaths, progress, cancellationToken, fileCount, displayDelay, path =>
			{
				FileInformations.Add(path, new ZlpFileInfo(path));
			});

			progress.SetTitle(ApplicationTranslations.Message_QueryingFileSizes);
			IterateFiles(filePaths, progress, cancellationToken, fileCount, displayDelay, path =>
			{
				SizesPerFile.Add(path, FileInformations[path].Length);
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
			var itemCount = SizesPerFolder.Count;
			var current = 0;
			progress.Minimum = 0;
			progress.Maximum = itemCount;

			foreach (var pair in SizesPerFolder)
			{
				cancellationToken.ThrowIfCancellationRequested();
				progress.SetMessage($"{ApplicationTranslations.Token_Folder}: {current} / {itemCount}", displayDelay);
				progress.SetProgress(current, displayDelay);
				SizeOfItem.Add(pair.Key, new ByteSize(pair.Value.Sum()));
				current++;
			}
		}

		private void IterateFiles(List<string> filePaths, IProgressController progress, CancellationToken cancellationToken, int fileCount, TimeSpan displayDelay, Action<string> callback)
		{
			for (int index = 0; index < fileCount; index++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				progress.SetMessage($"{ApplicationTranslations.Token_File}: {index} / {fileCount}", displayDelay);
				progress.SetProgress(index, displayDelay);
				var filePath = filePaths[index];
				if (!ZlpIOHelper.FileExists(filePath))
					continue;

				callback(filePath);
			}
		}

		private void RegisterPathMembersAndSizes(string filePath)
		{
			var splittedDirectory = filePath.Split(Path.DirectorySeparatorChar);
			var currentFileSize = SizesPerFile[filePath];

			for (int i = 0; i < splittedDirectory.Length; i++)
			{
				var mergedPath = string.Join(Path.DirectorySeparatorChar.ToString(), splittedDirectory.Take(i + 1));
				Collection<string> paths;
				if (!PathRegister.TryGetValue(mergedPath, out paths))
				{
					paths = new Collection<string>();
					PathRegister.Add(mergedPath, paths);
				}
				paths.Add(filePath);

				Collection<long> folderMemberSizes;
				if (!SizesPerFolder.TryGetValue(mergedPath, out folderMemberSizes))
				{
					folderMemberSizes = new Collection<long>();
					SizesPerFolder.Add(mergedPath, folderMemberSizes);
				}

				folderMemberSizes.Add(currentFileSize);
			}
		}
	}
}