using System;
using System.Collections.Generic;
using System.IO;

namespace Client.CSV
{
    public class CsvRow
    {
        public int RowIndex { get; set; }
        public string OriginalLine { get; set; }
        public string[] Values { get; set; }
    }

    public class CsvReader
    {
        public string[] ReadHeaders(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("CSV fajl ne postoji.", path);
            }

            using (StreamReader reader = new StreamReader(path))
            {
                string headerLine = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    throw new Exception("CSV fajl je prazan.");
                }

                return headerLine.Split(',');
            }
        }

        public IEnumerable<CsvRow> ReadRows(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("CSV fajl ne postoji.", path);
            }

            using (StreamReader reader = new StreamReader(path))
            {
                reader.ReadLine();

                string line;
                int rowIndex = 1;

                while ((line = reader.ReadLine()) != null)
                {
                    rowIndex++;

                    yield return new CsvRow
                    {
                        RowIndex = rowIndex,
                        OriginalLine = line,
                        Values = line.Split(',')
                    };
                }
            }
        }
    }
}