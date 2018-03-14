using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAVRewardHook
{
    public class GameEpisode
    {
        public long EpisodeID { get; private set; }
        public long WindowStart { get; private set; }
        public long EpisodeSize { get; private set; }

        public ISet<DrivingEvent> Events { get; set; }
        public double AvgSpeed { get; set; }
        public double AvgRoadAlignment { get; set; }

        public GameEpisode(long episodeID, long windowSize = 10000)
        {
            EpisodeID = episodeID;
            WindowStart = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
            EpisodeSize = windowSize;
            Events = new HashSet<DrivingEvent>();
        }
    }

    public enum DrivingEvent
    {
        NONE,
        DRIVING_AGAINST_TRAFFIC,
        DRIVING_ON_PAVEMENT,
        HIT_VEHICLE,
        HIT_PEDESTRIAN,
        HIT_OTHER,

        // Partial support
        RUNNING_RED_LIGHT,

        // Not implemented
        DRIVING_OFFROAD,
        SCARED_NPC
    }
}
