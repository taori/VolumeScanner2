using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Caliburn.Micro;
using FontAwesome.WPF;
using VolumeScanner2.Caliburn;
using VolumeScanner2.Model;
using VolumeScanner2.ViewModels.Sections;
using ZetaLongPaths;

namespace VolumeScanner2.ViewModels
{
	public enum FileInformationType
	{
		Directory,
		File
	}

	public class FileInformationTypeToIconConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType != typeof (FontAwesomeIcon))
				return FontAwesomeIcon.QuestionCircle;
			if (value.GetType() != typeof (FileInformationType))
				return FontAwesomeIcon.QuestionCircle;

			var c = (FileInformationType)value;

			switch (c)
			{
				case FileInformationType.Directory:
					return FontAwesomeIcon.Folder;
				case FileInformationType.File:
					return FontAwesomeIcon.File;
				default:
					return FontAwesomeIcon.QuestionCircle;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	[DebuggerDisplay("{Name} {Size}")]
	public class FileInformationViewModel : PropertyChangedValidationBase
	{
		public readonly FileQueryCache Cache;

		private readonly ZlpFileInfo _fileInfo;

		public string FormattedSize => $"{Size.MegaBytes.ToString("00000.00")} MB";

		public ByteSize Size { get; set; }

		public string FullPath => _fileInfo.FullName;

		private BindableCollection<FileInformationViewModel> _children = new BindableCollection<FileInformationViewModel>();

		public BindableCollection<FileInformationViewModel> Children
		{
			get { return _children; }
			set { SetValue(ref _children, value, nameof(Children)); }
		}

		public FileInformationViewModel() {}

		public FileInformationViewModel(ZlpFileInfo fileInformation, FileQueryCache cache)
		{
			Cache = cache;
			_fileInfo = fileInformation;
			Type = File.GetAttributes(fileInformation.FullName).HasFlag(FileAttributes.Directory) ? FileInformationType.Directory : FileInformationType.File;
			Name = Path.GetFileName(_fileInfo.FullName);
		}

		private FileInformationType _type;

		public FileInformationType Type
		{
			get { return _type; }
			set { SetValue(ref _type, value, nameof(Type), false); }
		}

		private string _name;

		public string Name
		{
			get { return _name; }
			set { SetValue(ref _name, value, nameof(Name), false); }
		}

		private bool _isExpanded;

		public bool IsExpanded
		{
			get { return _isExpanded; }
			set
			{
				LoadIfRequired(value);
				SetValue(ref _isExpanded, value, nameof(IsExpanded), validate:false);
			}
		}

		private void LoadIfRequired(bool expand)
		{
			if (IsLoaded || !expand)
				return;

			FileInformationExpander.ExpandFolder(this, Cache, 0);
			IsLoaded = true;
		}

		public bool IsLoaded { get; set; }

		public void OpenInExplorer()
		{
			System.Diagnostics.Process.Start("explorer", $"/e,/select,{_fileInfo.FullName}");
		}

		public void ExpandRecursive()
		{
			if (this.Type != FileInformationType.Directory)
				return;

			IsExpanded = true;
			foreach (var child in Children)
			{
				Task.Run(() => child.ExpandRecursive());
			}
		}

		public void CollapseRecursive()
		{
			if (this.Type != FileInformationType.Directory)
				return;

			IsExpanded = false;
			foreach (var child in Children)
			{
				Task.Run(() => child.CollapseRecursive());
			}
		}
	}
}