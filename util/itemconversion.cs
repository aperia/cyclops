using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

//TODO: Finish this crap
namespace Cyclops {
    public class ItemConversion {
        public static Dictionary<int, int> GetConversionDictionary() {
            string directory = System.Environment.CurrentDirectory;
            string path = directory + "/data/items/item_conversion.txt";
            if (!File.Exists(path)) {
                Console.WriteLine("Error: file " + path + " not found");
                return null;
            }

            Dictionary<int, int> _64TO71 = new Dictionary<int, int>();

            TextReader tr = new StreamReader(path);

            string val = tr.ReadLine();
            while (val != "-1") {
                if (val == "") {
                } else {
                    string[] ids = Regex.Split(val, "\\s+");
                    int key = int.Parse(ids[0]);
                    int value = int.Parse(ids[1]);
                    if (_64TO71.ContainsKey(key)) {
                    } else {
                        _64TO71.Add(key, value);
                    }
                }
                val = tr.ReadLine();
            }
            tr.Close();
            return _64TO71;
        }
    }
}
