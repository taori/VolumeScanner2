using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Caliburn.Micro;
using FontAwesome.WPF;
using VolumeScanner.BusinessObjects;
using VolumeScanner2.Caliburn;
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
		public ZlpFileInfo FileInfo { get; }

		public string FormattedSize => $"{Size.MegaBytes.ToString("00000.00")} MB";

		public ByteSize Size { get; set; }

		private BindableCollection<FileInformationViewModel> _children = new BindableCollection<FileInformationViewModel>();

		public BindableCollection<FileInformationViewModel> Children
		{
			get { return _children; }
			set { SetValue(ref _children, value, nameof(Children)); }
		}

		public FileInformationViewModel() {}

		public FileInformationViewModel(ZlpFileInfo fi)
		{
			FileInfo = fi;
			Type = File.GetAttributes(fi.FullName).HasFlag(FileAttributes.Directory) ? FileInformationType.Directory : FileInformationType.File;
			Name = Path.GetFileName(FileInfo.FullName);
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
			set { SetValue(ref _isExpanded, value, nameof(IsExpanded), validate:false); }
		}

		public void OpenInExplorer()
		{
			System.Diagnostics.Process.Start("explorer", $"/e,/select,{FileInfo.FullName}");
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