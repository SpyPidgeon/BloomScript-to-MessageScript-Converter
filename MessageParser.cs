using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BloomScriptConverter
{
    public enum ErrorValues : int
    { 
        None,
        MissingFile,
        MissingID,
        EmptyFile,
        NoDialogue,
        MissingBracket,
        FailedMessage
    }

    public sealed class MessageParser
    {
        public static MessageParser instance = new MessageParser();
        public List<string> messages { get; } = new List<string>();
        public List<Message> parsedMessages { get; } = new();
        public List<(int character,int outfit,int startLine)> outfitIDs { get; set; } = new();
        public ErrorValues ParseLines(FileStream stream)
        {
            if (stream.Length <= 0)
                return ErrorValues.EmptyFile;

            messages.Clear();
            messages.Add("");

            stream.Position = 0;
            for (int i = 0; i < stream.Length; i++)
            {
                int currentByte = stream.ReadByte();

                if ((currentByte == '\n' || currentByte == '\r') && messages[^1].Length <= 1)
                    continue;

                if (currentByte != '\n' && currentByte != '\r')
                {
                    messages[^1] += (char)currentByte;
                }
                else
                {
                    messages.Add("");
                }
            }

            return 0;
        }

        (ErrorValues,int) ParseLine(string message,int startLine = -1)
        {
            List<string> values = new();
            List<string> separateDialogue = new();

            for (int i = 0; i < message.Length; i++)
            {
                // Check bracket messages
                char c = message[i];
                if (c == '[')
                {
                    int next = message.IndexOf(']',i);
                    if (next == -1) return (ErrorValues.MissingBracket,71);
                    values.Add(message.Substring(i,next - i));
                    i = next;
                    continue;
                }

                // Check for dialogue
                if (c == '\"')
                {
                    int next = message.IndexOf('\"', i + 1);
                    if (next == -1) return (ErrorValues.MissingBracket, 81);
                    values.Add(message.Substring(i,next - i));
                    i = next;
                    continue;
                }

                // Check for if name is the first value
                if (i == 0 && char.IsLetter(c))
                {
                    int next = message.IndexOf(' ');
                    if (next == -1) return (ErrorValues.MissingBracket,91);
                    values.Add(message.Substring(i,next - i));
                    i = next - 1;
                    continue;
                }

                // General separator
                if (c == ' ' && char.IsLetter(message[i + 1]))
                {
                    int next = message.IndexOf(' ', i + 1);

                    if (next == -1)
                    {
                        next = message.Length - 1;
                    }

                    values.Add(message.Substring(i,next - i));
                    i = next - 1;
                    continue;
                }
            }

            Message? finalMessage = Message.CreateMessage(values, startLine);

            if (finalMessage == null)
                return (ErrorValues.FailedMessage,116);

            parsedMessages.Add(finalMessage);

            return (ErrorValues.None,0);
        }
        ErrorValues ParseOutfit(string message,int startLine)
        {
            message = message.ToLower();
            if (!File.Exists(JsonDirectories.charactersDirectory))
                return ErrorValues.MissingFile;

            string charactersJson = File.ReadAllText(JsonDirectories.charactersDirectory);
            JsonNode? nameNode = JsonNode.Parse(charactersJson);

            if (nameNode == null)
                return ErrorValues.EmptyFile;

            int end = message.IndexOf(' ');
            string name = message.Substring(0, end);
            name = name.ToLower();
            int dot = name.IndexOf('.');
            name = name.Remove(dot);
            int character = nameNode[name]?.GetValue<int>() ?? 1;

            string outfitsDirectory = JsonDirectories.outfitsDirectory + $"\\{name}.json";

            string outfitsJson = File.ReadAllText(outfitsDirectory);
            JsonNode? outfitNode = JsonNode.Parse(outfitsJson);
            if (outfitNode == null) return ErrorValues.MissingFile;

            end = message.IndexOf('"') + 1;
            int length = message.IndexOf('"', end) - end;

            string outfit = message.Substring(end,length);
            outfit = outfit.ToLower();
            Console.WriteLine(outfit);
            int outfitID = outfitNode[outfit]?.GetValue<int>() ?? 1;

            outfitIDs.Add((character,outfitID,startLine));

            return ErrorValues.None;
        }

        public (ErrorValues,int) ParseMessages()
        {
            Console.WriteLine($"Message count: {messages.Count}");

            for (int i = 0; i < messages.Count; i++)
            {
                var message = messages[i];
                if (message.IndexOf(".outfit") != -1 || message.IndexOf(".Outfit") != -1)
                {
                    ParseOutfit(message,i);
                    continue;
                }

                //Console.WriteLine($"Line to be parsed: {message}");
                (ErrorValues,int) value = ParseLine(message,i);

                if (value.Item1 != ErrorValues.None)
                {
                    return value;
                }
            }

            return (0,0);
        }

        public void PrintMessages()
        {
            foreach(var message in parsedMessages)
            {
                Type type = message.GetType();
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                foreach (FieldInfo field in fields)
                {
                    if (field.Name == "bup" || field.Name == "points")
                        continue;

                    object? value = field.GetValue(message);

                    if (value != null)
                        Console.WriteLine($"{field.Name} = {value}");
                }

                if (message.bup != null)
                    Console.WriteLine($"Bustup Id = {message.bup.Value.bustupID}\nBustup Outfit = {message.bup.Value.outfit}");

                if (message.points != null)
                    Console.WriteLine($"Confidant Id = {message.points.Value.confidantID}\nConfidant Points = {message.points.Value.confidantPoints}");

                Console.WriteLine();
            }
        }
    }
}
