using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Organizer.Utilities;
using System.Threading;

namespace Organizer
{
    public class ComicOrganizer
    {
        #region Fields
        readonly string[] _Title =
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
        static readonly Regex[] _Regices =
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
        static readonly Regex _AlreadyOrganizedRegex = new Regex(@"^\(Artist\).*|^\(Group\).*");
        readonly BlockingCollection<string> _Errors = new BlockingCollection<string>();

        /// <summary>
        /// A Dictionary of comics keyed by their new path to move
        /// </summary>
        readonly Dictionary<string, List<ComicInfo>> _ComicsNewPaths = new Dictionary<string, List<ComicInfo>>();

        int _MinNumberOfComics = 2;
        int _TotalDirectories = 0;
        int threwAnException = 0;
        #endregion

        float Divider => (_TotalDirectories == 0) ? 1 : _TotalDirectories;
        float ProbabilityOfError => _Errors.Count / Divider;
        float SuccessProbability => 1 - ProbabilityOfError;

        public ComicOrganizer() { }

        public Task StartApp()
        {
            ConsoleUtilities.ShowTitle(_Title);
            Configure();
            return OrganizeAsync();
        }

        /// <summary>
        /// Asks the user parameters on how to organize the comics.
        /// </summary>
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

        /// <summary>
        /// Starts and shows the process to organize all comics.
        /// </summary>
        /// <returns></returns>
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

            ConsoleUtilities.WarningMessage("Success Rate: {0}", SuccessProbability.ToString("P2"));
            ConsoleUtilities.WarningMessage("TOTAL ERROR COUNT: {0}", _Errors.Count + "");

            foreach (var errorMessage in _Errors)
                ConsoleUtilities.ErrorMessage(errorMessage);
            _Errors.Dispose();
        }

        /// <summary>
        /// Populates the <c>_ComicsNewPaths</c> dictionary with all the comics that need to be moved.
        /// It does so in a way that all comics that need to be moved appear just once in the entire dictionary,
        /// this way we can move all comics asynchronously.
        /// </summary>
        void PopulateDictionary()
        {
            ConsoleUtilities.InfoMessage("Scanning directories...");
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
                        _ComicsNewPaths[artistPath].Add(new ComicInfo(subDirectoryPath, artistFinalPath, artistName));

                        if (!_DoGroups || Directory.Exists(artistPath))
                            break;


                        InitializeKeyIfNotExists(groupPath);
                        _ComicsNewPaths[groupPath].Add(new ComicInfo(subDirectoryPath, groupFinalPath, artistName));

                        if (_ComicsNewPaths[artistPath].Count < _MinNumberOfComics)
                            break;

                        //Removes all references to previous comics that belonged to the same artist and group,
                        //which makes it safe to move all comics asynchonously
                        if (_ComicsNewPaths.TryGetValue(groupPath, out List<ComicInfo> listOfPaths))
                            listOfPaths.RemoveAll(info => info.ArtistName.Equals(artistName));

                        break;
                    }
                }
                ConsoleUtilities.InfoMessage("Finished!");
            }
            catch (Exception e)
            {
                ConsoleUtilities.ErrorMessage("An error ocurred...");
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
                    _ComicsNewPaths[artistPath].Add(new ComicInfo(dir, Path.Combine(artistPath, comicName), artistName));
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
                _ComicsNewPaths.Add(key, new List<ComicInfo>());
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
            string groupPath = string.IsNullOrEmpty(group) ? _MainPath : Path.Combine(_MainPath, $"(Group) {group}");
            string artistPath = Path.Combine(groupPath, $"(Artist) {artist}");
            if (!_DoGroups)
                artistPath = Path.Combine(_MainPath, $"(Artist) {artist}");

            return (groupPath, artistPath);
        }

        /// <summary>
        /// It deletes every key whose list count is 0. And the _MainPath key.
        /// </summary>
        void CleanDictionary()
        {
            _ComicsNewPaths.Remove(_MainPath);

            string[] keys = new string[_ComicsNewPaths.Count];
            _ComicsNewPaths.Keys.CopyTo(keys, 0);

            for (int i = 0; i < keys.Length; i++)
                if (!Directory.Exists(keys[i]) && _ComicsNewPaths[keys[i]].Count < _MinNumberOfComics)
                    _ComicsNewPaths.Remove(keys[i]);
        }

        /// <summary>
        /// Moves all comics from the dictionary to their respective paths in an async manner.
        /// </summary>
        /// <returns>Task that completes when all comics are moved.</returns>
        async Task MoveComics()
        {
            List<Task> comicsToMove = new List<Task>();

            foreach (var paths in _ComicsNewPaths.Values)
                foreach (var comicInfo in paths)
                    comicsToMove.Add(MoveComic(comicInfo.SourcePath, comicInfo.DestinyPath));

            await Task.WhenAll(comicsToMove);
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
                EnumerationOptions enumerationOptions = new EnumerationOptions();

                foreach (var imagePath in Directory.EnumerateFiles(source, "*", enumerationOptions))
                    imagesToMove.Add(MoveImageAsync(imagePath, destiny));

                await Task.WhenAll(imagesToMove);

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
                Interlocked.Increment(ref _TotalDirectories);
            }
        }

        /// <summary>
        /// Moves the <paramref name="image"/> to the specified <paramref name="newPath"/>.
        /// It doesn't have to be an image, it can be a file too.
        /// If there's already a file with the same name and extension in the specified path it will replace it.
        /// </summary>
        /// <param name="image">The path of the image to move, doesn't needs to be an image, can be any file.</param>
        /// <param name="newPath">The path to move <paramref name="image"/></param>
        Task MoveImageAsync(string image, string newPath)
        {
            try
            {
                File.Copy(image, Path.Combine(newPath, Path.GetFileName(image)), true);
                File.Delete(image);
                return Task.CompletedTask;
            }
            catch (Exception) { throw; }
        }
    }
}
