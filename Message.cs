using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BloomScriptConverter
{
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
        public int id;
        public Bustup bup;
//        public VoicePack? pack;
        public string? nameplate;
        public string? msg;
        public ConfidantPoints? points;

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

            // Bustup
            int dot = bustup.IndexOf('.');
            string name = bustup.Substring(0, dot);
            string bupName = bustup.Substring(dot + 1);
            Console.WriteLine(bupName);

            Bustup bustupValues = new Bustup();
            bustupValues.character = characterNode[name]?.GetValue<int>() ?? 0;
            bustupValues.bustupID = bustupNode[bupName]?.GetValue<int>() ?? 0;

            if (startLine != -1)
            {
                var outfitIDs = MessageParser.instance.outfitIDs;
                var outfitID = outfitIDs.Where(f => f.startLine < startLine).DefaultIfEmpty().Max();

                bustupValues.outfit = outfitID.outfit;
            }
            else
            {
                bustupValues.outfit = 1;
            }

            bustupValues.phone = phone;

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
                nameplate = name,
                msg = message,
                points = confidantPoints
            };

            return newMessage;
        }

        public Message()
        {
            id = 1;
            bup = new Bustup()
            {
                character = 1,
                bustupID = 1,
                outfit = 1,
                phone = false
            };
        }
    }
}
