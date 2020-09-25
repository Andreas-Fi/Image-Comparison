using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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


using CompareFunctions;

namespace ImageComparionGUI
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool deleteMode = false;
        public ObservableCollection<Files> MatchingFiles { get; set; }

        public void MainFunction()
        {
            IEnumerable<string> files;
            int count = 0;
            const int leftoverMemoryMB = 9000;

            files = Directory.EnumerateFiles(Compare.workingDirectory);

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ProgressBar.Text = "Progress: 0/" + files.Count();
            }, System.Windows.Threading.DispatcherPriority.Background);

            //int memoryUsage;
            List<Match> matches = new List<Match>();
            count = 0;
            for (int i = 1; i < files.Count(); i++)
            {
                int j = files.Count() - i;
                count += j;
            }

            System.Diagnostics.Process proc = System.Diagnostics.Process.GetCurrentProcess();

            int memsize = 0; // memsize in MB
            System.Diagnostics.PerformanceCounter PC = new System.Diagnostics.PerformanceCounter
            {
                CategoryName = "Process",
                CounterName = "Working Set - Private",
                InstanceName = proc.ProcessName
            };
            memsize = Convert.ToInt32(PC.NextValue()) / (int)(1048576);

            DateTime dt1 = DateTime.Now;
            Task<List<Match>>[] taskArray = new Task<List<Match>>[files.Count()];
            for (int i = 0; i < taskArray.Count(); i++)
            {
                ulong availableMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory / 1048576; // memsize in MB
                while (availableMemory <= leftoverMemoryMB)
                {
                    Thread.Sleep(100);
                    availableMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory / 1048576; // memsize in MB
                }

                var newArray = files.ToList().GetRange(i, files.Count() - i);
                taskArray[i] = Task<List<Match>>.Factory.StartNew(() => Compare.Comparerer(newArray, 16));
                Thread.Sleep(50);
            }

            List<int> ignore = new List<int>();
            while (!taskArray.Last().IsCompleted)
            {
                for (int i = 0; i < taskArray.Count(); i++)
                {
                    if (!ignore.Contains(i) && taskArray[i].IsCompleted)
                    {
                        for (int j = 0; j < taskArray[i].Result.Count; j++)
                        {
                            //Causes the UI thread to throw an exeption
                            //MatchingFiles.Add(new Files(taskArray[i].Result[j].FileName1, taskArray[i].Result[j].FileName2));

                            //Tells the UI thread (dispatcher) to do something to the UI
                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                MatchingFiles.Add(new Files(taskArray[i].Result[j].FileName1, taskArray[i].Result[j].FileName2));
                            }, System.Windows.Threading.DispatcherPriority.Loaded);
                        }
                        ignore.Add(i);
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            ProgressBar.Text = "Progress: " + ignore.Count + "/" + files.Count();
                        }, System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
                Task.WaitAll(taskArray, 100);
            }

            Task.WaitAll(taskArray);
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ProgressBar.Text = "Progress: " + taskArray.Count() + "/" + files.Count();
            }, System.Windows.Threading.DispatcherPriority.Background);

            PC.Close();
            PC.Dispose();

            for (int i = 0; i < taskArray.Length; i++)
            {
                matches.AddRange(taskArray[i].Result);
                taskArray[i].Dispose();
            }

            matches.Sort();
            foreach (Match item in matches)
            {
                File.AppendAllText(Compare.outputDirectory + "\\" + Compare.workingDirectory.Substring(Compare.workingDirectory.LastIndexOf('\\') + 1) + ".txt", "Files " + item.FileName1 + " and " + item.FileName2 + " are " + item.EqualElements.ToString("P") + " equal" + Environment.NewLine);
                if (item.MarkForDeletion && deleteMode)
                {
                    Compare.Delete(item);
                }
            }

            TimeSpan timeSpan = DateTime.Now - dt1;

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                MatchingFiles.Add(new Files("Done", "Execution time: " + timeSpan.TotalMinutes.ToString("#.##") + " min"));
            });

            return;
        }

        public MainWindow()
        {
            InitializeComponent();
            MatchingFiles = new ObservableCollection<Files>();
            ResultView.ItemsSource = MatchingFiles;
            MatchingFiles.Add(new Files("Matches:", ""));
        }

        private void Button_Click_Working(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                //Open directory
                WorkingDirectory.Text = dialog.SelectedPath;
            }
        }

        private void Button_Click_Output(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                //Gets the directory path
                OutputDirectory.Text = dialog.SelectedPath;
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            deleteMode = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            deleteMode = false;
        }

        private void Button_Click_StartProgram(object sender, RoutedEventArgs e)
        {
            Compare.workingDirectory = WorkingDirectory.Text;
            Compare.outputDirectory = OutputDirectory.Text;
            Task.Factory.StartNew(() => MainFunction());
        }
    }

    public class Files
    {
        public Files(string fileName1, string fileName2)
        {
            FileName1 = fileName1;
            FileName2 = fileName2;
        }

        public string FileName1 { get; set; }
        public string FileName2 { get; set; }
    }
}
