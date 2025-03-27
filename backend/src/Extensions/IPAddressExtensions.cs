using System.Net;

namespace Backend.Extensions
{
    public static class IPAddressExtensions
    {
        public static bool IsPrivate(this IPAddress address)
        {
            var bytes = address.GetAddressBytes();
            
            // 10.0.0.0 - 10.255.255.255
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0 - 172.31.255.255
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0 - 192.168.255.255
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            return false;
        }
    }
} 