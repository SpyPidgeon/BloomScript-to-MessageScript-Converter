using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
        NoDialogue
    }

    public enum Phase
    {
        Id,
        Bustup,
        Dialogue,
        Points
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

        ErrorValues ParseLine(string message,int startLine = -1)
        {
            int id = 0;
            Phase phase = 0;
            string bustup = "";
            string dialogue = "";
            string? points = null;
            int startQuotation = message.IndexOf('"');

            if (startQuotation <= -1)
                return ErrorValues.NoDialogue;

            if (startQuotation == 0)
            {

                Message newMessage = Message.CreateDialogueOnlyMessage(message);
                parsedMessages.Add(newMessage);
                return ErrorValues.None;
            }

            startQuotation--;

            for (int i = 0; i < message.Length; i++)
            {
                switch (phase)
                {
                    case Phase.Id:
                        int end = message.IndexOf(' ');
                        string number = message.Substring(0, end);
                        if (int.TryParse(number, out int messageID))
                            id = messageID;
                        else
                            return ErrorValues.MissingID;

                        phase++;
                        i = end;
                        continue;
                    case Phase.Bustup:
                        if (message[i] == ' ' && bustup.Length <= 1)
                            continue;

                        if (i == startQuotation)
                        {
                            phase++;
                            continue;
                        }

                        bustup += message[i];

                        continue;
                    case Phase.Dialogue:
                        if (i > startQuotation + 1 && message[i] == '"')
                        {
                            phase++;
                            continue;
                        }

                        if (message[i] != '"') dialogue += message[i];

                        continue;

                    case Phase.Points:
                        if (message[i] == ' ' && points == null)
                            continue;

                        if (!char.IsLetter(message[i]) && points == null)
                            phase++;

                        if (points == null)
                            points = "";

                        points += message[i];

                        break;
                }
            }

            List<string> separateDialogue = new();
            Message.HandleLongLines(ref dialogue, ref separateDialogue);
            if (separateDialogue.Count > 0) Console.WriteLine(separateDialogue[^1]);

            Message finalMessage = Message.CreateMessage(id,bustup,dialogue,points,false,startLine);
            parsedMessages.Add(finalMessage);

            if (separateDialogue.Count > 0)
            {
                foreach(var line in separateDialogue)
                {
                    parsedMessages.Add(Message.CreateDialogueOnlyMessage(line));
                }
            }

            return ErrorValues.None;
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

        public ErrorValues ParseMessages()
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
                ErrorValues value = ParseLine(message,i);

                if (value != ErrorValues.None)
                {
                    return value;
                }
            }

            return 0;
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

                    object value = field.GetValue(message);

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
