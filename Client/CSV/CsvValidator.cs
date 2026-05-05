using System;
using System.Globalization;

namespace Client.CSV
{
    public class CsvValidator
    {
        public int GetColumnIndex(string[] headers, string columnName)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i].Trim() == columnName)
                {
                    return i;
                }
            }

            return -1;
        }

        public void ValidateRequiredColumns(string[] headers, string countryCode,
            out int utcIndex,
            out int localIndex,
            out int actualIndex,
            out int forecastIndex)
        {
            utcIndex = GetColumnIndex(headers, "utc_timestamp");
            localIndex = GetColumnIndex(headers, "cet_cest_timestamp");

            string actualColumn = countryCode + "_load_actual_entsoe_transparency";
            string forecastColumn = countryCode + "_load_forecast_entsoe_transparency";

            actualIndex = GetColumnIndex(headers, actualColumn);
            forecastIndex = GetColumnIndex(headers, forecastColumn);

            if (utcIndex == -1)
            {
                throw new Exception("CSV ne sadrži kolonu utc_timestamp.");
            }

            if (localIndex == -1)
            {
                throw new Exception("CSV ne sadrži kolonu cet_cest_timestamp.");
            }

            if (actualIndex == -1)
            {
                throw new Exception("CSV ne sadrži kolonu: " + actualColumn);
            }

            if (forecastIndex == -1)
            {
                throw new Exception("CSV ne sadrži kolonu: " + forecastColumn);
            }
        }

        public bool HasEnoughColumns(string[] values, params int[] indexes)
        {
            foreach (int index in indexes)
            {
                if (index < 0 || values.Length <= index)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryParseDate(string text, out DateTime dateTime)
        {
            return DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out dateTime);
        }

        public bool TryParseDouble(string text, out double value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();

            if (trimmed.Equals("NaN", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool parsed = double.TryParse(
                trimmed,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);

            if (!parsed)
            {
                return false;
            }

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return false;
            }

            return true;
        }
    }
}