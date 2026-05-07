using System;
using System.Configuration;
using System.Globalization;

namespace Server.Config
{
    public class ServerSettingsReader
    {
        public double LoadFactorMin { get; private set; }
        public double FlatlineEpsilon { get; private set; }
        public int FlatlineWindowSamples { get; private set; }
        public double SpikeDeltaMW { get; private set; }

        public ServerSettingsReader()
        {
            LoadFactorMin = ReadDouble("LoadFactorMin");
            FlatlineEpsilon = ReadDouble("FlatlineEpsilon");
            FlatlineWindowSamples = ReadInt("FlatlineWindowSamples");
            SpikeDeltaMW = ReadDouble("SpikeDeltaMW");
        }

        private double ReadDouble(string key)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                throw new Exception("Neispravna vrijednost u App.config za: " + key);
            }

            return result;
        }

        private int ReadInt(string key)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (!int.TryParse(value, out int result))
            {
                throw new Exception("Neispravna vrijednost u App.config za: " + key);
            }

            return result;
        }
    }
}