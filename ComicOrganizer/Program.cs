using System;
using System.Text;

namespace ComicOrganizer
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.Clear();
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Organizer";

            new ComicOrganizer().StartProgram();
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}
