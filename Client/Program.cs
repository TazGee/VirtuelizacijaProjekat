using Client.Config;
using Client.CSV;
using Client.Services;
using Common.Models;
using System;
using System.Collections.Generic;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                AppSettingsReader settings = new AppSettingsReader();

                Console.WriteLine("CSV path: " + settings.CsvPath);
                Console.WriteLine("Country: " + settings.CountryCode);
                Console.WriteLine("Selected date: " + settings.SelectedDate.ToString("yyyy-MM-dd"));
                Console.WriteLine("Batch size: " + settings.BatchSize);
                Console.WriteLine();

                CsvParser parser = new CsvParser(
                    settings.CsvPath,
                    settings.CountryCode,
                    settings.SelectedDate);

                List<LoadSample> samples = parser.Parse();

                Console.WriteLine("Validnih uzoraka: " + samples.Count);
                Console.WriteLine("Problematični redovi su upisani u rejected_client.csv.");
                Console.WriteLine();

                ConsumptionClient consumptionClient = new ConsumptionClient();

                consumptionClient.SendSamples(
                    samples,
                    settings.CountryCode,
                    settings.SelectedDate,
                    settings.CsvPath,
                    settings.BatchSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška: " + ex.Message);
            }

            Console.WriteLine();
            Console.WriteLine("Client završen.");
            Console.ReadLine();
        }
    }
}