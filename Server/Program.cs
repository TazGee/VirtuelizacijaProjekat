using Server.Services;
using System;
using System.ServiceModel;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(ConsumptionService)))
            {
                host.Open();

                Console.WriteLine("Server je pokrenut.");
                Console.WriteLine("Pritisni ENTER za gasenje servera.");
                Console.ReadLine();

                host.Close();
            }
        }
    }
}