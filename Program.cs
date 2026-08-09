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
        (ErrorValues,int) value = MessageParser.instance.ParseMessages();

        if (value.Item1 != ErrorValues.None)
            Console.WriteLine($"Error: {value}");

        //MessageParser.instance.PrintMessages();

        string generatedfile = MessageScriptGenerator.Instance.CreateMessageString();

        Console.WriteLine($"Parsed Count: {MessageParser.instance.parsedMessages.Count}");
        Console.WriteLine(generatedfile);
        File.WriteAllText("test.msg", generatedfile);

        Console.ReadLine();
    }
}