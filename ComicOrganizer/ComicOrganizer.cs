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
        bool IncludePrevious;
        static Regex[] Regices =
        {
            //(C79) [Gokusaishiki (Aya Shachou)] Chikokuma Renko ~Shikomareta Chikan Kekkai~ (Touhou Project)
            new Regex(@"^\(.*[^(]\) \[(.*[^(]) \((.*)\)\] (.[^(\[]*)"),

            //[Gokusaishiki (Aya Shachou)] Chikokuma Renko ~Shikomareta Chikan Kekkai~ [Touhou Project]
            new Regex(@"^\[(.*[^(]) \((.*)\)\] (.[^(\[]*)"),

            //[Aya Shachou] Chikokuma Renko ~Shikomareta Chikan Kekkai~ [Touhou Project]
            //(Aya Shachou) Chikokuma Renko ~Shikomareta Chikan Kekkai~ [Touhou Project]
            new Regex(@"^[(\[](.[^\])]*)[)\]] (.[^(\[]*)"),
        };
        List<string> Errors = new List<string>();
        Dictionary<string, List<Tuple<DirectoryInfo, string>>> ComicGroups = new Dictionary<string, List<Tuple<DirectoryInfo, string>>>();

        int MinNumberOfComics = 2;
        int SuccesCount = 0;

        public ComicOrganizer()
        {
            Titulo();
            Console.Write("Do you want to log all files while copying? (y/n): ");
            string answer = Console.ReadLine();
            LogFiles = answer.Equals("y");

            while (true)
            {
                Console.Write("What's the min number of comics for making a group/artist? (Recommended is 2): ");
                answer = Console.ReadLine();
                if (int.TryParse(answer, out int minNumber) && minNumber > 0)
                {
                    MinNumberOfComics = minNumber;
                    break;
                }
                ErrorMessage("Please write a valid number!");
            }

            Console.Write("Do you want to organize previous comics too? (y/n): ");
            answer = Console.ReadLine();
            IncludePrevious = answer.Equals("y");

            Console.Write("Input the path: ");
            answer = Console.ReadLine();
            MainPath = Path.GetFullPath(answer);

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
                Environment.CurrentDirectory = "/";
                DirectoryInfo MainDirectory = new DirectoryInfo(MainPath);
                if (IncludePrevious)
                {
                    GetPreviousToMainPath();
                }
                foreach (DirectoryInfo subDirectory in MainDirectory.EnumerateDirectories())
                {
                    //If it isn't a directory with files already organized, organize it
                    if (!new Regex(@"^\(Artist\).*|^\(Group\).*").IsMatch(subDirectory.Name))
                    {
                        for (int i = 0; i < Regices.Count(); i++)
                        {
                            Regex rx = Regices[i];
                            if (rx.IsMatch(subDirectory.Name))
                            {
                                Environment.CurrentDirectory = "/";
                                int[] idsGroup = rx.GetGroupNumbers();
                                (string groupName, string artistName, string comicName) = GetComicInfo(idsGroup, rx.Match(subDirectory.Name).Groups);
                                (string groupPath, string artistPath) = CreatePaths(groupName, artistName, comicName);

                                InitializeKeyIfNotExists(groupPath);
                                InitializeKeyIfNotExists(artistPath);

                                if (groupPath.Equals(MainPath))
                                {
                                    if (Directory.Exists(artistPath))
                                    {
                                        MoveDirectory(subDirectory, Path.Combine(artistPath, comicName));
                                        break;
                                    }
                                    ComicGroups[artistPath].Add(Tuple.Create(subDirectory, Path.Combine(artistPath, comicName)));
                                    MoveComics(artistPath);
                                    break;
                                }

                                string destiny = Path.Combine(groupPath, $"[{groupName} ({artistName})] {comicName}");
                                DirectoryInfo groupDirectoryInfo = new DirectoryInfo(destiny);

                                if (Directory.Exists(groupPath))
                                {
                                    if (Directory.Exists(artistPath))
                                    {
                                        MoveDirectory(subDirectory, Path.Combine(artistPath, comicName));
                                        break;
                                    }
                                    MoveDirectory(subDirectory, destiny);

                                    ComicGroups[artistPath].Add(Tuple.Create(groupDirectoryInfo, Path.Combine(artistPath, comicName)));
                                    MoveComics(artistPath);
                                    break;
                                }

                                ComicGroups[groupPath].Add(Tuple.Create(subDirectory, destiny));
                                ComicGroups[artistPath].Add(Tuple.Create(groupDirectoryInfo, Path.Combine(artistPath, comicName)));

                                if (ComicGroups[groupPath].Count == MinNumberOfComics)
                                {
                                    foreach (var comic in ComicGroups[groupPath])
                                    {
                                        MoveDirectory(comic.Item1, comic.Item2);
                                    }
                                    ComicGroups.Remove(groupPath);
                                    MoveComics(artistPath);
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
                ErrorMessage("TASK FINISHED");
                return;
            }
            catch (Exception ex)
            {
                ErrorMessage("Sorry an error ocurred!");
                ErrorMessage(ex.Message);
                ErrorMessage(ex.StackTrace);
            }
            Division();
            SuccessMessage("TASK FINISHED!");
            WarningMessage("Success Rate: {0}", ((1 - (Errors.Count / ((SuccesCount==0)?1:SuccesCount))) * 100) + "");
            WarningMessage("TOTAL ERROR COUNT: {0}", Errors.Count+"");

            foreach (var err in Errors)
            {
                ErrorMessage(err);
            }
        }

        private void GetPreviousToMainPath()
        {
            foreach (string subDirectory in Directory.EnumerateDirectories(MainPath))
            {
                var dirs = Directory.GetDirectories(subDirectory, "[*(*)]*");
                foreach (var dir in dirs)
                {
                    string name = dir.Split(Path.PathSeparator).Last();
                    MoveDirectory(new DirectoryInfo(dir), Path.Combine(MainPath, name));
                }
            }
        }

        private void InitializeKeyIfNotExists(string key)
        {
            if (!ComicGroups.ContainsKey(key))
            {
                ComicGroups.Add(key, new List<Tuple<DirectoryInfo, string>>());
            }
        }

        private void MoveComics(string key)
        {
            if (ComicGroups[key].Count == MinNumberOfComics)
            {
                foreach (var comic in ComicGroups[key])
                {
                    MoveDirectory(comic.Item1, comic.Item2);
                }
                ComicGroups.Remove(key);
            }
        }

        private void MoveDirectory(DirectoryInfo source, string destiny)
        {
            SubDivision();
            try
            {
                if (!Directory.Exists(destiny))
                {
                    Directory.CreateDirectory(destiny);
                }
                foreach (string file in Directory.EnumerateFiles(source.FullName))
                {
                    MoveImage(file, destiny);
                }
                source.Delete(true);
                source.Refresh();
                SuccessMessage("{0} Succesfully moved to:\n{1}", source.Name, destiny);
                SuccesCount++;
            }
            catch (DirectoryNotFoundException ex)
            {
                ErrorMessage($"Sorry an Error ocurred trying to move the directories\n{source.FullName}\nto\n{destiny}!");
                ErrorMessage(ex.Message);
                ErrorMessage(ex.StackTrace);
                Errors.Add($"ERROR ON: {source.FullName}");
            }
            catch (Exception ex)
            {
                ErrorMessage("Sorry an error ocurred!");
                ErrorMessage(ex.Message);
                ErrorMessage(ex.StackTrace);
                Errors.Add($"ERROR ON: {source.FullName}");
            }
        }

        private void MoveImage(string image, string newPath)
        {
            File.Copy(image, Path.Combine(newPath, Path.GetFileName(image)), true);
            File.Delete(image);
            if (LogFiles)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(@"Copying {0}\{1}", newPath, Path.GetFileName(image));
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private (string group, string artist, string comicName) GetComicInfo(int[] idsGroup, GroupCollection gc)
        {
            string artist = gc[idsGroup[idsGroup.Length - 2]]?.Value;
            string group = gc[1].Value.Equals(artist) ? null : gc[1].Value;
            string comicName = gc[idsGroup.Last()].Value;

            return (group, artist, comicName);
        }

        private (string groupPath, string artistPath) CreatePaths(string group, string artist, string comicName)
        {
            string gp = (string.IsNullOrEmpty(group))? MainPath : Path.Combine(MainPath, $"(Group) {group}");
            string ap = Path.Combine(gp, $"(Artist) {artist}");

            return (gp, ap);
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
