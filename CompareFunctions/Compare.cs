using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;

namespace CompareFunctions
{
    public static class Compare
    {
        public static string workingDirectory;
        public static string outputDirectory;

        /// <summary>
        ///     Modifies the "bitmap" object from a colored image to a black and white image
        /// </summary>
        /// <param name="bitmap">The unmodified object</param>
        /// <returns>Returns a black and white object</returns>
        public static Bitmap ToBlackWhite(Bitmap bitmap)
        {
            int rgb;
            Color c;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    c = bitmap.GetPixel(x, y);
                    rgb = (int)(Math.Round(((double)(c.R + c.G + c.B) / 3.0) / 255) * 255);
                    bitmap.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            }
            return bitmap;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static List<byte> GetHash(Bitmap bitmap, int size)
        {
            bitmap = new Bitmap(bitmap, new Size(size, size)); //64 = 4096 resolution
            bitmap = ToBlackWhite(bitmap);
            List<byte> hashCode = new List<byte>();

            for (int i = 0; i < bitmap.Height; i++)
            {
                for (int j = 0; j < bitmap.Width; j++)
                {
                    if (bitmap.GetPixel(i, j) == Color.FromArgb(255, 0, 0, 0))
                        hashCode.Add(1);
                    else
                        hashCode.Add(0);
                }
            }
            bitmap.Dispose();
            return hashCode;
        }
        /// <summary>
        ///     Reads a image from "path" and returns the image as a Bitmap object
        /// </summary>
        /// <param name="path">Logical path to the file</param>
        /// <returns>Returns a bitmap object of the file</returns>
        public static Bitmap FromFile(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            MemoryStream ms = new MemoryStream(bytes);
            Image img = Image.FromStream(ms);
            return (Bitmap)img;
        }

        /// <summary>
        ///     Gets the difference of two byte objects in percent
        /// </summary>
        /// <param name="b1">Byte object 1</param>
        /// <param name="b2">Byte object 2</param>
        /// <returns>Returns the percentage</returns>
        public static double PercentDifference(byte b1, byte b2)
        {
            double returnValue = Math.Abs(((double)(b1 / (double)b2) - 1.0));
            return returnValue;
        }
        /// <summary>
        ///     Gets the difference of two color objects in percent
        /// </summary>
        /// <param name="c1">Color object 1</param>
        /// <param name="c2">Color object 2</param>
        /// <returns>Returns the percentage</returns>
        public static double PercentDifference(Color c1, Color c2)
        {
            double dif = 0;
            double difR = PercentDifference(c1.R, c2.R);
            double difG = PercentDifference(c1.G, c2.G);
            double difB = PercentDifference(c1.B, c2.B);

            dif = difR + difG + difB;
            return dif;
        }

        /// <summary>
        ///     Compares a list of modified images against eachother
        ///     Modifications made:
        ///         - Made the files black and white
        ///         - Size modified to "size" by "size"
        /// </summary>
        /// <param name="files">A list of paths to image files</param>
        /// <param name="size">How large every image gets modified to</param>
        /// <returns>Returns a list of matches</returns>
        public static List<Match> Comparerer(IEnumerable<string> files, int size)
        {
            // Return object
            List<Match> matches = new List<Match>();
            //Gets the first file from the list and creates a Bitmap from it
            Bitmap bitmap = FromFile(files.ElementAt(0));

            //Gets the hash for the first file
            List<byte> hash1 = GetHash(bitmap, size);
            bitmap.Dispose();

            string fileName1 = files.ElementAt(0).Substring(files.ElementAt(0).LastIndexOf('\\') + 1);
            if (fileName1.Length > 10)
                fileName1 = fileName1.Substring(0, 7) + "...";

            for (int j = 1; j < files.Count(); j++)
            {
                /*ulong availableMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory / 1048576; // memsize in MB
                while (availableMemory <= leftoverMemoryMB)
                {
                    Thread.Sleep(100);
                    availableMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory / 1048576; // memsize in MB
                }*/

                bitmap = FromFile(files.ElementAt(j));

                List<byte> hash2 = GetHash(bitmap, size);
                bitmap.Dispose();

                //determine the number of equal pixel (x of 64*64)
                int equalElements = hash1.Zip(hash2, (ii, jj) => ii == jj).Count(eq => eq);

                string fileName2 = files.ElementAt(j).Substring(files.ElementAt(j).LastIndexOf('\\') + 1 /*, files.ElementAt(j).LastIndexOf('.') - 1 - files.ElementAt(j).LastIndexOf('\\')*/);
                if (fileName2.Length > 10)
                    fileName2 = fileName2.Substring(0, 7) + "...";

                if (((double)equalElements / (double)hash1.Count) > .98 && size >= 64)
                {
                    bool match = ColourComparerer(files.ElementAt(0), files.ElementAt(j));
                    if (match)
                    {
                        //Console.WriteLine("Files {0} and {1} are {2} equal", fileName1, fileName2, ((double)equalElements / (double)hash1.Count).ToString("P"));

                        matches.Add(new Match()
                        {
                            FileName1 = files.ElementAt(0).Substring(files.ElementAt(0).LastIndexOf('\\') + 1 /*, files.ElementAt(0).LastIndexOf('.') - 1 - files.ElementAt(0).LastIndexOf('\\')*/),
                            FileName2 = files.ElementAt(j).Substring(files.ElementAt(j).LastIndexOf('\\') + 1 /*, files.ElementAt(j).LastIndexOf('.') - 1 - files.ElementAt(j).LastIndexOf('\\')*/),
                            EqualElements = ((double)equalElements / (double)hash1.Count),
                            MarkForDeletion = match
                        });
                    }
                }
                else if (((double)equalElements / (double)hash1.Count) > .98)
                {
                    List<string> list = new List<string>
                    {
                        files.ElementAt(0),
                        files.ElementAt(j)
                    };

                    matches.AddRange(Comparerer(list, 64));
                }
            }
            return matches;
        }
        /// <summary>
        ///     Compares two modified images against eachother
        ///     Modifications made:
        ///         - Made the image 512x512 pixels
        /// </summary>
        /// <param name="file1">Path to the first file</param>
        /// <param name="file2">Path to the second file</param>
        /// <returns>
        ///     Returns true if the files are 90% or more equal
        ///     Otherwise returns false
        /// </returns>
        public static bool ColourComparerer(string file1, string file2)
        {
            Bitmap bitmap = FromFile(file1);
            bitmap = new Bitmap(bitmap, new Size(512, 512));

            Bitmap bitmap2 = FromFile(file2);
            bitmap2 = new Bitmap(bitmap2, new Size(512, 512));

            int equalElements = 0;
            int count = 0;

            for (int i = 0; i < bitmap.Height; i++)
            {
                for (int j = 0; j < bitmap.Width; j++)
                {
                    count++;
                    if (PercentDifference(bitmap.GetPixel(i, j), bitmap2.GetPixel(i, j)) <= 0.10)
                    {
                        equalElements++;
                    }
                }
            }

            bitmap.Dispose();
            bitmap2.Dispose();

            double t = (double)equalElements / (double)count;

            if (((double)equalElements / (double)count) > .90)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Copies the "item" to the outputfolder
        ///     then deletes the original "item"
        /// </summary>
        /// <param name="item">Item that is being deleted</param>
        /// <returns>True if delete succeeded, false if not</returns>
        public static bool Delete(Match item)
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
            catch (Exception /*ex*/)
            {
                File.AppendAllText(outputDirectory + "\\" + workingDirectory.Substring(workingDirectory.LastIndexOf('\\') + 1) + ".txt", "File " + item.FileName2 + " COULD NOT be deleted" + Environment.NewLine);
                return false;
            }
            return true;
        }
    }
}