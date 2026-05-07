using System;
using System.IO;

namespace Server.Storage
{
    public class ServerRejectedWriter
    {
        private readonly string rejectsPath;

        public ServerRejectedWriter(string sessionDirectory)
        {
            rejectsPath = Path.Combine(sessionDirectory, "rejects.csv");

            if (!File.Exists(rejectsPath) || new FileInfo(rejectsPath).Length == 0)
            {
                File.WriteAllText(rejectsPath, "Reason,OriginalRow" + Environment.NewLine);
            }
        }

        public void WriteRejected(string reason, string originalRow)    
        {
            if (originalRow == null)
            {
                originalRow = string.Empty;
            }

            originalRow = originalRow.Replace("\"", "'");

            using (StreamWriter writer = new StreamWriter(rejectsPath, true))
            {
                writer.WriteLine("\"" + reason + "\",\"" + originalRow + "\"");
            }
        }
    }
}