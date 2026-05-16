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
            ValidateSessionFormat(meta);
            ValidateSessionRules(meta);

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

            ValidateBatchFormat(samples);
            ValidateBatchRules(samples);

            using (StreamWriter writer = new StreamWriter(sessionFilePath, true))
            {
                foreach (LoadSample sample in samples)
                {
                    string formatError;
                    string validationError;

                    if (!TryValidateSampleFormat(sample, out formatError))
                    {
                        string originalRow = ConvertSampleToCsvLine(sample);
                        WriteRejected(formatError, originalRow);

                        throw new FaultException<DataFormatFault>(
                            new DataFormatFault(formatError));
                    }

                    if (!TryValidateSampleRules(sample, out validationError))
                    {
                        string originalRow = ConvertSampleToCsvLine(sample);
                        WriteRejected(validationError, originalRow);

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

        private void ValidateSessionFormat(SessionData meta)
        {
            if (meta == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("StartSession message has invalid format: session data is null."));
            }

            if (string.IsNullOrWhiteSpace(meta.CountryCode))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("StartSession message has invalid format: CountryCode is missing."));
            }

            if (meta.Date == default(DateTime))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("StartSession message has invalid format: Date is not valid."));
            }

            if (string.IsNullOrWhiteSpace(meta.SourceFileName))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("StartSession message has invalid format: SourceFileName is missing."));
            }
        }

        private void ValidateSessionRules(SessionData meta)
        {
            if (meta.TotalSamples <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("TotalSamples must be greater than 0."));
            }

            if (meta.BatchSize <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("BatchSize must be greater than 0."));
            }
        }

        private void ValidateBatchFormat(List<LoadSample> samples)
        {
            if (samples == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("PushBatch message has invalid format: samples list is null."));
            }

            if (string.IsNullOrWhiteSpace(sessionFilePath))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Server session file path is not initialized."));
            }
        }

        private void ValidateBatchRules(List<LoadSample> samples)
        {
            if (samples.Count == 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Batch is empty."));
            }
        }

        private bool TryValidateSampleFormat(LoadSample sample, out string error)
        {
            error = string.Empty;
            if (sample == null)
            {
                error = "Sample has invalid format: sample is null.";
                return false;
            }
            if (sample.TimestampUtc == default(DateTime))
            {
                error = "Sample has invalid format: TimestampUtc is not valid.";
                return false;
            }
            if (sample.TimestampLocal == default(DateTime))
            {
                error = "Sample has invalid format: TimestampLocal is not valid.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(sample.CountryCode))
            {
                error = "Sample has invalid format: CountryCode is missing.";
                return false;
            }
            if (double.IsNaN(sample.ActualMW) || double.IsInfinity(sample.ActualMW))
            {
                error = "Sample has invalid format: ActualMW is NaN or Infinity.";
                return false;
            }
            if (double.IsNaN(sample.ForecastMW) || double.IsInfinity(sample.ForecastMW))
            {
                error = "Sample has invalid format: ForecastMW is NaN or Infinity.";
                return false;
            }
            if (double.IsNaN(sample.CumulativeMWh) || double.IsInfinity(sample.CumulativeMWh))
            {
                error = "Sample has invalid format: CumulativeMWh is NaN or Infinity.";
                return false;
            }
            return true;
        }

        private bool TryValidateSampleRules(LoadSample sample, out string error)
        {
            error = string.Empty;
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

        private void WriteRejected(string reason, string originalRow)
        {
            if (rejectedWriter != null)
            {
                rejectedWriter.WriteRejected(reason, originalRow);
            }
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
