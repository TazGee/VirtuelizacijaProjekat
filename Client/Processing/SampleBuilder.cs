using Common.Models;
using System;

namespace Client.Processing
{
    public class SampleBuilder
    {
        public LoadSample Build(
            DateTime timestampUtc,
            DateTime timestampLocal,
            double actualMW,
            double forecastMW,
            double cumulativeMWh,
            string countryCode,
            int rowIndex)
        {
            return new LoadSample
            {
                TimestampUtc = timestampUtc,
                TimestampLocal = timestampLocal,
                ActualMW = actualMW,
                ForecastMW = forecastMW,
                CumulativeMWh = cumulativeMWh,
                CountryCode = countryCode,
                RowIndex = rowIndex
            };
        }
    }
}