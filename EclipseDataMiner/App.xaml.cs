using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EclipseDataMiner
{
	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// UIスレッドの未処理例外ハンドラ
			DispatcherUnhandledException += (s, args) =>
			{
				string msg = $"Unhandled UI Exception:\n\n{FormatException(args.Exception)}";
				MessageBox.Show(msg, "EclipseDataMiner Error", MessageBoxButton.OK, MessageBoxImage.Error);
				args.Handled = true;
			};

			// バックグラウンド・全ドメインの未処理例外ハンドラ
			AppDomain.CurrentDomain.UnhandledException += (s, args) =>
			{
				if (args.ExceptionObject is Exception ex)
				{
					string msg = $"Critical Application Error:\n\n{FormatException(ex)}";
					MessageBox.Show(msg, "EclipseDataMiner Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			};
		}

		private static string FormatException(Exception ex)
		{
			var sb = new System.Text.StringBuilder();
			var curr = ex;
			while (curr != null)
			{
				sb.AppendLine($"[{curr.GetType().Name}] {curr.Message}");
				if (!string.IsNullOrEmpty(curr.StackTrace))
				{
					sb.AppendLine(curr.StackTrace);
				}
				curr = curr.InnerException;
				if (curr != null) sb.AppendLine("--- Inner Exception ---");
			}
			return sb.ToString();
		}
	}
}
