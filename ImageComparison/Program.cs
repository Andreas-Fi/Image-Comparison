using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        static void Main(string[] args)
        {
            string path = @"C:\Users\Andreas\OneDrive\Wallpaper4";
            string outputDirectory = @"C:\Users\Andreas\Desktop\";
            bool debugMode = false;

            IEnumerable<string> files;
            int count = 0;

            if (args.Count() >= 1)
            {
                if (args.Contains("-h"))
                {
                    Console.WriteLine("Usage: ImageComparison -h -e [Input path] [Output path] --debug\n" +
                        "\nOptions:" +
                        "\n\t-h\t\tDisplays the help" +
                        "\n\t-e Input path\tEstimates the execution time" +
                        "\n\t--debug\t\tEnables debug mode");
                    return;
                }
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
                    path = args[1];
                    files = Directory.EnumerateFiles(path);

                    for (int i = 1; i < files.Count(); i++)
                    {
                        int j = files.Count() - i;
                        count += j;
                    }
                    {
                        DateTime t1 = DateTime.Now;

                        Bitmap bitmap = new Bitmap(files.ElementAt(0));
                        List<byte> hash1 = GetHash(bitmap);
                        bitmap.Dispose();
                        bitmap = new Bitmap(files.ElementAt(1));
                        List<byte> hash2 = GetHash(new Bitmap(files.ElementAt(1)));
                        bitmap.Dispose();
                        int equalElements = hash1.Zip(hash2, (ii, jj) => ii == jj).Count(eq => eq);

                        DateTime t2 = DateTime.Now;
                        TimeSpan timeSpan = t2 - t1;
                        Console.WriteLine("Estimated execution time: {0} min", (timeSpan.TotalMinutes * count).ToString("N2"));
                        return;
                    }
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
            List<Match> matches = new List<Match>();
            count = 0;
            for (int i = 1; i < files.Count(); i++)
            {
                int j = files.Count() - i;
                count += j;
            }
            {                 
                DateTime t1 = DateTime.Now;

                Bitmap bitmap = new Bitmap(files.ElementAt(0));
                List<byte> hash1 = GetHash(bitmap);
                bitmap.Dispose();
                bitmap = new Bitmap(files.ElementAt(1));
                List<byte> hash2 = GetHash(new Bitmap(files.ElementAt(1)));
                bitmap.Dispose();
                int equalElements = hash1.Zip(hash2, (ii, jj) => ii == jj).Count(eq => eq);

                DateTime t2 = DateTime.Now;
                TimeSpan timeSpan = t2 - t1;
                Console.WriteLine("Estimated execution time: {0} min", (timeSpan.TotalMinutes * count).ToString("N2"));
            }

            for (int i = 0; i < files.Count(); i++) 
            {
                Bitmap bitmap = new Bitmap(files.ElementAt(i));
                if (debugMode)
                {
                    Console.Write("File 1: {0}", files.ElementAt(i));
                }
                List<byte> hash1 = GetHash(bitmap);
                bitmap.Dispose();

                string fileName1 = files.ElementAt(i).Substring(files.ElementAt(i).LastIndexOf('\\') + 1, files.ElementAt(i).LastIndexOf('.') - 1 - files.ElementAt(i).LastIndexOf('\\'));
                if (fileName1.Length > 10)
                    fileName1 = fileName1.Substring(0, 7) + "...";
                if (debugMode)
                {
                    Console.WriteLine("File 1 name: {0}", fileName1);
                }

                for (int j = i + 1; j < files.Count(); j++) 
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
                        Console.WriteLine("Files {0} and {1} are {2} equal\ti: {3}, j :{4}", fileName1, fileName2, ((double)equalElements / (double)hash1.Count).ToString("P"), i, j);
                        matches.Add(new Match()
                        {
                            FileName1 = files.ElementAt(i).Substring(files.ElementAt(i).LastIndexOf('\\') + 1, files.ElementAt(i).LastIndexOf('.') - 1 - files.ElementAt(i).LastIndexOf('\\')),
                            FileName2 = files.ElementAt(j).Substring(files.ElementAt(j).LastIndexOf('\\') + 1, files.ElementAt(j).LastIndexOf('.') - 1 - files.ElementAt(j).LastIndexOf('\\')),
                            EqualElements = ((double)equalElements / (double)hash1.Count)
                        });                    
                    }
                }
                Console.WriteLine("Iterations: {0} / {1}", i, files.Count()-1);
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