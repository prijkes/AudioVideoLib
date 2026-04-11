/*
 * Date: 2012-12-01
 * Sources used: 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AudioVideoLibExamples
{
    /// <summary>
    /// The program instance class.
    /// </summary>
    public static class ExampleClass
    {
        /// <summary>
        /// Searches the directory.
        /// </summary>
        /// <param name="path">The current path.</param>
        public static void SearchDirectory(string path)
        {
            // Look for subdirectories in current directory
            Console.WriteLine("[*] Current directory: {0}", path);
            string[] subs;
            try
            {
                subs = Directory.GetDirectories(path);
            }
            catch (Exception e)
            {
                Console.WriteLine("[-] Failed to search directory \"{0}\" for reason {1}.", path, e.Message);
                return;
            }

            foreach (string sub in subs)
                SearchDirectory(sub);

            // No sub directories in current directory, list the files in current directory.
            string[] list = SearchDirectoryForFiles(path, new[] { "*" });
            if (list.Length == 0)
                return;

            ParseFiles(list);

            Console.WriteLine("Press any key");
            Console.ReadKey();
        }

        /// <summary>
        /// Searches the directory for files.
        /// </summary>
        /// <param name="dir">The directory to look in.</param>
        /// <param name="fileExtension">The file extensions.</param>
        /// <returns>A string array filled with all the files in the directory.</returns>
        private static string[] SearchDirectoryForFiles(string dir, IEnumerable<string> fileExtension)
        {
            if (dir == null)
                throw new ArgumentNullException("dir");

            if (fileExtension == null)
                throw new ArgumentNullException("fileExtension");

            List<string> list = new List<string>();
            foreach (string[] files in fileExtension.Select(ext => Directory.GetFiles(dir, "*." + ext)))
                list.AddRange(files);

            return list.ToArray();
        }

        /// <summary>
        /// Parses the files.
        /// </summary>
        /// <param name="files">The files.</param>
        private static void ParseFiles(IEnumerable<string> files)
        {
            IEnumerable<string> list = files as List<string> ?? files.ToList();

            // Check each file which file path doesn't exceed 260 characters (FileInfo will throw an exception otherwise).
            foreach (FileInfo file in list.Where(filePath => filePath.Length < 260).Select(filePath => new FileInfo(filePath)))
            {
                Console.WriteLine("[*] Checking file: {0}", file.Name);
                FileStream fs = file.OpenRead();

                // Parse the files in the Id3v2 frame parsed example.
                Id3v2FrameParsedExample.ParseStream(fs);

                // Parse the files in the Id3v2 frame cryption example.
                fs.Position = 0;
                Id3v2FrameCryptionExample.ParseStream(fs);

                // Parse the files in the Id3v2 frame parse example.
                fs.Position = 0;
                Id3v2FrameParseExample.ParseStream(fs);

                // Parse the MPA files.
                fs.Position = 0;
                MpaStreamExample.ParseStream(fs);

                Console.WriteLine();
            }
        }
    }
}
