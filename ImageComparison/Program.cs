using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Management;

using CompareFunctions;

namespace ImageComparisonConsole
{
    class Program
    {
        static bool deleteMode = false;
        static string workingDirectory = @"C:\Users\Andreas\OneDrive\Wallpaper3";
        static string outputDirectory = @"C:\Users\Andreas\Desktop";
        const int leftoverMemoryMB = 9000;
        
        static void Main(string[] args)
        {
            IEnumerable<string> files;
            int count = 0;

            if (args.Count() >= 1)
            {
                if (args.Contains("-h"))
                {
                    Console.WriteLine("Usage: ImageComparison [Input path] [Output path] -d -h --debug\n" +
                        "\nOptions:" +
                        "\n\t-d\t\tDeletes matches"+
                        "\n\t-h\t\tDisplays the help" +
                        "\n\t--debug\t\tEnables debug mode");
                    return;
                }
                if (args.Contains("-d"))
                {
                    deleteMode = true;
                }
                if (args.Count() == 2 && !args.Contains("--debug"))
                {
                    workingDirectory = args[0];
                    outputDirectory = args[1];

                    if (workingDirectory.Last()=='\\')
                    {
                        workingDirectory.Remove(workingDirectory.Count(), 1);
                    }
                    if (outputDirectory.Last() == '\\')
                    {
                        outputDirectory.Remove(outputDirectory.Count(), 1);
                    }
                }
                else if (args.Count() == 1)
                {
                    workingDirectory = args[0];
                }
            }

            Compare.workingDirectory = workingDirectory;
            Compare.outputDirectory = outputDirectory;

            files = Directory.EnumerateFiles(workingDirectory);
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
            
            DateTime tt1 = DateTime.Now;
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
                            Console.WriteLine("Files {0} and {1} are equal", taskArray[i].Result[j].FileName1, taskArray[i].Result[j].FileName2);
                        }
                        ignore.Add(i);
                    }
                }
                Task.WaitAll(taskArray, 5000);
            }

            Task.WaitAll(taskArray);
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
                File.AppendAllText(outputDirectory + "\\" + workingDirectory.Substring(workingDirectory.LastIndexOf('\\') + 1) + ".txt", "Files " + item.FileName1 + " and " + item.FileName2 + " are " + item.EqualElements.ToString("P") + " equal" + Environment.NewLine);
                if (item.MarkForDeletion && deleteMode)
                {
                    Compare.Delete(item);
                }
            }

            DateTime tt2 = DateTime.Now;
            TimeSpan ttimeSpan = tt2 - tt1;

            Console.WriteLine("Execution time: {0} sec", (ttimeSpan.TotalSeconds).ToString("N2"));

            Console.Write("\nDone.");
            Console.ReadLine();
            return;
        }
    }
}