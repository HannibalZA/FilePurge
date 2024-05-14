using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FilePurge
{
    internal class Program
    {
        static string cli_base { get; set; }
        static string cli_args { get; set; }
        static bool cli_mode { get; set; } = false;
        static void Main(string[] args)
        {
            Console.WriteLine(@"
File Purge Utility
");
            CheckCLI(args);

            while (true)
            {
                string baseDir = GetBaseDirectory();
                if (!string.IsNullOrEmpty(baseDir))
                {
                    AppArgs appArgs = GetArguments();
                    if (appArgs != null)
                    {
                        SearchAndDeleteFiles(baseDir, appArgs);
                    }
                }
                if (cli_mode)
                {
                    Environment.Exit(0);
                }
            }
        }

        static void CheckCLI(string[] args)
        {
            if (args.Length > 1)
            {
                string baseDir = args[0];
                string arguments = String.Join(" ", args.Skip(1));
                if (Directory.Exists(baseDir))
                {
                    cli_base = baseDir;
                    cli_args = arguments;
                    cli_mode = true;
                }
            }
        }
        static string GetBaseDirectory()
        {
            Console.WriteLine("Set search base directory:");
            string baseDir = cli_mode ? cli_base : Console.ReadLine();
            if (string.IsNullOrEmpty(baseDir))
            {
                Console.WriteLine("Base directory cannot be empty.");
                return null;
            }
            if (!Directory.Exists(baseDir))
            {
                Console.WriteLine("Directory does not exist.");
                return null;
            }
            Console.WriteLine($@"Base directory set as: {baseDir}.");
            return baseDir;
        }

        static AppArgs GetArguments()
        {
            Console.WriteLine("Enter purge command:");
            Console.WriteLine(@"
Purge command options:
filematch [foldermatch] [ignorematch] [olderThan] [newerThan]

Individual parameters cannot have spaces!

Examples:

    Just delete all png files
    *.png

    Delete all png files in directories ending in 'to_delete'
    *.png *to_delete

    Delete png files in a directories ending in 'to_delete', skip files ending in 'keep.png' and delete files older than 2024-01-01 and newer than 2023-12-01
    *.png *to_delete *keep.png 2024-01-01 2023-12-01
");
            int restartcount = 0;
        restart:
            string[] arguments = cli_mode ? cli_args.Split(' ') : Console.ReadLine().Split(' ');
            AppArgs appArgs = new AppArgs(arguments);
            if (!appArgs.IsValid)
            {
                Console.WriteLine("Invalid purge command. Please try again.");
                restartcount++;
                if (restartcount == 3) { return null; }
                goto restart;
            }
            return appArgs;
        }

        static void SearchAndDeleteFiles(string baseDir, AppArgs appArgs)
        {
            List<string> files = new List<string>();
            SearchAccessibleFiles(baseDir, appArgs, files);
            if (files.Count > 0 && files.Count < 100)
            {
                foreach (string file in files)
                {
                    Console.WriteLine($"{file}");
                }
            }
            Console.WriteLine($"{files.Count} files found...");
            if (files.Count > 0)
            {
                Console.WriteLine("Proceed with delete? [y/n]");
                string proceed = cli_mode ? "y" : Console.ReadLine();
                if (proceed.ToLower() == "y")
                {
                    DeleteFiles(files);

                    Console.WriteLine(@"
All done! 

Hit q to quit or any other key to restart...
");
                    if (cli_mode)
                    {
                        Environment.Exit(0);
                    }
                    else if (Console.ReadKey().Key == ConsoleKey.Q)
                    {
                        Environment.Exit(0);
                    }
                    else
                    {
                        Console.Clear();
                    }
                }
            }
            else
            {
                if (cli_mode)
                {
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("Hit any key to restart...");
                    Console.ReadKey();
                }
            }
        }

        static void SearchAccessibleFiles(string root, AppArgs appArgs, List<string> files)
        {
            //Thanks
            //https://gist.github.com/smith-neil/9403767
            //Very handy indeed

            foreach (var file in Directory.EnumerateFiles(root).Where(m => MatchByFilename(m, appArgs.FileMatch) && !MatchByFilename(m, appArgs.IgnoreMatch)))
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime > appArgs.NewerThan && fileInfo.LastWriteTime < appArgs.OlderThan)
                {
                    files.Add(file);
                }
            }
            foreach (var subDir in Directory.EnumerateDirectories(root).Where(d => MatchByFoldername(d, appArgs.FolderMatch)))
            {
                try
                {
                    SearchAccessibleFiles(subDir, appArgs, files);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        static void DeleteFiles(List<string> files)
        {
            foreach (string file in files)
            {
                try
                {
                    File.Delete(file);
                    Console.WriteLine($"{Path.GetFileName(file)} deleted");
                }
                catch
                {
                    Console.WriteLine($"Unable to delete {file}");
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

            if (wildcardPattern == "!")
            { //for negative matching requirement e.g. ignoreMatch empty, so don't match anything...
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
                        return Regex.IsMatch(subject, regexPattern, RegexOptions.IgnoreCase);
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
                    return Regex.IsMatch(subject, regexPattern, RegexOptions.IgnoreCase);
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
        public string FileMatch { get; set; } = "";
        public string FolderMatch { get; set; } = "";
        public string IgnoreMatch { get; set; } = "!";
        public DateTime OlderThan { get; set; } = DateTime.Now;
        public DateTime NewerThan { get; set; } = DateTime.MinValue;

        public bool IsValid = false;

        public AppArgs(string[] args)
        {
            //Args filematch [foldermatch] [ignorematch] [olderThan] [newerThan]
            int len = args.Length;

            switch (len)
            {
                case 1:
                    IsValid = true;
                    FileMatch = args[0];
                    break;
                case 2:
                    IsValid = true;
                    FileMatch = args[0];
                    FolderMatch = args[1];
                    break;
                case 3:
                    IsValid = true;
                    FileMatch = args[0];
                    FolderMatch = args[1];
                    IgnoreMatch = args[2];
                    break;
                case 4:
                    IsValid = true;
                    FileMatch = args[0];
                    FolderMatch = args[1];
                    IgnoreMatch = args[2];
                    OlderThan = args[3].ToDate();
                    break;
                case 5:
                    IsValid = true;
                    FileMatch = args[0];
                    FolderMatch = args[1];
                    IgnoreMatch = args[2];
                    OlderThan = args[3].ToDate();
                    NewerThan = args[4].ToDate();
                    if (NewerThan > OlderThan)
                    {
                        NewerThan = OlderThan;
                        OlderThan = args[4].ToDate();
                    }
                    break;
                default:
                    IsValid = false;
                    break;
            }
            if (FileMatch == "") { IsValid = false; } //Let's not be silly...
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
