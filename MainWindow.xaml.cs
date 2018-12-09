using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using emanual.Wpf.Utility;

namespace R2KtoMVConverter
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs evt)
        {
            if (evt.Data.GetDataPresent(DataFormats.FileDrop)) {
                evt.Effects = DragDropEffects.Copy;
            } else {
                evt.Effects = DragDropEffects.None;
            }
            evt.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs evt)
        {
            string[] files = evt.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null) {
                return;
            }

            // LINQで抽出
            var values = from x in files
                         where System.IO.File.Exists(x)
                         orderby x
                         select x;
            if (values.Count() == 0) {
                return;
            }

            foreach (var file in values) {
                ConvertJob convertJob = new ConvertJob(
                    new ConvertRequest(new string[] { file }, 
                    Properties.Settings.Default.OutputDirectory));
                Task task = new Task(() => {
                    try {
                        convertJob.Execute();
                    }
                    catch (Exception e) {
                        Dispatcher.BeginInvoke(new Action(() => {
                            ShowErrorMessageBox(e.Message);
                        }));
                    }
                });
                task.ConfigureAwait(false);
                task.Start();
            }


        }

        private void ShowErrorMessageBox(string msg)
        {
            MessageBoxEx.Show(this, msg, Properties.Resources.MsgError);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = Properties.Resources.MsgSelectFolder;
            if (System.IO.Directory.Exists(Properties.Settings.Default.OutputDirectory)) {
                dialog.SelectedPath = Properties.Settings.Default.OutputDirectory;
            } else { 
                dialog.SelectedPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            } 

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                Properties.Settings.Default.OutputDirectory = dialog.SelectedPath;
            }
            labelOutputDirectory.Content = Properties.Settings.Default.OutputDirectory;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
                labelOutputDirectory.Content = Properties.Settings.Default.OutputDirectory;
        }
    }
}
