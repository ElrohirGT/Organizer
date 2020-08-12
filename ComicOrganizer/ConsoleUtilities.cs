using System;
using System.IO;

namespace Organizer.Utilities
{
    public static class ConsoleUtilities
    {
        public static void Division(int length)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string('=', length));
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void SubDivision(int length)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(new string('=', length));
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
        public static void ErrorMessage(string message, params string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message, args);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void LogImage(string image, string newPath, out string loggedString)
        {
            loggedString = $"Copying {newPath}{Path.DirectorySeparatorChar}{Path.GetFileName(image)}";
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(loggedString);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void ClearPreviousLogImage(int width)
        {
            int lines = (int)Math.Ceiling((decimal)(width / Console.BufferWidth)) + 1;
            if (Console.CursorTop > lines)
            {
                Console.SetCursorPosition(0, Console.CursorTop - lines);
                for (int i = 0; i < lines; i++)
                {
                    Console.Write(new string(' ', Console.BufferWidth));
                }
                Console.SetCursorPosition(0, Console.CursorTop - lines);
            }
        }
    }
}
