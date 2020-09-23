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

            ConsoleUtilities.ShowTitle(Title);
            Configure();
            Organize();
        }

        private void Configure()
        {
            Console.Write("Do you want to organize previous comics to? (y/n): ");
            IncludePrevious = Console.ReadLine().Trim().Equals("y");
            while (true)
            {
                Console.Write($"{Environment.NewLine}What's the min number of comics for making a group/artist? (Recommended is 2): ");
                string input = Console.ReadLine().Trim();
                if (int.TryParse(input, out int minNumber))
                {
                    MinNumberOfComics = minNumber;
                    break;
                }
                ConsoleUtilities.ErrorMessage($"{Environment.NewLine}Please write a valid number!");
            }
            while (true)
            {
                Console.Write($"{Environment.NewLine}Input the path (if you're on mac you need to escape it before!): ");
                string input = Console.ReadLine().Trim();
                if (Directory.Exists(input))
                {
                    MainPath = input;
                    break;
                }
                ConsoleUtilities.ErrorMessage($"{Environment.NewLine}Please write a valid path to a directory!");
            }
        }

        public void Organize()
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
            ConsoleUtilities.Division();
            ConsoleUtilities.SuccessMessage("TASK FINISHED!");
            ConsoleUtilities.WarningMessage("{0} organizing {1} directories", (EndTime-StartTime).ToString(), ""+TotalDirectories);
            ConsoleUtilities.WarningMessage("Success Rate: {0}", ((1 - (Errors.Count / ((TotalDirectories==0)?1:TotalDirectories))) * 100) + "");
            ConsoleUtilities.WarningMessage("TOTAL ERROR COUNT: {0}", Errors.Count+"");

            foreach (var err in Errors)
            {
                ConsoleUtilities.ErrorMessage(err);
            }
        }
        /// <summary>
        /// Adds the previous organized comics that are not in an artist directory but are inside a group, to the dictionary to be evaluated and possibly moved to the new location.
        /// </summary>
        private void GetPreviousToDictionary()
        {
            Regex rx = Regices[1];
            foreach (string subDirectory in Directory.EnumerateDirectories(MainPath, "(Group)*"))
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
        /// <summary>
        /// Initializes a <code>List<string[]> with the given key if there isn't one already.</code>
        /// </summary>
        /// <param name="key">The key to be checked.</param>
        private void InitializeKeyIfNotExists(string key)
        {
            if (!ComicGroups.ContainsKey(key))
            {
                ComicGroups.Add(key, new List<string[]>());
            }
        }
        /// <summary>
        /// Moves the comics inside the directory key if the count equal to the number of comics. In theory the count should never be
        /// greater than <code>MinNumberOfComics</code> but just to be safe.
        /// </summary>
        /// <param name="key">The key of the dictionary.</param>
        private void MoveComicsIfEqualsMinNumberOfComics(string key)
        {
            if (ComicGroups[key].Count >= MinNumberOfComics)
            {
                foreach (var comic in ComicGroups[key])
                {
                    MoveDirectory(comic[0], comic[1]);
                }
                ComicGroups.Remove(key);
            }
        }

        /// <summary>
        /// Moves the direcotry in <paramref name="source"/> to <paramref name="destiny"/>
        /// </summary>
        /// <param name="source">The path of the directory to move all the files to.</param>
        /// <param name="destiny">The path that the directory will be moved to</param>
        private void MoveDirectory(string source, string destiny)
        {
            ConsoleUtilities.SubDivision();
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
                        ConsoleUtilities.LogImage(file, destiny, out previousWidth);
                        MoveImage(file, destiny);
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
                    return;
                }
            }
            Directory.Delete(source, true);
            ConsoleUtilities.SuccessMessage("Succesfully moved to:\n{0}", destiny);
        }
        /// <summary>
        /// Moves the <paramref name="image"/> to the specified <paramref name="newPath"/>. It doesn't have to be an image, it can be a file too.
        /// If there's already a file in the specified path it will replace it.
        /// </summary>
        /// <param name="image">The path of the image to move, doesn't needs to be an image, can be any file.</param>
        /// <param name="newPath">The path to move <paramref name="image"/></param>
        private void MoveImage(string image, string newPath)
        {
            try
            {
                File.Copy(image, Path.Combine(newPath, Path.GetFileName(image)), true);
                File.Delete(image);
            }
            catch (Exception ex)
            {
                ConsoleUtilities.ErrorMessage($"Error Copying {image}");
                throw ex;
            }
        }
        /// <summary>
        /// Get's the group, artist and the comic name obtained from the regex.
        /// </summary>
        /// <param name="idsGroup">The array of id's from the regex.</param>
        /// <param name="gc">The group collection from the regex.</param>
        /// <returns>A Tuple with the elements in this order: Group name (this can be null if there is no group), Artist and the Comic name, this last ones will never be null.</returns>
        private (string group, string artist, string comicName) GetComicInfo(int[] idsGroup, GroupCollection gc)
        {
            string artist = gc[idsGroup[idsGroup.Length - 2]]?.Value.Trim();
            string group = gc[1].Value.Equals(artist) ? null : gc[1].Value.Trim();
            string comicName = gc[idsGroup.Last()].Value.Trim();

            return (group, artist, comicName);
        }
        /// <summary>
        /// Creates the possible paths were the comic will be moved potentially.
        /// </summary>
        /// <param name="group">It's the group name. This value may be null, in which case MainPath is used instead internally.</param>
        /// <param name="artist">It's the artist name.</param>
        /// <returns>A tuple with the path to the directory of the group (or MainPath, if <paramref name="group"/> was null or empty) and the path to the directory of the artis</returns>
        private (string groupPath, string artistPath) CreatePaths(string group, string artist)
        {
            string gp = (string.IsNullOrEmpty(group))? MainPath : Path.Combine(MainPath, $"(Group) {group}");
            string ap = Path.Combine(gp, $"(Artist) {artist}");

            return (gp, ap);
        }
    }
}
