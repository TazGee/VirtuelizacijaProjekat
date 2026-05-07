using System;
using Server.Services;

namespace Server.Events
{
    public class ConsoleEventSubscriber
    {
        public ConsoleEventSubscriber(ConsumptionService service)
        {
            service.OnTransferStarted += OnTransferStarted;
            service.OnBatchReceived += OnBatchReceived;
            service.OnTransferCompleted += OnTransferCompleted;
            service.OnWarningRaised += OnWarningRaised;
        }

        private void OnTransferStarted(object sender, TransferEventArgs e)
        {
            Console.WriteLine("[EVENT] Transfer started: " + e.CountryCode + " " + e.Date.ToString("yyyy-MM-dd"));
        }

        private void OnBatchReceived(object sender, BatchReceivedEventArgs e)
        {
            Console.WriteLine("[EVENT] Batch received. Batch size: " + e.BatchSize + ", total received: " + e.TotalReceived);
        }

        private void OnTransferCompleted(object sender, TransferEventArgs e)
        {
            Console.WriteLine("[EVENT] Transfer completed: " + e.CountryCode + " " + e.Date.ToString("yyyy-MM-dd"));
        }

        private void OnWarningRaised(object sender, WarningEventArgs e)
        {
            Console.WriteLine("[WARNING] " + e.WarningType + " | " + e.Message);
        }
    }
}