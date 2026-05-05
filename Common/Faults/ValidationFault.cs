using System.Runtime.Serialization;

namespace Common.Faults
{
    [DataContract]
    public class ValidationFault
    {
        public ValidationFault()
        {
        }

        public ValidationFault(string message)
        {
            Message = message;
        }

        [DataMember]
        public string Message { get; set; }
    }
}