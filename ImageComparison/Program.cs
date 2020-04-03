using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Management;

namespace ImageComparison
{
    class Match : IEquatable<Match>, IComparable<Match>
    {
        public string FileName1 { get; set; }
        public string FileName2 { get; set; }
        public double EqualElements { get; set; }

        public Match()
        {

        }

        public int CompareTo(Match compareMatch)
        {
            if (compareMatch == null)
                return 1;

            else
                return this.EqualElements.CompareTo(compareMatch.EqualElements);
        }
        public bool Equals(Match other)
        {
            if (other == null)
                return false;
            return (this.EqualElements.Equals(other.EqualElements));
        }
    }

    class Program
    {
        static bool debugMode = false;
        static Mutex mutex = new Mutex();
        static int tasksDone = 0;

        static Bitmap ToBlackWhite(Bitmap Bmp)
        {
            int rgb;
            Color c;

            for (int y = 0; y < Bmp.Height; y++)
            {
                for (int x = 0; x < Bmp.Width; x++)
                {
                    c = Bmp.GetPixel(x, y);
                    rgb = (int)(Math.Round(((double)(c.R + c.G + c.B) / 3.0) / 255) * 255);
                    Bmp.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            }
            return Bmp;
        }

        static List<byte> GetHash(Bitmap bitmap)
        {
            bitmap = new Bitmap(bitmap, new Size(64, 64)); //4096 resolution
            bitmap = ToBlackWhite(bitmap);
            List<byte> hashCode = new List<byte>();

            for (int i = 0; i < bitmap.Height; i++)
            {
                for (int j = 0; j < bitmap.Width; j++)
                {
                    if (bitmap.GetPixel(i,j) == Color.FromArgb(255,0,0,0))
                        hashCode.Add(1);
                    
                    else
                        hashCode.Add(0);
                    
                }                
            }
            bitmap.Dispose();
            return hashCode;
        }

        static List<Match> Comparer(IEnumerable<string> files)
        {
            List<Match> matches = new List<Match>();
            Bitmap bitmap = new Bitmap(files.ElementAt(0));
            if (debugMode)
            {
                Console.Write("File 1: {0}", files.ElementAt(0));
            }
            List<byte> hash1 = GetHash(bitmap);
            bitmap.Dispose();

            string fileName1 = files.ElementAt(0).Substring(files.ElementAt(0).LastIndexOf('\\') + 1, files.ElementAt(0).LastIndexOf('.') - 1 - files.ElementAt(0).LastIndexOf('\\'));
            if (fileName1.Length > 10)
                fileName1 = fileName1.Substring(0, 7) + "...";
            if (debugMode)
            {
                Console.WriteLine("File 1 name: {0}", fileName1);
            }

            for (int j = 1; j < files.Count(); j++)
            {
                bitmap = new Bitmap(files.ElementAt(j));
                if (debugMode)
                {
                    Console.Write("File 2: {0}", files.ElementAt(j));
                }
                List<byte> hash2 = GetHash(new Bitmap(files.ElementAt(j)));
                bitmap.Dispose();

                //determine the number of equal pixel (x of 64*64)
                int equalElements = hash1.Zip(hash2, (ii, jj) => ii == jj).Count(eq => eq);

                string fileName2 = files.ElementAt(j).Substring(files.ElementAt(j).LastIndexOf('\\') + 1, files.ElementAt(j).LastIndexOf('.') - 1 - files.ElementAt(j).LastIndexOf('\\'));
                if (fileName2.Length > 10)
                    fileName2 = fileName2.Substring(0, 7) + "...";
                if (debugMode)
                {
                    Console.WriteLine("File 2 name: {0}\nEqual elements: {1}", fileName2, equalElements);
                }


                if (((double)equalElements / (double)hash1.Count) > .93)
                {
                    if (debugMode)
                    {
                        Console.WriteLine("Files {0} and {1} are {2} equal", fileName1, fileName2, ((double)equalElements / (double)hash1.Count).ToString("P"));
                    }
                    matches.Add(new Match()
                    {
                        FileName1 = files.ElementAt(0).Substring(files.ElementAt(0).LastIndexOf('\\') + 1, files.ElementAt(0).LastIndexOf('.') - 1 - files.ElementAt(0).LastIndexOf('\\')),
                        FileName2 = files.ElementAt(j).Substring(files.ElementAt(j).LastIndexOf('\\') + 1, files.ElementAt(j).LastIndexOf('.') - 1 - files.ElementAt(j).LastIndexOf('\\')),
                        EqualElements = ((double)equalElements / (double)hash1.Count)
                    });
                }
                mutex.WaitOne();
                tasksDone++;
                mutex.ReleaseMutex();
            }
            return matches;
        }

        static void ProgressBar(int count)
        {
            int total = 0;
            int done = 0;

            for (int i = count; i > 0; i--)
            {
                total += i;
            }

            while(true)
            {
                mutex.WaitOne();
                done = tasksDone;
                mutex.ReleaseMutex();

                double percentDone = ((double)done / (double)total) * 100;

                Console.Clear();
                Console.Write("[");
                for (int i = 0; i < percentDone; i++)
                {
                    Console.Write("=");
                }
                for (double i = percentDone; i < 99; i++)
                {
                    Console.Write(" ");
                }
                Console.WriteLine("]\n{0}/{1}", done, total);

                if (done == total)
                {
                    break;
                }
                Thread.Sleep(1000);
            }
            return;
        }

        static void Main(string[] args)
        {
            string path = @"C:\Users\Andreas\OneDrive\Wallpaper3";
            string outputDirectory = @"C:\Users\Andreas\Desktop\";
            Console.WindowWidth = 120;
            

            IEnumerable<string> files;

            if (args.Count() >= 1)
            {
                if (args.Contains("-h") || args.Contains("-?"))
                {
                    Console.WriteLine("Usage: ImageComparison -h -e [Input path] [Output path] --debug\n" +
                        "\nOptions:" +
                        "\n\t-h\t\tDisplays the help" +
                        "\n\t--debug\t\tEnables debug mode");
                    return;
                }
                if (args.Contains("--debug"))
                {
                    debugMode = true;
                }
                if (args.Count() == 2 && !args.Contains("--debug"))
                {
                    path = args[0];
                    outputDirectory = args[1];
                }
                else
                {
                    path = args[0];
                }
            }

            files = Directory.EnumerateFiles(path);
            ulong memoryUsage;
            List<Match> matches = new List<Match>();
            {                 
                long preMemory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

                Bitmap bitmap = new Bitmap(files.ElementAt(0));
                List<byte> hash1 = GetHash(bitmap);
                bitmap.Dispose();
                long postMemory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

                memoryUsage = (ulong)(postMemory - preMemory);
            }

            Task progressBar = Task.Factory.StartNew(() => ProgressBar(files.Count() - 1));

            //Gets the amount of available memory
            //var memory = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory;

            Task<List<Match>>[] taskArray = new Task<List<Match>>[files.Count()];
            for (int i = 0; i < taskArray.Count(); i++)
            {
                ulong availableMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory;
                while (availableMemory <= memoryUsage * 10) 
                {
                    Thread.Sleep(100);
                    availableMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory;
                }

                var newArray = files.ToList().GetRange(i, files.Count() - i);
                taskArray[i] = Task<List<Match>>.Factory.StartNew(() => Comparer(newArray));
                Thread.Sleep(10);
            }

            Task.WaitAll(taskArray);
            Task.WaitAll(progressBar);

            for (int i = 0; i < taskArray.Length; i++)
            {
                matches.AddRange(taskArray[i].Result);
            }

            matches.Sort();
            foreach (Match item in matches)
            {
                File.AppendAllText(outputDirectory + path.Substring(path.LastIndexOf('\\') + 1) + ".txt", "Files " + item.FileName1 + " and " + item.FileName2 + " are " + item.EqualElements.ToString("P") + " equal" + Environment.NewLine);
            }
            Console.Write("\nDone.");
            Console.ReadLine();
            return;
        }

    }
}