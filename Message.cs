using System;
using System.Collections.Generic;
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

        public static Message CreateMessage(int id, string bustup, string message,string? points = null)
        {
            string directory = AppDomain.CurrentDomain.BaseDirectory;
            string charactersjson = Path.Combine(directory,"config/characters.json");
            if (!File.Exists(charactersjson))
                return null;

            string charactersJson = File.ReadAllText(charactersjson);
            JsonNode? node = JsonNode.Parse(charactersJson);

            bustup = bustup.ToLower();
            points = points?.ToLower() ?? null;

            
        }
    }
}
