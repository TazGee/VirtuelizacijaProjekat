namespace Server.Events
{
    public class BatchReceivedEventArgs : System.EventArgs
    {
        public int BatchSize { get; private set; }
        public int TotalReceived { get; private set; }
        public string Message { get; private set; }

        public BatchReceivedEventArgs(int batchSize, int totalReceived, string message)
        {
            BatchSize = batchSize;
            TotalReceived = totalReceived;
            Message = message;
        }
    }
}