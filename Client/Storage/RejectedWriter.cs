using System;
using System.IO;

namespace Client.Storage
{
    public class RejectedWriter : IDisposable
    {
        private readonly StreamWriter writer;
        private bool disposed;

        public RejectedWriter(string path)
        {
            writer = new StreamWriter(path, false);
            writer.WriteLine("Reason,OriginalRow");
        }

        public void WriteRejected(string reason, string originalRow)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("RejectedWriter");
            }

            if (originalRow == null)
            {
                originalRow = string.Empty;
            }

            originalRow = originalRow.Replace("\"", "'");

            writer.WriteLine($"\"{reason}\",\"{ originalRow}\"");
        }

        public void Dispose()
        {
            if (!disposed)
            {
                writer.Dispose();
                disposed = true;
            }
        }
    }
}