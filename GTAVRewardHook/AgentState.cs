using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAVRewardHook
{
    class AgentState
    {
        public Boolean HitVehicle { get; set; }
        public Boolean HitPedestrian { get; set; }        
        public Boolean DrivingOnPavement { get; set; }
        public Boolean DrivingWrongSide { get; set; }
        public float RoadAlignment { get; set; }
    }
}
