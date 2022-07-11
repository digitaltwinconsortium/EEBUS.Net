
using System;

namespace EEBUS
{
    public class SPINE
    {
        private ulong _counter = 0;

        public object GenerateDatagram(string destination, ulong reference = 0)
        {
            DatagramType datagram = new DatagramType();
            
            datagram.header = new SpineHeaderType();
            
            datagram.header.addressSource = new FeatureAddressType();
            datagram.header.addressSource.device = "MICROSOFT-Azure-EEBUS-Gateway-100";
            datagram.header.addressSource.entity = new uint[1];
            datagram.header.addressSource.entity[0] = 0;
            datagram.header.addressSource.feature = 0;
            datagram.header.addressSource.featureSpecified = true;

            datagram.header.addressDestination = new FeatureAddressType();
            datagram.header.addressDestination.device = destination;
            datagram.header.addressDestination.entity = new uint[1];
            datagram.header.addressDestination.entity[0] = 0;
            datagram.header.addressDestination.feature = 0;
            datagram.header.addressDestination.featureSpecified = true;

            datagram.header.specificationVersion = "1.0";
            
            datagram.header.msgCounter = _counter++;

            if (reference > 0)
            {
                datagram.header.msgCounterReference = reference;
            }

            datagram.header.timestamp = DateTime.UtcNow.ToString();

            datagram.header.ackRequest = false;

            datagram.header.cmdClassifierSpecified = false;

            datagram.payload = new CmdType[1];
            // TODO: Set payload

            return datagram;
        }
    }
}
