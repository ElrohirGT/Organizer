using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace ComicOrganizer
{
    public class ComicOrganizer
    {
        string[] Title =
        {
            "░█████╗░██████╗░░██████╗░░█████╗░███╗░░██╗██╗███████╗███████╗██████╗░",
            "██╔══██╗██╔══██╗██╔════╝░██╔══██╗████╗░██║██║╚════██║██╔════╝██╔══██╗",
            "██║░░██║██████╔╝██║░░██╗░███████║██╔██╗██║██║░░███╔═╝█████╗░░██████╔╝",
            "██║░░██║██╔══██╗██║░░╚██╗██╔══██║██║╚████║██║██╔══╝░░██╔══╝░░██╔══██╗",
            "╚█████╔╝██║░░██║╚██████╔╝██║░░██║██║░╚███║██║███████╗███████╗██║░░██║",
            "░╚════╝░╚═╝░░╚═╝░╚═════╝░╚═╝░░╚═╝╚═╝░░╚══╝╚═╝╚══════╝╚══════╝╚═╝░░╚═╝"
        };
        string MainPath;
        bool LogFiles;
        static Regex[] Regices =
        {
            //(C79) [Gokusaishiki (Aya Shachou)] Chikokuma Renko ~Shikomareta Chikan Kekkai~ (Touhou Project)
            //(C79) [Gokusaishiki (Aya Shachou)] Chikokuma Renko ~Shikomareta Chikan Kekkai~ [Touhou Project]
            new Regex(@"^\(.*[^(]\) \[(.*[^(]) \((.*)\)\] (.[^(\[]*)"),
            //[Gokusaishiki (Aya Shachou)] Chikokuma Renko ~Shikomareta Chikan Kekkai~ [Touhou Project]
            new Regex(@"^\[(.*[^(]) \((.*)\)\] (.[^(\[]*)"),
            //[Aya Shachou] Chikokuma Renko ~Shikomareta Chikan Kekkai~ [Touhou Project]
            //(Aya Shachou) Chikokuma Renko ~Shikomareta Chikan Kekkai~ [Touhou Project]
            new Regex(@"^[(\[](.[^\])]*)[)\]] (.[^(\[]*)"),
        };
        DirectoryInfo[] subDirectoryNames;
        List<string> Errors = new List<string>();

        public ComicOrganizer()
        {
            Titulo();
            Console.Write("Do you want to log all files while copying? (y/n)");
            string a = Console.ReadLine();

            LogFiles = a.Equals("y");

            Console.Write("Input the path: ");

            string p = Console.ReadLine();
            MainPath = Path.GetFullPath(p);

        }

        public void Titulo()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(string.Join("\n", Title));
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void StartProgram()
        {
            try
            {
                DirectoryInfo MainDirectory = new DirectoryInfo(MainPath);
                subDirectoryNames = MainDirectory.GetDirectories();
                foreach (DirectoryInfo subDirectory in subDirectoryNames)
                {
                    for (int i = 0; i < Regices.Count(); i++)
                    {
                        Regex rx = Regices[i];
                        if (rx.IsMatch(subDirectory.Name))
                        {
                            int[] idsGroup = rx.GetGroupNumbers();
                            GroupCollection groups = rx.Match(subDirectory.Name).Groups;
                            string newPath = CreatePath(idsGroup, groups);

                            SubDivision();

                            Environment.CurrentDirectory = "/";
                            try
                            {
                                if (!Directory.Exists(newPath))
                                {
                                    Directory.CreateDirectory(newPath);
                                }

                                foreach (FileInfo image in subDirectory.GetFiles())
                                {
                                    image.CopyTo(Path.Combine(newPath, image.Name), true);
                                    image.Delete();
                                    if (LogFiles)
                                    {
                                        Console.ForegroundColor = ConsoleColor.DarkGray;
                                        Console.WriteLine(@"Copying {0}\{1}", newPath, image.Name);
                                        Console.ForegroundColor = ConsoleColor.White;
                                    }
                                }
                                subDirectory.Refresh();
                                subDirectory.Delete(true);
                                SuccessMessage("{0}: -> {1}", subDirectory.FullName, newPath);
                            }
                            catch (DirectoryNotFoundException ex)
                            {
                                ErrorMessage("Please report this error!");
                                ErrorMessage(ex.Message);
                                ErrorMessage(ex.StackTrace);
                                Errors.Add($"ERROR ON: {subDirectory}");
                                
                            }
                            catch (Exception ex)
                            {
                                ErrorMessage("Please report this error!");
                                ErrorMessage(ex.Message);
                                ErrorMessage(ex.StackTrace);
                                Errors.Add($"ERROR ON: {subDirectory}");
                            }
                            break;
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                ErrorMessage("Sorry! Couldn't find a directory with that path");
            }
            catch (Exception ex)
            {
                ErrorMessage("Please report this error!");
                ErrorMessage(ex.Message);
                ErrorMessage(ex.StackTrace);
            }
            Division();
            Console.WriteLine("Task finished!");
            WarningMessage("Success Rate: {0}", ((1 - (Errors.Count / ((subDirectoryNames.Length==0)?1:subDirectoryNames.Length))) * 100) + "");
            WarningMessage("TOTAL ERROR COUNT: {0}", Errors.Count+"");

            foreach (var err in Errors)
            {
                ErrorMessage(err);
            }
        }

        private string CreatePath(int[] idsGroup, GroupCollection groups)
        {
            string p = MainPath;

            string artist = groups[idsGroup[idsGroup.Length - 2]]?.Value;
            string group = groups[1].Value.Equals(artist) ? null : $"(Group) {groups[1].Value}";
            string comicName = groups[idsGroup.Last()].Value;

            p = string.Join("/", p, group, $"(Artist) {artist}", comicName);
            return p;
        }

        public void Division()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string('=', Title.Last().Length));
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void SubDivision(string subTitle = "")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(new string('=', Title.Last().Length));
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void SuccessMessage(string message, params string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message, args);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void WarningMessage(string message, params string[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(message, args);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void ErrorMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
