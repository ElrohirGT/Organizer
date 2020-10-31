﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Organizer.Utilities;

namespace Organizer
{
    public class ComicOrganizer
    {
        #region Fields
        string[] _Title =
        {
            "░█████╗░██████╗░░██████╗░░█████╗░███╗░░██╗██╗███████╗███████╗██████╗░",
            "██╔══██╗██╔══██╗██╔════╝░██╔══██╗████╗░██║██║╚════██║██╔════╝██╔══██╗",
            "██║░░██║██████╔╝██║░░██╗░███████║██╔██╗██║██║░░███╔═╝█████╗░░██████╔╝",
            "██║░░██║██╔══██╗██║░░╚██╗██╔══██║██║╚████║██║██╔══╝░░██╔══╝░░██╔══██╗",
            "╚█████╔╝██║░░██║╚██████╔╝██║░░██║██║░╚███║██║███████╗███████╗██║░░██║",
            "░╚════╝░╚═╝░░╚═╝░╚═════╝░╚═╝░░╚═╝╚═╝░░╚══╝╚═╝╚══════╝╚══════╝╚═╝░░╚═╝"
        };
        string _MainPath;
        bool _DoGroups;
        DateTime _StartTime;
        DateTime _EndTime;
        static Regex[] _Regices =
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
        static Regex _AlreadyOrganizedRegex = new Regex(@"^\(Artist\).*|^\(Group\).*");
        List<string> _Errors = new List<string>();

        /// <summary>
        /// A Dictionary of comics keyed by their new path to move
        /// </summary>
        Dictionary<string, List<ComicInfo>> _ComicsNewPaths = new Dictionary<string, List<ComicInfo>>();

        int _MinNumberOfComics = 2;
        int _TotalDirectories = 0;
        #endregion

        public ComicOrganizer() { }

        public Task StartApp()
        {
            ConsoleUtilities.ShowTitle(_Title);
            Configure();
            return OrganizeAsync();
        }

        void Configure()
        {
            Console.Write("Do you want to use groups? (y/n): ");
            _DoGroups = Console.ReadLine().Trim().Equals("y");
            while (true)
            {
                Console.Write("What's the min number of comics for making a group/artist? (Recommended is 2): ");
                string input = Console.ReadLine().Trim();
                if (int.TryParse(input, out int minNumber))
                {
                    _MinNumberOfComics = minNumber;
                    break;
                }
                ConsoleUtilities.ErrorMessage("Please write a valid number!");
            }
            while (true)
            {
                Console.Write("Input the path (if you're on mac you need to escape it before!): ");
                string input = Console.ReadLine().Trim();
                if (Directory.Exists(input))
                {
                    _MainPath = input;
                    break;
                }
                ConsoleUtilities.ErrorMessage("Please write a valid path to a directory!");
            }
        }

        async Task OrganizeAsync()
        {
            _StartTime = DateTime.Now;
            PopulateDictionary();
            CleanDictionary();

            await MoveComics();

            _EndTime = DateTime.Now;
            ConsoleUtilities.Division();
            ConsoleUtilities.SuccessMessage("TASK FINISHED!");
            ConsoleUtilities.WarningMessage("{0} organizing {1} directories", (_EndTime - _StartTime).ToString(), "" + _TotalDirectories);
            ConsoleUtilities.WarningMessage("Success Rate: {0}", ((1 - (_Errors.Count / ((_TotalDirectories == 0) ? 1 : _TotalDirectories))) * 100) + "");
            ConsoleUtilities.WarningMessage("TOTAL ERROR COUNT: {0}", _Errors.Count + "");
            foreach (var errorMessage in _Errors)
            {
                ConsoleUtilities.ErrorMessage(errorMessage);
            }
        }

        /// <summary>
        /// It deletes every key whose list count is 0.
        /// </summary>
        void CleanDictionary()
        {
            string[] keys = new string[_ComicsNewPaths.Count];
            _ComicsNewPaths.Keys.CopyTo(keys, 0);
            for (int i = 0; i < keys.Length; i++)
            {
                if (_ComicsNewPaths[keys[i]].Count < _MinNumberOfComics)
                {
                    _ComicsNewPaths.Remove(keys[i]);
                }
            }
        }

        /// <summary>
        /// Populates the <c>_ComicsNewPaths</c> dictionary with all the comics that need to be moved.
        /// It does so in a way that all comics that need to be moved appear just once in the entire dictionary,
        /// this way we can move all comics asynchronously.
        /// </summary>
        void PopulateDictionary()
        {
            try
            {
                Environment.CurrentDirectory = Path.GetPathRoot(Environment.SystemDirectory);
                if (_DoGroups)
                    GetPreviousToDictionary();

                foreach (var subDirectoryPath in Directory.EnumerateDirectories(_MainPath))
                {
                    string subDirectoryName = Path.GetFileName(subDirectoryPath);

                    //If it isn't a directory with files already organized, organize it
                    if (_AlreadyOrganizedRegex.IsMatch(subDirectoryName))
                        continue;

                    for (int i = 0; i < _Regices.Count(); i++)
                    {
                        Regex regex = _Regices[i];
                        if (!regex.IsMatch(subDirectoryName))
                            continue;

                        int[] idsGroup = regex.GetGroupNumbers();
                        (string groupName, string artistName, string comicName) =
                            GetComicInfo(idsGroup, regex.Match(subDirectoryName).Groups);

                        (string groupPath, string artistPath) = CreatePaths(groupName, artistName);

                        string artistFinalPath = Path.Combine(artistPath, comicName);
                        string groupFinalPath = Path.Combine(groupPath, comicName);

                        InitializeKeyIfNotExists(artistPath);
                        if (!_DoGroups)
                        {
                            _ComicsNewPaths[artistPath].Add(new ComicInfo(subDirectoryPath, artistFinalPath, groupName, artistName));
                            break;
                        }

                        InitializeKeyIfNotExists(groupPath);

                        //Adds the artis and if the count of comics of that artist is greater than the minimum required
                        //eliminates any reference to any of the comics from the group path.
                       _ComicsNewPaths[artistPath].Add(new ComicInfo(subDirectoryPath, artistFinalPath, groupName, artistName));
                        if (_ComicsNewPaths[artistPath].Count >= _MinNumberOfComics)
                        {
                            //Removes all references to previous comics that belonged to the same artist and group,
                            //which makes it safe to move all comics asynchonously
                            if (_ComicsNewPaths.TryGetValue(groupPath, out List<ComicInfo> listOfPaths))
                                listOfPaths.RemoveAll(info => info.ArtistName.Equals(artistName));

                            //If it entered here it means that there is no possibility of it having a groupPath.
                            break;
                        }
                        _ComicsNewPaths[groupPath].Add(new ComicInfo(subDirectoryPath, groupFinalPath, groupName, artistName));
                    }
                }
            }
            catch (Exception e)
            {
                ConsoleUtilities.ErrorMessage("An error ocurred trying get info from all comics...");
                Console.Error.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Adds the previous organized comics that are not in an artist directory but are inside a group,
        /// to the dictionary to be evaluated and possibly moved to a new location.
        /// </summary>
        void GetPreviousToDictionary()
        {
            Regex rx = _Regices[1];
            foreach (string subDirectory in Directory.EnumerateDirectories(_MainPath, "(Group)*"))
            {
                foreach (string dir in Directory.EnumerateDirectories(subDirectory, "[*(*)]*"))
                {
                    string name = Path.GetFileName(dir);
                    int[] idsGroup = rx.GetGroupNumbers();
                    (string groupName, string artistName, string comicName) = GetComicInfo(idsGroup, rx.Match(name).Groups);
                    (_, string artistPath) = CreatePaths(groupName, artistName);

                    InitializeKeyIfNotExists(artistPath);
                    _ComicsNewPaths[artistPath].Add(new ComicInfo(dir, Path.Combine(artistPath, comicName), groupName, artistName));
                }
            }
        }
        /// <summary>
        /// Initializes a <code>List<string[]> with the given key if there isn't one already.</code>
        /// </summary>
        /// <param name="key">The key to be checked.</param>
        void InitializeKeyIfNotExists(string key)
        {
            if (!_ComicsNewPaths.ContainsKey(key))
            {
                _ComicsNewPaths.Add(key, new List<ComicInfo>());
            }
        }

        /// <summary>
        /// Get's the group, artist and the comic name obtained from the regex.
        /// </summary>
        /// <param name="idsGroup">The array of id's from the regex.</param>
        /// <param name="gc">The group collection from the regex.</param>
        /// <returns>A Tuple with the elements in this order: Group name (this can be null if there is no group),
        /// Artist and the Comic name, this two last ones will never be null.</returns>
        (string group, string artist, string comicName) GetComicInfo(int[] idsGroup, GroupCollection gc)
        {
            //gc[0] is the first group in regex, so it's the entire string, the values start on gc[1].
            //The artist name will sometaimes be the gc[1] but it will always be
            //the second last element in the groupCollection, that's why we use ^2.
            string artistName = gc[^2].Value.Trim();
            string groupName = gc[1].Value.Equals(artistName) ? null : gc[1].Value.Trim();
            string comicName = gc[idsGroup.Last()].Value.Trim();

            return (groupName, artistName, comicName);
        }
        /// <summary>
        /// Creates the possible paths were the comic will be moved potentially.
        /// </summary>
        /// <param name="group">It's the group name. This value may be null, in which case MainPath is used instead internally.</param>
        /// <param name="artist">It's the artist name.</param>
        /// <returns>A tuple with the path to the directory of the group
        /// (or MainPath, if <paramref name="group"/> was null or empty)
        /// and the path to the directory of the artist</returns>
        (string groupPath, string artistPath) CreatePaths(string group, string artist)
        {
            string groupPath = (string.IsNullOrEmpty(group)) ? _MainPath : Path.Combine(_MainPath, $"(Group) {group}");
            string artistPath = Path.Combine(groupPath, $"(Artist) {artist}");
            if (!_DoGroups)
            {
                artistPath = Path.Combine(_MainPath, $"(Artist) {artist}");
            }

            return (groupPath, artistPath);
        }

        /// <summary>
        /// Moves all comics from the dictionary to their respective paths in an async manner.
        /// </summary>
        /// <returns>Task that completes when all comics are moved.</returns>
        async Task MoveComics()
        {
            List<Task> comicsToMove = new List<Task>();
            foreach (var paths in _ComicsNewPaths.Values)
            {
                foreach (var comicInfo in paths)
                {
                    comicsToMove.Add(MoveComic(comicInfo.SourcePath, comicInfo.DestinyPath));
                }
            }
            await Task.WhenAll(comicsToMove.ToArray());
        }

        /// <summary>
        /// Moves a single comic in an async manner.
        /// </summary>
        /// <param name="source">The source directory.</param>
        /// <param name="destiny">The destiny directory.</param>
        /// <returns>A Task that completes when all images of the comics are moved.</returns>
        async Task MoveComic(string source, string destiny)
        {
            List<Task> imagesToMove = new List<Task>();
            Directory.CreateDirectory(destiny);

            try
            {
                foreach (var imagePath in Directory.EnumerateFiles(source))
                {
                    imagesToMove.Add(MoveImage(imagePath, destiny));
                }

                await Task.WhenAll(imagesToMove.ToArray());
                Directory.Delete(source);
                ConsoleUtilities.SubDivision();
                ConsoleUtilities.SuccessMessage(
                    "{0}{2}Moved to:{2}{1}",
                    source, destiny, Environment.NewLine
                );
            }
            catch (Exception)
            {
                ConsoleUtilities.SubDivision();
                ConsoleUtilities.ErrorMessage(
                    "Sorry an error ocurred trying to copy {0}{2}to{2}{1}",
                    source, destiny, Environment.NewLine
                );
                _Errors.Add($"Error on: {source}");
            }
            finally
            {
                _TotalDirectories++;
            }
        }

        /// <summary>
        /// Moves the <paramref name="image"/> to the specified <paramref name="newPath"/>.
        /// It doesn't have to be an image, it can be a file too.
        /// If there's already a file with the same name and extension in the specified path it will replace it.
        /// </summary>
        /// <param name="image">The path of the image to move, doesn't needs to be an image, can be any file.</param>
        /// <param name="newPath">The path to move <paramref name="image"/></param>
        Task MoveImage(string image, string newPath)
        {
            try
            {
                //throw new Exception("Simulated Error");
                File.Copy(image, Path.Combine(newPath, Path.GetFileName(image)), true);
                File.Delete(image);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Synchronous Code
        //public void Organize()
        //{
        //    _StartTime = DateTime.Now;
        //    try
        //    {
        //        Environment.CurrentDirectory = "/";
        //        if (_DoGroups)
        //        {
        //            GetPreviousToDictionary();
        //        }
        //        foreach (string subDirectory in Directory.EnumerateDirectories(_MainPath))
        //        {
        //            string subDirectoryName = Path.GetFileName(subDirectory);
        //            //If it isn't a directory with files already organized, organize it
        //            if (!new Regex(@"^\(Artist\).*|^\(Group\).*").IsMatch(subDirectoryName))
        //            {
        //                for (int i = 0; i < _Regices.Count(); i++)
        //                {
        //                    Regex rx = _Regices[i];
        //                    if (rx.IsMatch(subDirectoryName))
        //                    {
        //                        Environment.CurrentDirectory = "/";
        //                        int[] idsGroup = rx.GetGroupNumbers();
        //                        (string groupName, string artistName, string comicName) = GetComicInfo(idsGroup, rx.Match(subDirectoryName).Groups);
        //                        (string groupPath, string artistPath) = CreatePaths(groupName, artistName);

        //                        InitializeKeyIfNotExists(groupPath);
        //                        InitializeKeyIfNotExists(artistPath);

        //                        if (groupPath.Equals(_MainPath))
        //                        {
        //                            if (Directory.Exists(artistPath))
        //                            {
        //                                MoveDirectory(subDirectory, Path.Combine(artistPath, comicName));
        //                                break;
        //                            }
        //                            _ComicsNewPaths[artistPath].Add(new string[2] { subDirectory, Path.Combine(artistPath, comicName)});
        //                            MoveComicsIfEqualsMinNumberOfComics(artistPath);
        //                            break;
        //                        }

        //                        string destiny = Path.Combine(groupPath, $"[{groupName} ({artistName})] {comicName}");

        //                        if (Directory.Exists(groupPath))
        //                        {
        //                            if (Directory.Exists(artistPath))
        //                            {
        //                                MoveDirectory(subDirectory, Path.Combine(artistPath, comicName));
        //                                break;
        //                            }
        //                            MoveDirectory(subDirectory, destiny);

        //                            _ComicsNewPaths[artistPath].Add(new string[2] { destiny, Path.Combine(artistPath, comicName) });
        //                            MoveComicsIfEqualsMinNumberOfComics(artistPath);
        //                            break;
        //                        }

        //                        _ComicsNewPaths[groupPath].Add(new string[2] { subDirectory, destiny });
        //                        _ComicsNewPaths[artistPath].Add(new string[2] { destiny, Path.Combine(artistPath, comicName) });

        //                        if (_ComicsNewPaths[groupPath].Count == _MinNumberOfComics)
        //                        {
        //                            foreach (var comic in _ComicsNewPaths[groupPath])
        //                            {
        //                                MoveDirectory(comic[0], comic[1]);
        //                            }
        //                            _ComicsNewPaths.Remove(groupPath);
        //                            MoveComicsIfEqualsMinNumberOfComics(artistPath);
        //                        }
        //                        break;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ConsoleUtilities.ErrorMessage("Sorry an error ocurred!");
        //        ConsoleUtilities.ErrorMessage(ex.Message);
        //    }
        //    _EndTime = DateTime.Now;
        //    ConsoleUtilities.Division();
        //    ConsoleUtilities.SuccessMessage("TASK FINISHED!");
        //    ConsoleUtilities.WarningMessage("{0} organizing {1} directories", (_EndTime-_StartTime).ToString(), ""+_TotalDirectories);
        //    ConsoleUtilities.WarningMessage("Success Rate: {0}", ((1 - (_Errors.Count / ((_TotalDirectories==0)?1:_TotalDirectories))) * 100) + "");
        //    ConsoleUtilities.WarningMessage("TOTAL ERROR COUNT: {0}", _Errors.Count+"");

        //    foreach (var err in _Errors)
        //    {
        //        ConsoleUtilities.ErrorMessage(err);
        //    }
        //}

        /// <summary>
        /// Moves the comics inside the directory key if the count equal to the number of comics. In theory the count should never be
        /// greater than <code>MinNumberOfComics</code> but just to be safe.
        /// </summary>
        /// <param name="key">The key of the dictionary.</param>
        //private void MoveComicsIfEqualsMinNumberOfComics(string key)
        //{
        //    if (_ComicsNewPaths[key].Count >= _MinNumberOfComics)
        //    {
        //        foreach (var comic in _ComicsNewPaths[key])
        //        {
        //            MoveDirectory(comic[0], comic[1]);
        //        }
        //        _ComicsNewPaths.Remove(key);
        //    }
        //}

        /// <summary>
        /// Moves the direcotry in <paramref name="source"/> to <paramref name="destiny"/>
        /// </summary>
        /// <param name="source">The path of the directory to move all the files to.</param>
        /// <param name="destiny">The path that the directory will be moved to</param>
        //private void MoveDirectory(string source, string destiny)
        //{
        //    ConsoleUtilities.SubDivision();
        //    ConsoleUtilities.WarningMessage("MOVING: {0}", source);
        //    if (!Directory.Exists(destiny))
        //    {
        //        Directory.CreateDirectory(destiny);
        //    }
        //    int maxTries = 5;
        //    Console.WriteLine();
        //    for (int i = 0; i < maxTries; i++)
        //    {
        //        try
        //        {
        //            int previousWidth = 0;
        //            foreach (string file in Directory.EnumerateFiles(source))
        //            {
        //                ConsoleUtilities.ClearPreviousLogImage(previousWidth);
        //                ConsoleUtilities.LogImage(file, destiny, out previousWidth);
        //                MoveImage(file, destiny);
        //            }
        //            break;
        //        }
        //        catch (Exception ex)
        //        {
        //            if (i < 4)
        //            {
        //                ConsoleUtilities.WarningMessage($"An error ocurred. Trying again {i + 1}/{maxTries}.");
        //                Console.WriteLine();
        //                continue;
        //            }
        //            ConsoleUtilities.ErrorMessage($"Sorry an Error ocurred trying to move the directory:\n{source}\nTo:\n{destiny}!");
        //            ConsoleUtilities.ErrorMessage(ex.Message);
        //            _Errors.Add($"ERROR ON: {source}");
        //            return;
        //        }
        //    }
        //    _TotalDirectories++;
        //    Directory.Delete(source, true);
        //    ConsoleUtilities.SuccessMessage("Succesfully moved to:\n{0}", destiny);
        //}

        #endregion
    }
}
