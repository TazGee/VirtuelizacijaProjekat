using Common.Models;
using System;
using System.Collections.Generic;

namespace Client.Processing
{
    public class BatchBuilder
    {
        public List<List<LoadSample>> BuildBatches(List<LoadSample> samples, int batchSize)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (batchSize <= 0)
            {
                throw new ArgumentException("BatchSize mora biti veći od 0.");
            }

            List<List<LoadSample>> batches = new List<List<LoadSample>>();

            for (int i = 0; i < samples.Count; i += batchSize)
            {
                int count = Math.Min(batchSize, samples.Count - i);
                batches.Add(samples.GetRange(i, count));
            }

            return batches;
        }
    }
}