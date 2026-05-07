using Common.Contracts;
using Common.Faults;
using Common.Models;
using Server.Analytics;
using Server.Config;
using Server.Events;
using Server.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;

namespace Server.Services
{
    public class ConsumptionService : IConsumptionService
    {
        public event EventHandler<TransferEventArgs> OnTransferStarted;
        public event EventHandler<BatchReceivedEventArgs> OnBatchReceived;
        public event EventHandler<TransferEventArgs> OnTransferCompleted;
        public event EventHandler<WarningEventArgs> OnWarningRaised;

        private static SessionData currentSession;
        private static string sessionDirectory;
        private static string sessionFilePath;
        private static double lastCumulativeMWh = -1;
        private static int totalReceivedSamples = 0;

        private static ServerSettingsReader settings = new ServerSettingsReader();
        private static ConsumptionAnalytics analytics = new ConsumptionAnalytics(settings);
        private static ServerRejectedWriter rejectedWriter;

        private readonly ConsoleEventSubscriber subscriber;

        public ConsumptionService()
        {
            subscriber = new ConsoleEventSubscriber(this);
        }

        public ServiceResponse StartSession(SessionData meta)
        {
            if (meta == null)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Session data is null."));
            }

            if (string.IsNullOrWhiteSpace(meta.CountryCode))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("CountryCode is required."));
            }

            currentSession = meta;
            lastCumulativeMWh = -1;
            totalReceivedSamples = 0;

            string dateFolder = meta.Date.ToString("yyyy-MM-dd");

            sessionDirectory = Path.Combine("Data", meta.CountryCode, dateFolder);
            Directory.CreateDirectory(sessionDirectory);

            sessionFilePath = Path.Combine(sessionDirectory, "session.csv");

            File.WriteAllText(
                sessionFilePath,
                "RowIndex,TimestampUtc,TimestampLocal,ActualMW,ForecastMW,CumulativeMWh,CountryCode" + Environment.NewLine);

            rejectedWriter = new ServerRejectedWriter(sessionDirectory);

            Console.WriteLine("Transfer started: " + meta.CountryCode + " " + dateFolder);

            RaiseTransferStarted(meta.CountryCode, meta.Date, "Transfer started.");

            return new ServiceResponse
            {
                Success = true,
                Message = "Transfer started."
            };
        }

        public ServiceResponse PushBatch(List<LoadSample> samples)
        {
            if (currentSession == null)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Session is not started. Call StartSession first."));
            }

            if (samples == null || samples.Count == 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Batch is empty."));
            }

            using (StreamWriter writer = new StreamWriter(sessionFilePath, true))
            {
                foreach (LoadSample sample in samples)
                {
                    string validationError;

                    if (!TryValidateSample(sample, out validationError))
                    {
                        string originalRow = ConvertSampleToCsvLine(sample);

                        if (rejectedWriter != null)
                        {
                            rejectedWriter.WriteRejected(validationError, originalRow);
                        }

                        throw new FaultException<ValidationFault>(
                            new ValidationFault(validationError));
                    }

                    writer.WriteLine(ConvertSampleToCsvLine(sample));

                    lastCumulativeMWh = sample.CumulativeMWh;
                }
            }

            totalReceivedSamples += samples.Count;

            Console.WriteLine("blok primljen: " + samples.Count + " uzoraka.");

            RaiseBatchReceived(samples.Count, totalReceivedSamples);

            List<WarningEventArgs> warnings = analytics.AnalyzeBatch(samples);

            foreach (WarningEventArgs warning in warnings)
            {
                RaiseWarning(warning);
            }

            return new ServiceResponse
            {
                Success = true,
                Message = "blok primljen"
            };
        }

        public ServiceResponse EndSession()
        {
            if (currentSession == null)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("No active session."));
            }

            string countryCode = currentSession.CountryCode;
            DateTime date = currentSession.Date;

            Console.WriteLine("prenos završen.");

            RaiseTransferCompleted(countryCode, date, "Transfer completed.");

            currentSession = null;
            sessionDirectory = null;
            sessionFilePath = null;
            rejectedWriter = null;
            lastCumulativeMWh = -1;
            totalReceivedSamples = 0;

            return new ServiceResponse
            {
                Success = true,
                Message = "prenos završen"
            };
        }

        private bool TryValidateSample(LoadSample sample, out string error)
        {
            error = string.Empty;

            if (sample == null)
            {
                error = "Sample is null.";
                return false;
            }

            if (sample.TimestampUtc == default(DateTime))
            {
                error = "TimestampUtc is not valid.";
                return false;
            }

            if (sample.TimestampLocal == default(DateTime))
            {
                error = "TimestampLocal is not valid.";
                return false;
            }

            if (sample.ActualMW < 0)
            {
                error = "ActualMW must be greater than or equal to 0.";
                return false;
            }

            if (sample.ForecastMW < 0)
            {
                error = "ForecastMW must be greater than or equal to 0.";
                return false;
            }

            if (sample.CumulativeMWh < lastCumulativeMWh)
            {
                error = "CumulativeMWh must grow monotonically inside the day.";
                return false;
            }

            if (sample.CountryCode != currentSession.CountryCode)
            {
                error = "Sample CountryCode does not match session CountryCode.";
                return false;
            }

            return true;
        }

        private string ConvertSampleToCsvLine(LoadSample sample)
        {
            if (sample == null)
            {
                return "";
            }

            return sample.RowIndex + "," +
                   sample.TimestampUtc.ToString("o") + "," +
                   sample.TimestampLocal.ToString("o") + "," +
                   sample.ActualMW + "," +
                   sample.ForecastMW + "," +
                   sample.CumulativeMWh + "," +
                   sample.CountryCode;
        }

        private void RaiseTransferStarted(string countryCode, DateTime date, string message)
        {
            if (OnTransferStarted != null)
            {
                OnTransferStarted(this, new TransferEventArgs(countryCode, date, message));
            }
        }

        private void RaiseBatchReceived(int batchSize, int totalReceived)
        {
            if (OnBatchReceived != null)
            {
                OnBatchReceived(this, new BatchReceivedEventArgs(
                    batchSize,
                    totalReceived,
                    "Batch received."));
            }
        }

        private void RaiseTransferCompleted(string countryCode, DateTime date, string message)
        {
            if (OnTransferCompleted != null)
            {
                OnTransferCompleted(this, new TransferEventArgs(countryCode, date, message));
            }
        }

        private void RaiseWarning(WarningEventArgs warning)
        {
            if (OnWarningRaised != null)
            {
                OnWarningRaised(this, warning);
            }
        }
    }
}