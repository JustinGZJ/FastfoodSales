using HslCommunication.Core;

namespace DAQ
{
    public interface IReadWriteFactory
    {
        IReadWriteNet GetReadWriteNet();
        string AddressA { get; }
        string AddressB { get; }
    }
}
