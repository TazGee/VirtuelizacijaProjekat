using System;
using System.Configuration;

namespace Client.Config
{
    public class AppSettingsReader
    {
        public string CsvPath { get; private set; }
        public string CountryCode { get; private set; }
        public DateTime SelectedDate { get; private set; }
        public int BatchSize { get; private set; }

        public AppSettingsReader()
        {
            CsvPath = ConfigurationManager.AppSettings["CsvPath"];
            CountryCode = ConfigurationManager.AppSettings["CountryCode"];

            string selectedDateText = ConfigurationManager.AppSettings["SelectedDate"];
            string batchSizeText = ConfigurationManager.AppSettings["BatchSize"];

            if (string.IsNullOrWhiteSpace(CsvPath))
            {
                throw new Exception("CsvPath nije podešen u App.config.");
            }

            if (string.IsNullOrWhiteSpace(CountryCode))
            {
                throw new Exception("CountryCode nije podešen u App.config.");
            }

            if (!DateTime.TryParse(selectedDateText, out DateTime selectedDate))
            {
                throw new Exception("SelectedDate nije validan u App.config.");
            }

            if (!int.TryParse(batchSizeText, out int batchSize) || batchSize <= 0)
            {
                throw new Exception("BatchSize nije validan u App.config.");
            }

            SelectedDate = selectedDate.Date;
            BatchSize = batchSize;
            CountryCode = CountryCode.Trim().ToUpper();
        }
    }
}