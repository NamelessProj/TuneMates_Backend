using System.Text.RegularExpressions;

namespace TuneMates_Backend.Utils
{
    public static partial class String
    {
        /// <summary>
        /// Remove non-ASCII characters from a string
        /// </summary>
        /// <param name="s">The input string</param>
        /// <returns>A string with non-ASCII characters removed</returns>
        public static string RemoveNonAscii(this string s) => Regex.Replace(s, "[^\x20-\x7E]", "");
    }
}