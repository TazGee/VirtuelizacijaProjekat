using System.Runtime.Serialization;

namespace Common.Faults
{
    [DataContract]
    public class DataFormatFault
    {
        public DataFormatFault()
        {
        }

        public DataFormatFault(string message)
        {
            Message = message;
        }

        [DataMember]
        public string Message { get; set; }
    }
}