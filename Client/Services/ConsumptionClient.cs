using Client.Processing;
using Common.Contracts;
using Common.Faults;
using Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;

namespace Client.Services
{
    public class ConsumptionClient
    {
        public void SendSamples(
            List<LoadSample> samples,
            string countryCode,
            DateTime selectedDate,
            string sourceFileName,
            int batchSize)
        {
            if (samples == null || samples.Count == 0)
            {
                Console.WriteLine("Nema validnih uzoraka za slanje.");
                return;
            }

            ChannelFactory<IConsumptionService> factory = null;
            IConsumptionService proxy = null;

            try
            {
                factory = new ChannelFactory<IConsumptionService>("ConsumptionServiceEndpoint");
                proxy = factory.CreateChannel();

                SessionData sessionData = new SessionData
                {
                    CountryCode = countryCode,
                    Date = selectedDate,
                    SourceFileName = Path.GetFileName(sourceFileName),
                    TotalSamples = samples.Count,
                    BatchSize = batchSize
                };

                ServiceResponse startResponse = proxy.StartSession(sessionData);
                Console.WriteLine(startResponse.Message);

                BatchBuilder batchBuilder = new BatchBuilder();
                List<List<LoadSample>> batches = batchBuilder.BuildBatches(samples, batchSize);

                int sentCount = 0;

                foreach (List<LoadSample> batch in batches)
                {
                    ServiceResponse batchResponse = proxy.PushBatch(batch);

                    sentCount += batch.Count;

                    Console.WriteLine(batchResponse.Message);
                    Console.WriteLine("Poslato: " + sentCount + "/" + samples.Count);
                }

                ServiceResponse endResponse = proxy.EndSession();
                Console.WriteLine(endResponse.Message);

                CloseProxy(proxy);
                factory.Close();
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine("ValidationFault: " + ex.Detail.Message);
                AbortProxy(proxy, factory);
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine("DataFormatFault: " + ex.Detail.Message);
                AbortProxy(proxy, factory);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška pri slanju podataka: " + ex.Message);
                AbortProxy(proxy, factory);
            }
        }

        private void CloseProxy(IConsumptionService proxy)
        {
            IClientChannel channel = proxy as IClientChannel;

            if (channel != null)
            {
                channel.Close();
            }
        }

        private void AbortProxy(IConsumptionService proxy, ChannelFactory<IConsumptionService> factory)
        {
            IClientChannel channel = proxy as IClientChannel;

            if (channel != null)
            {
                channel.Abort();
            }

            if (factory != null)
            {
                factory.Abort();
            }
        }
    }
}