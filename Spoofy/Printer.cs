namespace Spoofy;

using System.Globalization;

static class Printer
{
    public static int BoxCount { get; set; }
    public static int[] BoxSize { get; set; }

    public static void PrintBoxLine()
    {
        for (int i = 0; i < BoxCount; i++)
        {
            Console.Write($"+{string.Concat(Enumerable.Repeat("-", BoxSize[i]))}");
        }
        Console.WriteLine("+");
    }

    public static void PrintInfoLine(string[] lineData)
    {
        for (int i = 0; i < BoxCount; i++)
        {
            Console.Write(
                $"| {lineData[i]}{string.Concat(Enumerable.Repeat(" ", BoxSize[i] - lineData[i].Length - 1))}"
            );
        }
        Console.WriteLine("|");
    }

    public static string HandleLength(string prevStr)
    {
        int doubleWidthCount = prevStr.Count(IsDoubleWidth);
        int trueLength = prevStr.Length + doubleWidthCount;

        if (trueLength >= 20)
            return prevStr[..(17 - doubleWidthCount)] + "...";

        return prevStr;
    }

    // Deals with Hangul and other CJK characters
    public static bool IsDoubleWidth(char c)
    {
        return c >= '\u1100'
            && c <= '\uFF60'
            && char.GetUnicodeCategory(c) == UnicodeCategory.OtherLetter;
    }
}
