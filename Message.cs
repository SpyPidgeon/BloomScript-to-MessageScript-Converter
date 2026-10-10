using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BloomScriptConverter
{
    public enum MessageType : int
    {
        MSG,
        TRV,
        MND,
        SEL,
        SYS,
        DVL
    }

    public struct Bustup
    {
        public int character;
        public int bustupID;
        public int outfit;
        public bool phone;
    }
    public struct VoicePack
    {
        public int unk1;
        public int cueSource;
        public int cueID;
        public int mouthMove;
        public int unk2;
    }
    public struct ConfidantPoints
    {
        public int confidantID;
        public int confidantPoints;
    }
    public class Message
    {
        public int? id;
        public int subID = 0;
        public MessageType type;
        public Bustup? bup;
//        public VoicePack? pack;
        public string? nameplate;
        public string msg;
        public ConfidantPoints? points;

        public static int maxCharacters = 30;
        public static int maxLines = 3;

        public static Message CreateDialogueOnlyMessage(string message)
        {
            Message previousMessage = MessageParser.instance.parsedMessages[^1];
            string dialogue = message;
            dialogue = dialogue.Replace("\"", "");

            Message thisMessage = new Message()
            {
                id = null,
                bup = previousMessage.bup,
                type = previousMessage.type,
                msg = dialogue
            };

            return thisMessage;
        }

        public static void HandleLongLines(ref string message,ref List<string> separateMessages)
        {
            if (message.Length > maxCharacters)
            {
                int lineBreakCount = message.Length / maxCharacters;

                for (int i = 1; i < lineBreakCount; i++)
                {
                    bool updateMessageList = separateMessages.Count > 0 && message == separateMessages[^1];
                    int newLineIndex = i * maxCharacters;
                    if (message[newLineIndex] != ' ') newLineIndex = message.IndexOf(' ', newLineIndex);
                    if (i < maxLines)
                    {
                        message = message.Insert(newLineIndex, "[n]");

                        if (updateMessageList)
                            separateMessages[^1] = message;
                    }
                    else
                    {
                        string newMessage = message.Substring(newLineIndex);
                        message = message.Remove(newLineIndex);
                        if (updateMessageList) separateMessages[^1] = message;

                        separateMessages.Add(newMessage);
                        HandleLongLines(ref newMessage, ref separateMessages);
                        Console.WriteLine(newMessage);
                        break;
                    }
                }
            }
        }

        public static Message? CreateMessage(List<string> values, int startLine = -1)
        {
            Message? newMessage = null;

            if (!File.Exists(JsonDirectories.charactersDirectory)
                || !File.Exists(JsonDirectories.arcanaDirectory))
                return new Message();

            string charactersJson = File.ReadAllText(JsonDirectories.charactersDirectory);
            string arcanaJson = File.ReadAllText(JsonDirectories.arcanaDirectory);
            JsonNode? characterNode = JsonNode.Parse(charactersJson);
            JsonNode? arcanaNode = JsonNode.Parse(arcanaJson);
            JsonNode? bustupNode = null;

            if (characterNode == null || arcanaNode == null)
                return null;

            int? characterID = null;
            int? majorID = null;
            int subID = 0;
            MessageType type = MessageType.MSG;
            Bustup? bustup = null;
            string? bustupString = null;
            string? nameplate = null;
            string dialogue = "";
            ConfidantPoints? points = null;
            bool phone = false;

            bool dialogueOnly = values.Count == 1 && values[0][0] == '\"';

            for (int i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (value[0] != '\"') value = value.ToLower();
                value = value.Trim();

                if (value[0] == '"' && dialogue.Length <= 1)
                {
                    dialogue = value.Replace("\"", "");
                    continue;
                }

                if (value == "phone")
                {
                    phone = true;
                    continue;
                }

                if (value[0] == '[')
                {
                    bool isIDs = value.Any(char.IsDigit) && !value.Any(char.IsLetter);
                    bool isPoints = value.Any(char.IsLetter) && value.Any(char.IsDigit);
                    value = value.Replace("[", "");
                    value = value.Replace("]", "");
                    value = value.Trim();
                    Console.WriteLine($"Value: {value}, isID: {isIDs}, isPoints: {isPoints}");

                    if (isIDs)
                    {
                        int mIndex = Array.FindIndex(value.ToCharArray(), char.IsDigit);
                        int nextIndex = value.IndexOf(' ', mIndex);

                        if (nextIndex == -1) nextIndex = 1;

                        majorID = int.Parse(value.Substring(mIndex, nextIndex - mIndex));

                        if (value.Length == 1) continue;

                        int sIndex = Array.FindIndex(value.ToCharArray(), nextIndex + 1, char.IsDigit);
                        if (sIndex == -1)
                            continue;

                        nextIndex = value.Length;
                        nextIndex--;
                        Console.WriteLine(value.Substring(sIndex));
                        subID = int.Parse(value.Substring(sIndex));
                    }
                    else if (isPoints)
                    {
                        int space = value.IndexOf(' ');
                        string arcana = value.Substring(0,space);
                        Console.WriteLine($"Arcana: {arcana}");

                        int? confidantID = arcanaNode[arcana]?.GetValue<int>() ?? null;
                        if (confidantID == null) return null;

                        string pointString = value.Substring(space + 1);
                        int confidantPoints = int.Parse(pointString);

                        points = new ConfidantPoints()
                        {
                            confidantID = confidantID.Value,
                            confidantPoints = confidantPoints
                        };
                    }
                    else
                    {
                        int? idCheck = characterNode[value]?.GetValue<int>();
                        if (idCheck != null)
                        {
                            Console.WriteLine(idCheck);
                            characterID = idCheck;
                            nameplate = value;
                            continue;
                        }

                        if (type == MessageType.MSG)
                        {
                            for (int j = 0; j < MessageScriptGenerator.msgTypes.Length; j++)
                            {
                                var key = MessageScriptGenerator.msgTypes[j].key;
                                if (value == key)
                                {
                                    type = (MessageType)j;
                                    break;
                                }
                            }

                            if (type != MessageType.MSG)
                                continue;
                        }

                        if (characterID != null)
                        {
                            bustupString = value;
                            continue;
                        }

                        nameplate = value;
                        continue;
                    }
                }
            }

            int outfit = 0;
            var outfitIDs = MessageParser.instance.outfitIDs;

            if (characterID != null)
            {
                string path = $"{JsonDirectories.bustupDirectory}\\{nameplate}.json";

                if (File.Exists(path))
                {
                    Console.WriteLine(path);
                    string bustupJson = File.ReadAllText(path);
                    bustupNode = JsonNode.Parse(bustupJson);
                }
            }

            if (dialogueOnly)
            {
                var parsedMessages = MessageParser.instance.parsedMessages;
                bustup = parsedMessages[^1]?.bup;
                majorID = parsedMessages[^1]?.id;
            }

            if (outfitIDs.Any() && characterID != null)
            {
                var outfitID = outfitIDs.Where(x => x.startLine < startLine && x.character == characterID).Last();
                outfit = outfitID.outfit;
            }

            if (bustupString != null && bustupNode != null)
            {
                bustupString = bustupString.Trim();

                int? bustupID = bustupNode[bustupString]?.GetValue<int>();
                if (bustupID == null) return null;

                bustup = new Bustup()
                {
                    bustupID = bustupID.Value,
                    outfit = outfit,
                    phone = phone,
                    character = characterID.Value
                };
            }

            if (majorID == null && !dialogueOnly)
            {
                if (MessageParser.instance.parsedMessages.Any())
                    majorID = MessageParser.instance.parsedMessages[^1].id + 1;
                else
                    majorID = 0;
            }

            if (nameplate != null) nameplate = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(nameplate);

            newMessage = new Message()
            {
                id = majorID,
                subID = subID,
                type = type,
                bup = bustup,
                nameplate = nameplate,
                msg = dialogue,
                points = points
            };

            return newMessage;
        }

        public Message()
        {
            id = 1;
            msg = "";
        }
    }
}
