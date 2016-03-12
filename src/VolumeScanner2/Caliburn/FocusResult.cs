namespace VolumeScanner2.Caliburn
{
	//	public abstract class Result : IResult
	//	{
	//		public static List<T> GetChildrenByType<T>(UIElement element,
	//												   Func<T, bool> condition) where T : UIElement
	//		{
	//			List<T> results = new List<T>();
	//			GetChildrenByType<T>(element, condition, results);
	//			return results;
	//		}
	//
	//		private static void GetChildrenByType<T>(UIElement element, Func<T, bool> condition, List<T> results) where T : UIElement
	//		{
	//			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
	//			{
	//				UIElement child = VisualTreeHelper.GetChild(element, i) as UIElement;
	//				if (child != null)
	//				{
	//					T t = child as T;
	//					if (t != null)
	//					{
	//						if (condition == null)
	//							results.Add(t);
	//						else if (condition(t))
	//							results.Add(t);
	//					}
	//					GetChildrenByType<T>(child, condition, results);
	//				}
	//			}
	//		}
	//
	//		public abstract void Execute(CoroutineExecutionContext context);
	//
	//		public event EventHandler<ResultCompletionEventArgs> Completed;
	//
	//		public void RaiseCompleted()
	//		{
	//			var handler = Completed;
	//			if (handler != null)
	//				handler(this, new ResultCompletionEventArgs());
	//		}
	//	}
	//
	//	public class FocusResult : Result
	//	{
	//		private readonly string _controlToFocus;
	//
	//		public FocusResult(string controlToFocus)
	//		{
	//			_controlToFocus = controlToFocus;
	//		}
	//
	//		public override void Execute(CoroutineExecutionContext context)
	//		{
	//			var view = context.View as UserControl;
	//
	//
	//			// add support for further controls here
	//			var editableControls = GetChildrenByType<Control>(view, c => c is CheckBox ||
	//														  c is TextBox ||
	//														  c is Button);
	//
	//			var control = editableControls.SingleOrDefault(c => c.Name == _controlToFocus);
	//
	//			if (control != null)
	//			{
	//				Caliburn.Micro.Execute.OnUIThread(() =>
	//				{
	//					control.Focus();
	//
	//					var textBox = control as TextBox;
	//					if (textBox != null)
	//						textBox.Select(textBox.Text.Length, 0);
	//				});
	//			}
	//
	//
	//			RaiseCompleted();
	//		}
	//	}
}