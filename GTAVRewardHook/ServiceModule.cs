using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAVRewardHook
{
    public class ServiceModule : NancyModule
    {
        public ServiceModule()
        {
            Get["/test"] = (x => "Ok");
        }
    }
}
