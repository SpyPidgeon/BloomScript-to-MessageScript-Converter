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
        public MessageType type;
        public Bustup? bup;
//        public VoicePack? pack;
        public string? nameplate;
        public string msg;
        public ConfidantPoints? points;

        public static int maxCharacters = 30;
        public static int maxLines = 3;

        public static string[] messageNames =
        {
            "message",
            "trivia",
            "mind",
            "select",
            "system",
            "devil"
        };
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

        public static Message CreateMessage(int id, string bustup, string message,string? points = null,bool phone = false,int startLine = -1)
        {
            if (!File.Exists(JsonDirectories.charactersDirectory) 
                || !File.Exists(JsonDirectories.arcanaDirectory) 
                || !File.Exists(JsonDirectories.bustupDirectory))
                return new Message();

            string charactersJson = File.ReadAllText(JsonDirectories.charactersDirectory);
            string arcanaJson = File.ReadAllText(JsonDirectories.arcanaDirectory);
            string bustupJson = File.ReadAllText(JsonDirectories.bustupDirectory);
            JsonNode? characterNode = JsonNode.Parse(charactersJson);
            JsonNode? arcanaNode = JsonNode.Parse(arcanaJson);
            JsonNode? bustupNode = JsonNode.Parse(bustupJson);

            if (characterNode == null || arcanaNode == null || bustupNode == null)
                return new Message();

            bustup = bustup.ToLower();
            points = points?.ToLower() ?? null;

            // Bustup/MessageType
            int dot = bustup.IndexOf('.');
            string name = "";

            Bustup? bustupValues = null;
            MessageType messageType = MessageType.MSG;
            if (dot > -1)
            {

                name = bustup.Substring(0, dot);
                string bupName = bustup.Substring(dot + 1);

                bool usesBustup = true;
                for (int i = 0; i < messageNames.Length; i++)
                {
                    var msgtype = messageNames[i];

                    if (msgtype == name)
                    {
                        usesBustup = false;
                        messageType = (MessageType)i;
                        break;
                    }
                }

                if (usesBustup)
                {
                    int character = characterNode[name]?.GetValue<int>() ?? 0;
                    int bustupID = bustupNode[bupName]?.GetValue<int>() ?? 0;
                    int outfit = 0;

                    if (startLine != -1)
                    {
                        var outfitIDs = MessageParser.instance.outfitIDs;
                        var outfitID = outfitIDs.Where(f => f.startLine < startLine && f.character == character).DefaultIfEmpty().Max();

                        outfit = outfitID.outfit;
                    }

                    bustupValues = new Bustup()
                    {
                        character = character,
                        bustupID = bustupID,
                        outfit = outfit,
                        phone = phone
                    };
                }
                else
                {
                    name = bupName;
                }
            }
            else
            {
                name = bustup;
            }

            // Points
            ConfidantPoints? confidantPoints = null;
            if (points != null)
            {
                int space = points.IndexOf(' ');

                string arcana = points.Substring(0, space);
                int confidantID = arcanaNode[arcana]?.GetValue<int>() ?? 1;

                string value = points.Substring(space + 1);
                int confidantPointsValue = 1;
                if (int.TryParse(value, out int pointValue))
                    confidantPointsValue = pointValue;

                confidantPoints = new ConfidantPoints()
                {
                    confidantID = confidantID,
                    confidantPoints = confidantPointsValue,
                };
            }

            name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);

            Message newMessage = new Message()
            {
                id = id,
                bup = bustupValues,
                type = messageType,
                nameplate = name,
                msg = message,
                points = confidantPoints
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
