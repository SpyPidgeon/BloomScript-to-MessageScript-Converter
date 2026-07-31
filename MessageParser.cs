using System;
using System.Collections.Generic;
using System.Linq;
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
        List<string> messages = new List<string>();
        List<Message> parsedMessages = new();
        List<(int character,int outfit,int startLine)> outfitIDs = new();
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

                if (currentByte == '\n' && messages[^1].Length <= 1)
                    continue;

                if (currentByte != '\n')
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

        ErrorValues ParseLine(string message)
        {
            int id = 0;
            Phase phase = 0;
            string bustup = "";
            string dialogue = "";
            string points = "";

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

                        bustup += message[i];

                        if (message[i] == ' ')
                        {
                            i = message.IndexOf('"');

                            if (i == -1)
                                return ErrorValues.NoDialogue;

                            phase++;
                        }

                        continue;
                    case Phase.Dialogue:
                        if (message[i] == '"')
                        {
                            phase++;
                            continue;
                        }

                        dialogue += message[i];

                        continue;

                    case Phase.Points:
                        if (message[i] == ' ' && points.Length <= 1)
                            continue;

                        if (!char.IsLetter(message[i]) && points.Length <= 1)
                            phase++;

                        points += message[i];

                        break;
                }
            }

            Console.WriteLine($"Bustup: {bustup}\nPoints: {points}\nDialogue: {dialogue}\nID: {id}");

            return ErrorValues.None;
        }
        ErrorValues ParseOutfit(string message,int startLine)
        {
            string directory = AppDomain.CurrentDomain.BaseDirectory;
            string charactersjson = Path.Combine(directory, "config/characters.json");
            if (!File.Exists(charactersjson))
                return ErrorValues.MissingFile;

            string charactersJson = File.ReadAllText(charactersjson);
            JsonNode? node = JsonNode.Parse(charactersJson);

            if (node == null)
                return ErrorValues.EmptyFile;

            int end = message.IndexOf(' ');
            string name = message.Substring(0, end);
            int character = node[name]?.GetValue<int>() ?? 1;

            end = message.IndexOf('=') + 1;
            
            while (message[end] == ' ' && end < message.Length)
            {
                end++;
            }

            string outfit = message.Substring(end);
            int outfitID = node[outfit]?.GetValue<int>() ?? 1;

            outfitIDs.Add((character,outfitID,startLine));

            return ErrorValues.None;
        }

        public ErrorValues ParseMessages()
        {
            for (int i = 0; i < messages.Count; i++)
            {
                var message = messages[i];

                if (message.IndexOf('=') != -1)
                {
                    ParseOutfit(message,i);
                    continue;
                }

                ErrorValues value = ParseLine(message);

                if (value != ErrorValues.None)
                {
                    return value;
                }
            }

            return 0;
        }
    }
}
