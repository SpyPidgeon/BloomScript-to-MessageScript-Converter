using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloomScriptConverter
{
    public static class JsonDirectories
    {
        static string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static string charactersDirectory = Path.Combine(exeDirectory, "config\\characters.json");
        public static string arcanaDirectory = Path.Combine(exeDirectory, "config\\arcanas.json");
        public static string bustupDirectory = Path.Combine(exeDirectory, "config\\bustups");
        public static string outfitsDirectory = Path.Combine(exeDirectory, "config\\outfits");
    }
}
