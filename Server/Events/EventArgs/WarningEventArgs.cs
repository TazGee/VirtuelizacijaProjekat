namespace Server.Events
{
    public class WarningEventArgs : System.EventArgs
    {
        public string WarningType { get; private set; }
        public string CountryCode { get; private set; }
        public int Hour { get; private set; }
        public double Value { get; private set; }
        public string Direction { get; private set; }
        public string Message { get; private set; }

        public WarningEventArgs(
            string warningType,
            string countryCode,
            int hour,
            double value,
            string direction,
            string message)
        {
            WarningType = warningType;
            CountryCode = countryCode;
            Hour = hour;
            Value = value;
            Direction = direction;
            Message = message;
        }
    }
}