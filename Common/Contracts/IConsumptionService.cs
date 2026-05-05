using Common.Faults;
using Common.Models;
using System.Collections.Generic;
using System.ServiceModel;

namespace Common.Contracts
{
    [ServiceContract]
    public interface IConsumptionService
    {
        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        ServiceResponse StartSession(SessionData meta);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        ServiceResponse PushBatch(List<LoadSample> samples);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        ServiceResponse EndSession();
    }
}