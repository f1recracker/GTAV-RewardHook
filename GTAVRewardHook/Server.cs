using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAVRewardHook
{
    class Server
    {
        public static HostConfiguration hostConfig = new HostConfiguration()
        {
            UrlReservations = new UrlReservations() { CreateAutomatically = true }
        };

        public static void StartHost()
        {
            using (var host = new NancyHost(hostConfig, new Uri("http://localhost:31730")))
            {
                host.Start();
                while (true);
            }
        }
    }
}
