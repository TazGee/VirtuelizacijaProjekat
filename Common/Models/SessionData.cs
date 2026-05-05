using System;
using System.Runtime.Serialization;

namespace Common.Models
{
    [DataContract]
    public class SessionData
    {
        [DataMember]
        public string CountryCode { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public string SourceFileName { get; set; }

        [DataMember]
        public int TotalSamples { get; set; }

        [DataMember]
        public int BatchSize { get; set; }
    }
}
