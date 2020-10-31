using System;
using System.IO;

namespace Organizer.Utilities
{
    public static class ConsoleUtilities
    {
        #region Properties

        /// <summary>
        /// Char used to make the divisions on the console.
        /// </summary>
        /// <seealso cref="Division"/>
        /// <seealso cref="Division(int)"/>
        /// <seealso cref="SubDivision"/>
        /// <seealso cref="SubDivision(int)"/>
        public static char DivisionSign { get; set; } = '=';

        /// <summary>
        /// Text color used in the <c>InfoMessage</c> method.
        /// </summary>
        /// <seealso cref="InfoMessage(string, string[])"/>
        public static ConsoleColor InfoMessageColor { get; set; } = ConsoleColor.DarkGray;

        /// <summary>
        /// Text color used in the <c>SuccessMessage</c> method.
        /// </summary>
        /// <seealso cref="SuccessMessage(string, string[])"/>
        public static ConsoleColor SuccesMessageColor { get; set; } = ConsoleColor.Green;

        /// <summary>
        /// Text color used in the <c>WarningMessage</c> method.
        /// </summary>
        /// <seealso cref="WarningMessage(string, string[])"/>
        public static ConsoleColor WarningMessageColor { get; set; } = ConsoleColor.DarkYellow;

        /// <summary>
        /// Text color used in the <c>ErrorMessage</c> method.
        /// </summary>
        /// <seealso cref="ErrorMessage(string, string[])"/>
        public static ConsoleColor ErrorMessageColor { get; set; } = ConsoleColor.Red;

        /// <summary>
        /// Text color used in the <c>LogImage</c> method.
        /// </summary>
        /// <seealso cref="LogImage(string, string, out string)"/>
        public static ConsoleColor LogImageColor { get; set; } = ConsoleColor.DarkGray;

        /// <summary>
        /// Text color used in the <c>ShowTitle</c> method.
        /// </summary>
        /// <seealso cref="ShowTitle(string[])"/>
        public static ConsoleColor TitleColor { get; set; } = ConsoleColor.Green;

        #endregion

        #region Simple Methods

        /// <summary>
        /// Shows each line of the given array to the console,
        /// uses the color of <c>TitleColor</c>.
        /// </summary>
        /// <param name="title">The title to be showed.</param>
        /// <seealso cref="TitleColor"/>
        public static void ShowTitle(string[] title)
        {
            Division();
            Console.ForegroundColor = TitleColor;
            foreach (var line in title)
            {
                Console.WriteLine(CenterLine(line));
            }
            Console.ForegroundColor = ConsoleColor.White;
            Division();
            WarningMessage("Running version: {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }

        /// <summary>
        /// Returns the <paramref name="text"/> given width the right amount of spaces to be centered on the screen.
        /// </summary>
        /// <param name="text">The text to be centered.</param>
        /// <returns>The text with the right amount of spaces.</returns>
        public static string CenterLine(string text)
        {
            int extraSpaces = (int)Math.Ceiling((decimal)(Console.BufferWidth - text.Length) / 2);
            return new string(' ', extraSpaces) + text;
        }

        /// <summary>
        /// Creates a Cyan division with the specified <paramref name="length"/>,
        /// uses the <c>DivisionSign</c> property to make the division.
        /// </summary>
        /// <param name="length">The length of the division to create.</param>
        /// <seealso cref="DivisionSign"/>
        public static void Division(int length)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string(DivisionSign, length));
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Creates a Cyan division with a length equal to the <c>Console.BufferWidth</c>,
        /// uses the <c>DivisionSign</c> property to make the division.
        /// </summary>
        /// <seealso cref="DivisionSign"/>
        public static void Division()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string(DivisionSign, Console.BufferWidth));
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Creates a Cyan sub-division with the specified <paramref name="length"/>,
        /// uses the <c>DivisionSign</c> property to make the division.
        /// </summary>
        /// <param name="length">The length of the division to create.</param>
        /// <seealso cref="DivisionSign"/>
        public static void SubDivision(int length)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(new string(DivisionSign, length));
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Creates a Cyan sub-division with a length equal to the <c>Console.BufferWidth</c>
        /// Uses the <c>DivisionSign</c> property to make the division.
        /// </summary>
        /// <seealso cref="DivisionSign"/>
        public static void SubDivision()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(new string(DivisionSign, Console.BufferWidth));
            Console.ForegroundColor = ConsoleColor.White;
        }
        #endregion

        #region Messages Methods

        /// <summary>
        /// Shows the specified <paramref name="message"/>,
        /// With the color of <c>InfoMessageColor</c>,
        /// <paramref name="args"/> is passed as is to the <c>Console.WriteLine</c> method.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        /// <param name="args">An array of objects to write using.</param>
        public static void InfoMessage(string message, params string[] args)
        {
            Console.ForegroundColor = InfoMessageColor;
            Console.WriteLine(message, args);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Shows the specified <paramref name="message"/>,
        /// With the color of <c>SuccessMessageColor</c>,
        /// <paramref name="args"/> is passed as is to the <c>Console.WriteLine</c> method.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        /// <param name="args">An array of objects to write using.</param>
        public static void SuccessMessage(string message, params string[] args)
        {
            Console.ForegroundColor = SuccesMessageColor;
            Console.WriteLine(message, args);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Shows the specified <paramref name="message"/>,
        /// With the color of <c>WarningMessageColor</c>,
        /// <paramref name="args"/> is passed as is to the <c>Console.WriteLine</c> method.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        /// <param name="args">An array of objects to write using.</param>
        public static void WarningMessage(string message, params string[] args)
        {
            Console.ForegroundColor = WarningMessageColor;
            Console.WriteLine(message, args);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Shows the specified <paramref name="message"/>,
        /// With the color of <c>ErrorMessageColor</c>,
        /// <paramref name="args"/> is passed as is to the <c>Console.WriteLine</c> method.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        /// <param name="args">An array of objects to write using.</param>
        public static void ErrorMessage(string message, params string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message, args);
            Console.ForegroundColor = ConsoleColor.White;
        }
        #endregion

        #region Image methods

        /// <summary>
        /// Logs a message that shows that the <paramref name="image"/> is being copied to <paramref name="newPath"/>,
        /// the color used is from <c>LogImageColor</c>.
        /// </summary>
        /// <param name="image">The path of the image that is being copied.</param>
        /// <param name="newPath">The new path that the image will be copied to.</param>
        /// <param name="loggedString">The string that was logged to the console.</param>
        public static void LogImage(string image, string newPath, out int loggedStringLength)
        {
            string loggedString = $"Copying {newPath}{Path.DirectorySeparatorChar}{Path.GetFileName(image)}";
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(loggedString);
            Console.ForegroundColor = ConsoleColor.White;
            loggedStringLength = loggedString.Length;
        }

        /// <summary>
        /// Deletes the previous Log of an image being copied by <c>LogImage</c> function.
        /// </summary>
        /// <param name="width">The size of the previous logged string</param>
        public static void ClearPreviousLogImage(int width)
        {
            int lines = (int)Math.Floor((decimal)(width / Console.BufferWidth)) + 1;

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
        #endregion
    }
}
