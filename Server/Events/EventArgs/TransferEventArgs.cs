using System;

namespace Server.Events
{
    public class TransferEventArgs : System.EventArgs
    {
        public string CountryCode { get; private set; }
        public DateTime Date { get; private set; }
        public string Message { get; private set; }

        public TransferEventArgs(string countryCode, DateTime date, string message)
        {
            CountryCode = countryCode;
            Date = date;
            Message = message;
        }
    }
}