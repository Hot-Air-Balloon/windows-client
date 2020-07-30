using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Windows.Forms;

namespace HotAirBalloon
{
    public class Config
    {
        public static ConfigJson LoadJson()
        {
            using (StreamReader r = new StreamReader("config.json"))
            {
                string jsonString = r.ReadToEnd();
                ConfigJson config = JsonSerializer.Deserialize<ConfigJson>(jsonString);
                return config;
            }
        }
    }
    public class ConfigJson
    {
        public string server { get; set; }
        public int port { get; set; }
        public string passport { get; set; }
    }
}
