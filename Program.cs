using BloomScriptConverter;
using System;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
            return;

        string extension = Path.GetExtension(args[0]);

        if (extension != ".msg" && extension != ".bloom")
        {
            Console.WriteLine("Invalid extension!");
            return;
        }

        FileStream file = File.Open(args[0], FileMode.Open);

        MessageParser.instance.ParseLines(file);
        ErrorValues value = MessageParser.instance.ParseMessages();

        if (value != ErrorValues.None)
            Console.WriteLine($"Error: {value}");

        Console.ReadLine();
    }
}