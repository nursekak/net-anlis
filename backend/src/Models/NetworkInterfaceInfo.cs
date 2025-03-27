using System.Net.NetworkInformation;

namespace Backend.Models
{
    public class NetworkInterfaceInfo
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string IpAddress { get; set; }
        public required string SubnetMask { get; set; }
        public required string MacAddress { get; set; }
        public required OperationalStatus Status { get; set; }
        public required long Speed { get; set; }
        public required NetworkInterfaceType InterfaceType { get; set; }
    }
} 