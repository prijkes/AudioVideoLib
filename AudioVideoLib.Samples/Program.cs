/*
 * Date: 2012-12-01
 * Sources used: 
 */

using System;
using System.IO;

namespace AudioVideoLibExamples
{
    /// <summary>
    /// The program instance class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main function.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>0 if no error; else, 1</returns>
        public static int Main(string[] args)
        {
            Console.WriteLine(" ---------------------------------------");
            Console.WriteLine("|\t\t  2012-12-01\t\t|");
            Console.WriteLine(" ---------------------------------------");
            if (args.Length < 1)
            {
                PrintHelp();
                return 1;
            }

            string rootPath = null;
            for (int i = 0; i < args.Length; i++)
            {
                string s = args[i];
                switch (s)
                {
                    case "-p":
                        {
                            if (++i == args.Length)
                            {
                                Console.WriteLine("[-] Search path not specified.");
                                return 1;
                            }
                            rootPath = args[i];
                            if (!Directory.Exists(rootPath))
                            {
                                Console.WriteLine("[-] Path \"{0}\" not found.", rootPath);
                                return 1;
                            }
                        }
                        break;

                    default:
                        {
                            PrintHelp();
                        }
                        break;
                }
            }

            if (rootPath == null)
            {
                Console.WriteLine("[-] Missing search path.");
                return 1;
            }

            // Look for audio files.
            ExampleClass.SearchDirectory(rootPath);

            Console.WriteLine("All done.");
            return 0;
        }

        /// <summary>
        /// Prints the help.
        /// </summary>
        private static void PrintHelp()
        {
            Console.WriteLine(" argument\t\t parameters\t");
            Console.WriteLine(" -h (help)\t\t\t\t\t\t\t\t");
            Console.WriteLine(" -p (path)\t\t<path>\t\t path to look for files with Id3v2 tag(s).");
            Console.WriteLine();
        }
    }
}
