namespace Organizer.Utilities
{
    public class ComicInfo
    {
        public string SourcePath { get; private set; }
        public string DestinyPath { get; private set; }
        public string ArtistName { get; private set; }

        public ComicInfo(string sourcePath, string destinyPath, string artistName)
        {
            SourcePath = sourcePath;
            DestinyPath = destinyPath;
            ArtistName = artistName;
        }

        public override string ToString()
        {
            return DestinyPath;
        }
    }
}
