using Client.Filters;
using Client.Processing;
using Client.Storage;
using Common.Models;
using System;
using System.Collections.Generic;

namespace Client.CSV
{
    public class CsvParser
    {
        private readonly string csvPath;
        private readonly string countryCode;
        private readonly DateTime selectedDate;

        private readonly CsvReader csvReader;
        private readonly CsvValidator csvValidator;
        private readonly DataFilter dataFilter;
        private readonly SampleBuilder sampleBuilder;

        public CsvParser(string csvPath, string countryCode, DateTime selectedDate)
        {
            this.csvPath = csvPath;
            this.countryCode = countryCode;
            this.selectedDate = selectedDate.Date;

            csvReader = new CsvReader();
            csvValidator = new CsvValidator();
            dataFilter = new DataFilter();
            sampleBuilder = new SampleBuilder();
        }

        public List<LoadSample> Parse()
        {
            List<LoadSample> samples = new List<LoadSample>();

            string[] headers = csvReader.ReadHeaders(csvPath);

            csvValidator.ValidateRequiredColumns(
                headers,
                countryCode,
                out int utcIndex,
                out int localIndex,
                out int actualIndex,
                out int forecastIndex);

            double cumulativeMWh = 0;

            using (RejectedWriter rejectedWriter = new RejectedWriter("rejected_client.csv"))
            {
                foreach (CsvRow row in csvReader.ReadRows(csvPath))
                {
                    if (string.IsNullOrWhiteSpace(row.OriginalLine))
                    {
                        rejectedWriter.WriteRejected("Empty row", row.OriginalLine);
                        continue;
                    }

                    if (!csvValidator.HasEnoughColumns(row.Values, utcIndex, localIndex, actualIndex, forecastIndex))
                    {
                        rejectedWriter.WriteRejected("Invalid column count", row.OriginalLine);
                        continue;
                    }

                    string utcText = row.Values[utcIndex];
                    string localText = row.Values[localIndex];
                    string actualText = row.Values[actualIndex];
                    string forecastText = row.Values[forecastIndex];

                    if (!csvValidator.TryParseDate(utcText, out DateTime timestampUtc))
                    {
                        rejectedWriter.WriteRejected("Invalid utc_timestamp", row.OriginalLine);
                        continue;
                    }

                    if (!csvValidator.TryParseDate(localText, out DateTime timestampLocal))
                    {
                        rejectedWriter.WriteRejected("Invalid cet_cest_timestamp", row.OriginalLine);
                        continue;
                    }

                    if (!dataFilter.IsSelectedDate(timestampLocal, selectedDate))
                    {
                        continue;
                    }

                    if (!csvValidator.TryParseDouble(actualText, out double actualMW))
                    {
                        rejectedWriter.WriteRejected("Invalid ActualMW", row.OriginalLine);
                        continue;
                    }

                    if (!csvValidator.TryParseDouble(forecastText, out double forecastMW))
                    {
                        rejectedWriter.WriteRejected("Invalid ForecastMW", row.OriginalLine);
                        continue;
                    }

                    if (actualMW < 0)
                    {
                        rejectedWriter.WriteRejected("ActualMW is negative", row.OriginalLine);
                        continue;
                    }

                    if (forecastMW < 0)
                    {
                        rejectedWriter.WriteRejected("ForecastMW is negative", row.OriginalLine);
                        continue;
                    }

                    double energyMWh = actualMW * 0.25;
                    cumulativeMWh += energyMWh;

                    LoadSample sample = sampleBuilder.Build(
                        timestampUtc,
                        timestampLocal,
                        actualMW,
                        forecastMW,
                        cumulativeMWh,
                        countryCode,
                        row.RowIndex);

                    samples.Add(sample);
                }
            }

            return samples;
        }
    }
}