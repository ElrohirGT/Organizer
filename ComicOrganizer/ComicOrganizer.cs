using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using Organizer.Utilities;

namespace Organizer
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
        bool IncludePrevious;
        DateTime StartTime;
        DateTime EndTime;
        static Regex[] Regices =
        {
            //(C79) [Gokusaishiki (Aya Shachou)] Chikokuma Renko ~Shikomareta Chikan Kekkai~ (Touhou Project)
            new Regex(@"^\(.*[^(]\) \[(.*[^(]) \((.*)\)\] (.[^(\[]*)"),

            //[Gokusaishiki (Aya Shachou)] Chikokuma Renko ~Shikomareta Chikan Kekkai~ [Touhou Project]
            //GetPreviousToDictionary uses this to add directories in group but not artist to the ComicGroups dictionary
            new Regex(@"^\[(.*[^(]) \((.*)\)\] (.[^(\[]*)"),

            //[Aya Shachou] Chikokuma Renko ~Shikomareta Chikan Kekkai~ [Touhou Project]
            //(Aya Shachou) Chikokuma Renko ~Shikomareta Chikan Kekkai~ [Touhou Project]
            new Regex(@"^[(\[](.[^\])]*)[)\]] (.[^(\[]*)"),
        };
        List<string> Errors = new List<string>();
        Dictionary<string, List<string[]>> ComicGroups = new Dictionary<string, List<string[]>>();

        int MinNumberOfComics = 2;
        int TotalDirectories = 0;

        public ComicOrganizer()
        {
            Titulo();
            string answer;

            while (true)
            {
                Console.Write("What's the min number of comics for making a group/artist? (Recommended is 2): ");
                answer = Console.ReadLine().Trim();
                if (int.TryParse(answer, out int minNumber) && minNumber > 0)
                {
                    MinNumberOfComics = minNumber;
                    break;
                }
                ConsoleUtilities.ErrorMessage("Please write a valid number!");
            }

            Console.Write("Do you want to organize previous comics too? (y/n): ");
            answer = Console.ReadLine();
            IncludePrevious = answer.Equals("y");

            while (true)
            {
                Console.Write("Input the path (you don't need to escape it): ");
                answer = Console.ReadLine().Trim();
                if (Directory.Exists(answer))
                {
                    MainPath = Path.GetFullPath(answer);
                    break;
                }
                ConsoleUtilities.ErrorMessage("That path doesn't exists!");
            }
        }

        public void Titulo()
        {
            ConsoleUtilities.Division(Title[0].Length);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(string.Join("\n", Title));
            Console.ForegroundColor = ConsoleColor.White;
            ConsoleUtilities.Division(Title.Last().Length);
            ConsoleUtilities.WarningMessage("Running version: {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }

        public void StartProgram()
        {
            StartTime = DateTime.Now;
            try
            {
                Environment.CurrentDirectory = "/";
                if (IncludePrevious)
                {
                    GetPreviousToDictionary();
                }
                foreach (string subDirectory in Directory.EnumerateDirectories(MainPath))
                {
                    string subDirectoryName = Path.GetFileName(subDirectory);
                    //If it isn't a directory with files already organized, organize it
                    if (!new Regex(@"^\(Artist\).*|^\(Group\).*").IsMatch(subDirectoryName))
                    {
                        for (int i = 0; i < Regices.Count(); i++)
                        {
                            Regex rx = Regices[i];
                            if (rx.IsMatch(subDirectoryName))
                            {
                                TotalDirectories++;
                                Environment.CurrentDirectory = "/";
                                int[] idsGroup = rx.GetGroupNumbers();
                                (string groupName, string artistName, string comicName) = GetComicInfo(idsGroup, rx.Match(subDirectoryName).Groups);
                                (string groupPath, string artistPath) = CreatePaths(groupName, artistName);

                                InitializeKeyIfNotExists(groupPath);
                                InitializeKeyIfNotExists(artistPath);

                                if (groupPath.Equals(MainPath))
                                {
                                    if (Directory.Exists(artistPath))
                                    {
                                        MoveDirectory(subDirectory, Path.Combine(artistPath, comicName));
                                        break;
                                    }
                                    ComicGroups[artistPath].Add(new string[2] { subDirectory, Path.Combine(artistPath, comicName)});
                                    MoveComicsIfEqualsMinNumberOfComics(artistPath);
                                    break;
                                }

                                string destiny = Path.Combine(groupPath, $"[{groupName} ({artistName})] {comicName}");

                                if (Directory.Exists(groupPath))
                                {
                                    if (Directory.Exists(artistPath))
                                    {
                                        MoveDirectory(subDirectory, Path.Combine(artistPath, comicName));
                                        break;
                                    }
                                    MoveDirectory(subDirectory, destiny);

                                    ComicGroups[artistPath].Add(new string[2] { destiny, Path.Combine(artistPath, comicName) });
                                    MoveComicsIfEqualsMinNumberOfComics(artistPath);
                                    break;
                                }

                                ComicGroups[groupPath].Add(new string[2] { subDirectory, destiny });
                                ComicGroups[artistPath].Add(new string[2] { destiny, Path.Combine(artistPath, comicName) });

                                if (ComicGroups[groupPath].Count == MinNumberOfComics)
                                {
                                    foreach (var comic in ComicGroups[groupPath])
                                    {
                                        MoveDirectory(comic[0], comic[1]);
                                    }
                                    ComicGroups.Remove(groupPath);
                                    MoveComicsIfEqualsMinNumberOfComics(artistPath);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleUtilities.ErrorMessage("Sorry an error ocurred!");
                ConsoleUtilities.ErrorMessage(ex.Message);
            }
            EndTime = DateTime.Now;
            ConsoleUtilities.Division(Title.Last().Length);
            ConsoleUtilities.SuccessMessage("TASK FINISHED!");
            ConsoleUtilities.WarningMessage("{0} organizing {1} directories", (EndTime-StartTime).ToString(), ""+TotalDirectories);
            ConsoleUtilities.WarningMessage("Success Rate: {0}", ((1 - (Errors.Count / ((TotalDirectories==0)?1:TotalDirectories))) * 100) + "");
            ConsoleUtilities.WarningMessage("TOTAL ERROR COUNT: {0}", Errors.Count+"");

            foreach (var err in Errors)
            {
                ConsoleUtilities.ErrorMessage(err);
            }
        }

        private void GetPreviousToDictionary()
        {
            Regex rx = Regices[1];
            foreach (string subDirectory in Directory.EnumerateDirectories(MainPath))
            {
                foreach (string dir in Directory.EnumerateDirectories(subDirectory, "[*(*)]*"))
                {
                    string name = Path.GetFileName(dir);
                    int[] idsGroup = rx.GetGroupNumbers();
                    (string groupName, string artistName, string comicName) = GetComicInfo(idsGroup, rx.Match(name).Groups);
                    (_, string artistPath) = CreatePaths(groupName, artistName);

                    InitializeKeyIfNotExists(artistPath);
                    ComicGroups[artistPath].Add(new string[2] { dir, Path.Combine(artistPath, comicName) });
                    MoveComicsIfEqualsMinNumberOfComics(artistPath);
                }
            }
        }

        private void InitializeKeyIfNotExists(string key)
        {
            if (!ComicGroups.ContainsKey(key))
            {
                ComicGroups.Add(key, new List<string[]>());
            }
        }

        private void MoveComicsIfEqualsMinNumberOfComics(string key)
        {
            if (ComicGroups[key].Count == MinNumberOfComics)
            {
                foreach (var comic in ComicGroups[key])
                {
                    MoveDirectory(comic[0], comic[1]);
                }
                ComicGroups.Remove(key);
            }
        }

        private void MoveDirectory(string source, string destiny)
        {
            ConsoleUtilities.SubDivision(Title.Last().Length);
            ConsoleUtilities.WarningMessage("MOVING: {0}", source);
            if (!Directory.Exists(destiny))
            {
                Directory.CreateDirectory(destiny);
            }
            int maxTries = 5;
            Console.WriteLine();
            for (int i = 0; i < maxTries; i++)
            {
                try
                {
                    int previousWidth = 0;
                    foreach (string file in Directory.EnumerateFiles(source))
                    {
                        ConsoleUtilities.ClearPreviousLogImage(previousWidth);
                        MoveImage(file, destiny, out int loggedStringWidth);
                        previousWidth = loggedStringWidth;
                    }
                    break;
                }
                catch (Exception ex)
                {
                    if (i<4)
                    {
                        ConsoleUtilities.WarningMessage($"An error ocurred. Trying again {i+1}/{maxTries}.");
                        Console.WriteLine();
                        continue;
                    }
                    ConsoleUtilities.ErrorMessage($"Sorry an Error ocurred trying to move the directory:\n{source}\nTo:\n{destiny}!");
                    ConsoleUtilities.ErrorMessage(ex.Message);
                    Errors.Add($"ERROR ON: {source}");
                }
            }
            Directory.Delete(source, true);
            ConsoleUtilities.SuccessMessage("Succesfully moved to:\n{0}", destiny);
        }

        private void MoveImage(string image, string newPath, out int loggedStringWidth)
        {
            loggedStringWidth = 0;
            try
            {
                ConsoleUtilities.LogImage(image, newPath, out string loggedString);
                loggedStringWidth = loggedString.Length;
                File.Copy(image, Path.Combine(newPath, Path.GetFileName(image)), true);
                File.Delete(image);
            }
            catch (Exception ex)
            {
                ConsoleUtilities.ErrorMessage($"Error Copying {image}");
                throw ex;
            }
        }

        private (string group, string artist, string comicName) GetComicInfo(int[] idsGroup, GroupCollection gc)
        {
            string artist = gc[idsGroup[idsGroup.Length - 2]]?.Value.Trim();
            string group = gc[1].Value.Equals(artist) ? null : gc[1].Value.Trim();
            string comicName = gc[idsGroup.Last()].Value.Trim();

            return (group, artist, comicName);
        }

        private (string groupPath, string artistPath) CreatePaths(string group, string artist)
        {
            string gp = (string.IsNullOrEmpty(group))? MainPath : Path.Combine(MainPath, $"(Group) {group}");
            string ap = Path.Combine(gp, $"(Artist) {artist}");

            return (gp, ap);
        }
    }
}
