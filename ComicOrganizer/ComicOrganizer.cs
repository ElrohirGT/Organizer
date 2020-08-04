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
            Console.Write("Do you want to log all files while copying? (y/n): ");
            string a = Console.ReadLine();

            LogFiles = a.Equals("y");

            Console.Write("Input the path: ");

            string p = Console.ReadLine();
            MainPath = Path.GetFullPath(p);

        }

        public void Titulo()
        {
            Division();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(string.Join("\n", Title));
            Console.ForegroundColor = ConsoleColor.White;
            Division();
        }

        public void StartProgram()
        {
            try
            {
                DirectoryInfo MainDirectory = new DirectoryInfo(MainPath);
                foreach (DirectoryInfo subDirectory in MainDirectory.GetDirectories())
                {
                    //If it isn't a directory with files already organized, organize it
                    if (!new Regex(@"^\(Artist\).*|^\(Group\).*").IsMatch(subDirectory.Name))
                    {
                        for (int i = 0; i < Regices.Count(); i++)
                        {
                            Regex rx = Regices[i];
                            if (rx.IsMatch(subDirectory.Name))
                            {
                                int[] idsGroup = rx.GetGroupNumbers();
                                (string group, string artist, string name) = GetComicInfo(idsGroup, rx.Match(subDirectory.Name).Groups);
                                string newPath = CreatePath(group, artist, name);

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
                                        MoveImage(image, newPath);
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

        private void MoveImage(FileInfo image, string newPath)
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

        private (string group, string artist, string comicName) GetComicInfo(int[] idsGroup, GroupCollection gc)
        {
            string artist = gc[idsGroup[idsGroup.Length - 2]]?.Value;
            string group = gc[1].Value.Equals(artist) ? null : $"(Group) {gc[1].Value}";
            string comicName = gc[idsGroup.Last()].Value;

            return (group, artist, comicName);
        }

        private string CreatePath(string group, string artist, string comicName)
        {
            return string.Join("/", MainPath, group, $"(Artist) {artist}", comicName);
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
