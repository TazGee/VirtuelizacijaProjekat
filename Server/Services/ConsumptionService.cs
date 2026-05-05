using Common.Contracts;
using Common.Faults;
using Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;

namespace Server.Services
{
    public class ConsumptionService : IConsumptionService
    {
        private static SessionData currentSession;
        private static string sessionDirectory;
        private static string sessionFilePath;
        private static double lastCumulativeMWh = -1;

        public ServiceResponse StartSession(SessionData meta)
        {
            if (meta == null)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Session metadata je null."));
            }

            if (string.IsNullOrWhiteSpace(meta.CountryCode))
            {
                throw new FaultException<ValidationFault>(new ValidationFault("CountryCode je neophodan."));
            }

            currentSession = meta;
            lastCumulativeMWh = -1;

            string dateFolder = meta.Date.ToString("yyyy-MM-dd");

            sessionDirectory = Path.Combine("Data", meta.CountryCode, dateFolder);
            Directory.CreateDirectory(sessionDirectory);

            sessionFilePath = Path.Combine(sessionDirectory, "session.csv");

            if (!File.Exists(sessionFilePath))
            {
                File.WriteAllText(sessionFilePath, "RowIndex,TimestampUtc,TimestampLocal,ActualMW,ForecastMW,CumulativeMWh,CountryCode" + Environment.NewLine);
            }

            Console.WriteLine($"Transfer pokrenut: {meta.CountryCode} {dateFolder}");

            return new ServiceResponse
            {
                Success = true,
                Message = "Sessija pokrenuta."
            };
        }

        public ServiceResponse PushBatch(List<LoadSample> samples)
        {
            if (currentSession == null)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Sessija nije pokrenuta. Prvo pokreni sesiju."));
            }

            if (samples == null || samples.Count == 0)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Batch je prazan."));
            }

            using (StreamWriter writer = new StreamWriter(sessionFilePath, true))
            {
                foreach (LoadSample sample in samples)
                {
                    ValidateSample(sample);

                    writer.WriteLine($"{sample.RowIndex},{sample.TimestampUtc:o},{sample.TimestampLocal:o},{sample.ActualMW},{sample.ForecastMW},{sample.CumulativeMWh},{sample.CountryCode}");

                    lastCumulativeMWh = sample.CumulativeMWh;
                }
            }

            Console.WriteLine($"Batch je dobio: {samples.Count} samplova.");

            return new ServiceResponse
            {
                Success = true,
                Message = $"Batch je dobio: {samples.Count} samplova."
            };
        }

        public ServiceResponse EndSession()
        {
            if (currentSession == null)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Nema aktivnih sesija."));
            }

            Console.WriteLine("Transfer zavrsen.");

            currentSession = null;
            sessionDirectory = null;
            sessionFilePath = null;
            lastCumulativeMWh = -1;

            return new ServiceResponse
            {
                Success = true,
                Message = "Transfer zavrsen."
            };
        }

        private void ValidateSample(LoadSample sample)
        {
            if (sample == null)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Sample je prazan."));
            }

            if (sample.TimestampUtc == default(DateTime))
            {
                throw new FaultException<ValidationFault>(new ValidationFault("TimestampUtc nije validan."));
            }

            if (sample.TimestampLocal == default(DateTime))
            {
                throw new FaultException<ValidationFault>(new ValidationFault("TimestampLocal nije validan."));
            }

            if (sample.ActualMW < 0)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("ActualMW mora biti veci ili jednak 0."));
            }

            if (sample.ForecastMW < 0)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("ForecastMW mora biti veci ili jednak 0."));
            }

            if (sample.CumulativeMWh < lastCumulativeMWh)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("CumulativeMWh mora monotono rasti tokom dana."));
            }

            if (sample.CountryCode != currentSession.CountryCode)
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Sample CountryCode nije isti kao session CountryCode."));
            }
        }
    }
}