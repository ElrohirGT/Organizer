using System;
using System.Text;
using Organizer.Utilities;

namespace Organizer
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.Clear();
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Organizer";

            new ComicOrganizer().StartApp().Wait();

            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }
    }
}
