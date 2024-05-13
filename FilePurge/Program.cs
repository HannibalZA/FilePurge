using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FilePurge
{
    internal class Program
    {
        public static List<string> files = new List<string>();
        static void Main(string[] args)
        {
        start:
            string base_dir = "";
            Console.WriteLine(@"
File Purge utility
");
            Console.WriteLine("Set search base directory:");
            base_dir = Console.ReadLine();
            if (base_dir != "")
            {
                if (!Directory.Exists(base_dir))
                {
                    Console.WriteLine("Directory does not exist. Try again...");
                    goto start;
                }
            }

            Console.WriteLine($@"Base directory set as: {base_dir}.");
            Console.WriteLine(@"Purge command options:
filematch [foldermatch] [ignorematch] [olderThan] [newerThan]
examples:

Just delete all png files
*.png

 Delete all png files in directories ending in 'to delete'
*.png *to delete

Delete png files in a directories ending in 'to delete', skip files ending in 'keep.png' and delete files older than 2024-01-01 and newer than 2023-12-01
*.png *to delete *keep.png 2024-01-01 2023-12-01
");

        restart:
            Console.WriteLine("proceed...");
            string[] arguments = Console.ReadLine().Split(' ');
            AppArgs appargs = new AppArgs(arguments);
            if (!appargs.valid)
            {
                Console.Clear();
                Console.WriteLine(@"Purge command options:
filematch [foldermatch] [ignorematch] [olderThan] [newerThan]
examples:

Just delete all png files
*.png

 Delete all png files in directories ending in 'to delete'
*.png *to delete

Delete png files in a directories ending in 'to delete', skip files ending in 'keep.png' and delete files older than 2024-01-01 and newer than 2023-12-01
*.png *to delete *keep.png 2024-01-01 2023-12-01
");
                goto restart;
            }
            else
            {
                files.Clear();
                Console.WriteLine("Searching...");
                SearchAccessibleFiles(base_dir, appargs.fileMatch, appargs.folderMatch, appargs.ignoreMatch, appargs.olderThan, appargs.newerThan);

                Console.WriteLine("Done searching.");

                if (files.Count < 100)
                {
                    foreach (string file in files)
                    {
                        Console.WriteLine($"{file}");
                    }
                }

                Console.WriteLine(files.Count + " files found...");
                Console.WriteLine("Proceed with delete? [y/n]");
                string proceed = Console.ReadLine();
                if (proceed.ToLower() == "y")
                {
                    Console.WriteLine("Deleting!");
                    foreach (string file in files)
                    {
                        Console.WriteLine($"{Path.GetFileName(file)} deleted");
                        File.Delete(file);
                    }
                }
                else
                {
                    goto restart;
                }
            }
            Console.WriteLine("All done. Hit any key to exit...");
            Console.ReadLine();
        }
        static void SearchAccessibleFiles(string root, string fileMatch, string folderMatch, string ignoreMatch, DateTime olderThan, DateTime newerThan)
        {
            Console.WriteLine("Searching " + root);
            foreach (var file in Directory.EnumerateFiles(root).Where(m => MatchByFilename(m, fileMatch) && !MatchByFilename(m, ignoreMatch)))
            {
                FileInfo fileInfo = new FileInfo(file);

                if (fileInfo.LastWriteTime > newerThan && fileInfo.LastWriteTime < olderThan)
                {
                    files.Add(file);
                }
            }
            foreach (var subDir in Directory.EnumerateDirectories(root).Where(d => MatchByFoldername(d, folderMatch)))
            {
                try
                {
                    SearchAccessibleFiles(subDir, fileMatch, folderMatch, ignoreMatch, olderThan, newerThan);
                }
                catch (UnauthorizedAccessException ex)
                {
                    // ... do what you like or just catch it...
                    Console.WriteLine(ex.Message);
                }
            }
        }
        static bool MatchByFilename(string filepath, string fileMatch)
        {
            return Matches(Path.GetFileName(filepath), fileMatch);
        }
        static bool MatchByFoldername(string folderpath, string folderMatch)
        {
            return Matches(new DirectoryInfo(folderpath).Name, folderMatch);
        }
        public static bool Matches(string subject, string wildcardPattern)
        {
            //Courtesy of
            //https://www.hiimray.co.uk/2020/04/18/implementing-simple-wildcard-string-matching-using-regular-expressions/474
            //Thanks, nerd.

            if (string.IsNullOrWhiteSpace(wildcardPattern))
            {
                return true;
            }

            if (wildcardPattern == "!") { //for negative matching requirement e.g. ignoreMatch empty, so don't match anything...
                return false; 
            }

            string regexPattern = string.Concat("^", Regex.Escape(wildcardPattern).Replace("\\*", ".*"), "$");

            int wildcardCount = wildcardPattern.Count(x => x.Equals('*'));

            if (wildcardCount <= 0)
            {
                return subject.Equals(wildcardPattern, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (wildcardCount == 1)
            {
                string newWildcardPattern = wildcardPattern.Replace("*", "");

                if (wildcardPattern.StartsWith("*"))
                {
                    return subject.EndsWith(newWildcardPattern, StringComparison.CurrentCultureIgnoreCase);
                }
                else if (wildcardPattern.EndsWith("*"))
                {
                    return subject.StartsWith(newWildcardPattern, StringComparison.CurrentCultureIgnoreCase);
                }
                else
                {
                    try
                    {
                        return Regex.IsMatch(subject, regexPattern);
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            else
            {
                try
                {
                    return Regex.IsMatch(subject, regexPattern);
                }
                catch
                {
                    return false;
                }
            }
        }
    }
    public class AppArgs
    {
        public string fileMatch { get; set; } = "";
        public string folderMatch { get; set; } = "";
        public string ignoreMatch { get; set; } = "!";
        public DateTime olderThan { get; set; } = DateTime.Now;
        public DateTime newerThan { get; set; } = DateTime.MinValue;

        public bool valid = false;

        public AppArgs(string[] args)
        {
            //Args filematch [foldermatch] [ignorematch] [olderThan] [newerThan]
            int len = args.Length;
            switch (len)
            {
                case 0:
                    valid = false;
                    break;
                case 1:
                    valid = true;
                    fileMatch = args[0];
                    break;
                case 2:
                    valid = true;
                    fileMatch = args[0];
                    folderMatch = args[1];
                    break;
                case 3:
                    valid = true;
                    fileMatch = args[0];
                    folderMatch = args[1];
                    ignoreMatch = args[2];
                    break;
                case 4:
                    valid = true;
                    fileMatch = args[0];
                    folderMatch = args[1];
                    ignoreMatch = args[2];
                    olderThan = args[3].ToDate();
                    break;
                case 5:
                    valid = true;
                    fileMatch = args[0];
                    folderMatch = args[1];
                    ignoreMatch = args[2];
                    olderThan = args[3].ToDate();
                    newerThan = args[4].ToDate();
                    if (newerThan > olderThan)
                    {
                        newerThan = olderThan;
                        olderThan = args[4].ToDate();
                    }
                    break;
            }
        }
    }
    public static class Extensions
    {
        public static bool IsDate(this object Expression)
        {
            if (Expression != null)
            {
                if (Expression is DateTime)
                {
                    return true;
                }
                if (Expression is string)
                {
                    DateTime dt;
                    return DateTime.TryParse((string)Expression, out dt);
                }
            }
            return false;
        }
        public static DateTime ToDate(this object Expression)
        {
            if (Expression.IsDate())
            {
                return Convert.ToDateTime(Expression);
            }
            else
            {
                return DateTime.MinValue;
            }
        }
    }
}
