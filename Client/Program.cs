using Common.Contracts;
using Common.Faults;
using Common.Models;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<IConsumptionService> factory = null;
            IConsumptionService proxy = null;

            try
            {
                factory = new ChannelFactory<IConsumptionService>("ConsumptionServiceEndpoint");
                proxy = factory.CreateChannel();

                SessionData meta = new SessionData
                {
                    CountryCode = "DE",
                    Date = DateTime.Today,
                    SourceFileName = "time_series_15min_singleindex.csv",
                    TotalSamples = 2,
                    BatchSize = 16
                };

                ServiceResponse startResponse = proxy.StartSession(meta);
                Console.WriteLine(startResponse.Message);

                List<LoadSample> samples = new List<LoadSample>
                {
                    new LoadSample
                    {
                        TimestampUtc = DateTime.UtcNow,
                        TimestampLocal = DateTime.Now,
                        ActualMW = 100,
                        ForecastMW = 120,
                        CumulativeMWh = 25,
                        CountryCode = "DE",
                        RowIndex = 1
                    },
                    new LoadSample
                    {
                        TimestampUtc = DateTime.UtcNow.AddMinutes(15),
                        TimestampLocal = DateTime.Now.AddMinutes(15),
                        ActualMW = 110,
                        ForecastMW = 125,
                        CumulativeMWh = 52.5,
                        CountryCode = "DE",
                        RowIndex = 2
                    }
                };

                ServiceResponse batchResponse = proxy.PushBatch(samples);
                Console.WriteLine(batchResponse.Message);

                ServiceResponse endResponse = proxy.EndSession();
                Console.WriteLine(endResponse.Message);

                ((IClientChannel)proxy).Close();
                factory.Close();
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine("Validation fault: " + ex.Detail.Message);
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine("Data format fault: " + ex.Detail.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška: " + ex.Message);

                if (proxy != null)
                {
                    ((IClientChannel)proxy).Abort();
                }

                if (factory != null)
                {
                    factory.Abort();
                }
            }

            Console.WriteLine("Kraj testa.");
            Console.ReadLine();
        }
    }
}