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
        public bool MarkForDeletion { get; set; }

        public Match()
        {
            MarkForDeletion = false;
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
        static bool deleteMode = false;
        static string workingDirectory = @"C:\Users\Andreas\OneDrive\Wallpaper4";
        static string outputDirectory = @"C:\Users\Andreas\Desktop";

        public static Bitmap ToBlackWhite(Bitmap Bmp)
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
        static List<byte> GetHash(Bitmap bitmap, int size)
        {
            bitmap = new Bitmap(bitmap, new Size(size, size)); //64 = 4096 resolution
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
        static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            //file is not locked
            return false;
        }
        public static Bitmap FromFile(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var ms = new MemoryStream(bytes);
            var img = Image.FromStream(ms);
            return (Bitmap)img;
        }

        private static List<Match> Comparerer64(IEnumerable<string> files)
        {
            List<Match> matches = new List<Match>();
            Bitmap bitmap = FromFile(files.ElementAt(0));
            if (debugMode)
            {
                Console.Write("File 1: {0}", files.ElementAt(0));
            }
            List<byte> hash1 = GetHash(bitmap, 64);
            bitmap.Dispose();

            string fileName1 = files.ElementAt(0).Substring(files.ElementAt(0).LastIndexOf('\\') + 1 /*, files.ElementAt(0).LastIndexOf('.') - 1 - files.ElementAt(0).LastIndexOf('\\')*/);
            if (fileName1.Length > 10)
                fileName1 = fileName1.Substring(0, 7) + "...";
            if (debugMode)
            {
                Console.WriteLine("File 1 name: {0}", fileName1);
            }

            for (int j = 1; j < files.Count(); j++)
            {
                bitmap = FromFile(files.ElementAt(j));
                if (debugMode)
                {
                    Console.Write("File 2: {0}", files.ElementAt(j));
                }
                List<byte> hash2 = GetHash(bitmap, 64);
                bitmap.Dispose();

                //determine the number of equal pixel (x of 64*64)
                int equalElements = hash1.Zip(hash2, (ii, jj) => ii == jj).Count(eq => eq);

                string fileName2 = files.ElementAt(j).Substring(files.ElementAt(j).LastIndexOf('\\') + 1 /*, files.ElementAt(j).LastIndexOf('.') - 1 - files.ElementAt(j).LastIndexOf('\\')*/);
                if (fileName2.Length > 10)
                    fileName2 = fileName2.Substring(0, 7) + "...";
                if (debugMode)
                {
                    Console.WriteLine("File 2 name: {0}\nEqual elements: {1}", fileName2, equalElements);
                }

                if (((double)equalElements / (double)hash1.Count) > .98)
                {
                    Console.WriteLine("Files {0} and {1} are {2} equal", fileName1, fileName2, ((double)equalElements / (double)hash1.Count).ToString("P"));
                    
                    bool match = ColourComparerer(files.ElementAt(0), files.ElementAt(j));
                    if (match)
                    {
                        matches.Add(new Match()
                        {
                            FileName1 = files.ElementAt(0).Substring(files.ElementAt(0).LastIndexOf('\\') + 1 /*, files.ElementAt(0).LastIndexOf('.') - 1 - files.ElementAt(0).LastIndexOf('\\')*/),
                            FileName2 = files.ElementAt(j).Substring(files.ElementAt(j).LastIndexOf('\\') + 1 /*, files.ElementAt(j).LastIndexOf('.') - 1 - files.ElementAt(j).LastIndexOf('\\')*/),
                            EqualElements = ((double)equalElements / (double)hash1.Count),
                            MarkForDeletion = match
                        });
                    }                    
                }
            }
            return matches;
        }
        private static bool ColourComparerer(string file1, string file2)
        {
            Bitmap bitmap = FromFile(file1);
            bitmap = new Bitmap(bitmap, new Size(512, 512));
            if (debugMode)
            {
                Console.Write("File 1: {0}", file1);
            }
            //List<byte> hash1 = GetHash(bitmap, 128);
            //bitmap.Dispose();

            string fileName1 = file1.Substring(file1.LastIndexOf('\\') + 1 /*, file1.LastIndexOf('.') - 1 - file1.LastIndexOf('\\')*/);
            if (fileName1.Length > 10)
                fileName1 = fileName1.Substring(0, 7) + "...";
            if (debugMode)
            {
                Console.WriteLine("File 1 name: {0}", fileName1);
            }

            Bitmap bitmap2 = FromFile(file2);
            bitmap2 = new Bitmap(bitmap2, new Size(512, 512));
            if (debugMode)
            {
                Console.Write("File 2: {0}", file2);
            }
            //List<byte> hash2 = GetHash(bitmap, 128);
            //bitmap.Dispose();

            //determine the number of equal pixel (x of 128*128)
            int equalElements = 0; //hash1.Zip(hash2, (ii, jj) => ii == jj).Count(eq => eq);
            int count = 0;

            for (int i = 0; i < bitmap.Height; i++)
            {
                for (int j = 0; j < bitmap.Width; j++)
                {
                    count++;
                    if (bitmap.GetPixel(i,j).ToArgb() == bitmap2.GetPixel(i,j).ToArgb())
                    {
                        equalElements++;
                    }
                }
            }

            string fileName2 = file2.Substring(file2.LastIndexOf('\\') + 1 /*, file2.LastIndexOf('.') - 1 - file2.LastIndexOf('\\')*/);
            if (fileName2.Length > 10)
                fileName2 = fileName2.Substring(0, 7) + "...";
            if (debugMode)
            {
                Console.WriteLine("File 2 name: {0}\nEqual elements: {1}", fileName2, equalElements);
            }

            bitmap.Dispose();
            bitmap2.Dispose();

            if (((double)equalElements / (double)/*hash1.Count*/ count) > .98)
            {
                return true;
            }
            
            return false;
        }
        static void Delete(Match item)
        {
            string folder = workingDirectory.Substring(workingDirectory.LastIndexOf('\\') + 1);
            if (!Directory.Exists(outputDirectory + "\\" + folder))
            {
                Directory.CreateDirectory(outputDirectory + "\\" + folder);
            }

            try
            {
                File.Copy(workingDirectory + "\\" + item.FileName2, outputDirectory + "\\" + folder + "\\" + item.FileName2, true);
                File.Delete(workingDirectory + "\\" + item.FileName2);
                File.AppendAllText(outputDirectory + "\\" + workingDirectory.Substring(workingDirectory.LastIndexOf('\\') + 1) + ".txt", "File " + item.FileName2 + " is now deleted" + Environment.NewLine);
            }
            catch (Exception ex)
            {
                File.AppendAllText(outputDirectory + "\\" + workingDirectory.Substring(workingDirectory.LastIndexOf('\\') + 1) + ".txt", "File " + item.FileName2 + " COULD NOT be deleted" + Environment.NewLine);
            }            
            return;
        }

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
                        //"\n\t-e Input path\tEstimates the execution time (Depricated)" +
                        "\n\t--debug\t\tEnables debug mode");
                    return;
                }
                //Exec time estimation (Depricated)
                if (args.Contains("-e"))
                {
                    if (args.Count() < 2)
                    {
                        Console.WriteLine("Usage: ImageComparison -h -e [Input path] [Output path]\n" +
                        "\nOptions:" +
                        "\n\t-h\t\tDisplays the help" +
                        "\n\t-e Input path\tEstimates the execution time");
                        return;
                    }
                    workingDirectory = args[1];
                    files = Directory.EnumerateFiles(workingDirectory);

                    for (int i = 1; i < files.Count(); i++)
                    {
                        int j = files.Count() - i;
                        count += j;
                    }
                    {
                        DateTime t1 = DateTime.Now;
                                                
                        Bitmap bitmap = new Bitmap(files.ElementAt(0));
                        List<byte> hash1 = GetHash(bitmap, 64);
                        bitmap.Dispose();
                        bitmap = new Bitmap(files.ElementAt(1));
                        List<byte> hash2 = GetHash(new Bitmap(files.ElementAt(1)), 64);
                        bitmap.Dispose();
                        int equalElements = hash1.Zip(hash2, (ii, jj) => ii == jj).Count(eq => eq);

                        DateTime t2 = DateTime.Now;
                        TimeSpan timeSpan = t2 - t1;

                        Console.WriteLine("Estimated execution time: {0} min", (timeSpan.TotalMinutes * count).ToString("N2"));
                        return;
                    }
                }
                //Enables debugging information
                if (args.Contains("--debug"))
                {
                    debugMode = true;
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
                else
                {
                    workingDirectory = args[0];
                }
            }

            files = Directory.EnumerateFiles(workingDirectory);
            ulong memoryUsage;
            List<Match> matches = new List<Match>();
            count = 0;
            for (int i = 1; i < files.Count(); i++)
            {
                int j = files.Count() - i;
                count += j;
            }
            //Estimates the exec time and memory usage
            {                 
                DateTime t1 = DateTime.Now;
                long preMemory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

                Bitmap bitmap = new Bitmap(files.ElementAt(0));
                List<byte> hash1 = GetHash(bitmap, 64);
                bitmap.Dispose();
                bitmap = new Bitmap(files.ElementAt(1));
                List<byte> hash2 = GetHash(new Bitmap(files.ElementAt(1)), 64);
                long postMemory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
                bitmap.Dispose();
                int equalElements = hash1.Zip(hash2, (ii, jj) => ii == jj).Count(eq => eq);

                DateTime t2 = DateTime.Now;
                TimeSpan timeSpan = t2 - t1;

                memoryUsage = (ulong)(postMemory - preMemory);
                Console.WriteLine("Estimated execution time: {0} min", (timeSpan.TotalMinutes * count).ToString("N2"));
            }

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
                taskArray[i] = Task<List<Match>>.Factory.StartNew(() => Comparerer64(newArray));
                Thread.Sleep(10);
            }

            Task.WaitAll(taskArray);

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
                    Delete(item);
                }
            }
            Console.Write("\nDone.");
            Console.ReadLine();
            return;
        }
    }
}