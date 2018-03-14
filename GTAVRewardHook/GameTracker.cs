
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTAVRewardHook
{
    public class GameTrackerScript : Script
    {
        public RollingTimeSeries<GameEpisode> EpisodeTracker;
        public event EventHandler EpisodeReset;

        private Stopwatch stopwatch;
        private GameEpisode currentEpisode;
        private Thread serverThread;

        // Event trackers
        private List<IDrivingEventTracker> eventTrackers;
        private Dictionary<String, IDrivingMetricTracker> metricTrackers;
        
        public GameTrackerScript()
        {
            EpisodeTracker = new RollingTimeSeries<GameEpisode>(1024);

            // Setup trackers
            eventTrackers = new List<IDrivingEventTracker> {
                new TimeSinceEventTracker(this, Hash.GET_TIME_SINCE_PLAYER_DROVE_AGAINST_TRAFFIC, DrivingEvent.DRIVING_AGAINST_TRAFFIC),
                new TimeSinceEventTracker(this, Hash.GET_TIME_SINCE_PLAYER_DROVE_ON_PAVEMENT,     DrivingEvent.DRIVING_ON_PAVEMENT),
                new TimeSinceEventTracker(this, Hash.GET_TIME_SINCE_PLAYER_HIT_PED,               DrivingEvent.HIT_PEDESTRIAN),
                new TimeSinceEventTracker(this, Hash.GET_TIME_SINCE_PLAYER_HIT_VEHICLE,           DrivingEvent.HIT_VEHICLE),
                new CollisionTracker(this),
                // Experimental
                new RanLightsTracker(this)
            };
            metricTrackers = new Dictionary<String, IDrivingMetricTracker>
            {
                {"avg_speed", new SpeedTracker(this)},
                {"avg_road_alignment", new RoadAlignmentTracker(this)}
            };

            serverThread = new Thread(new ThreadStart(Server.StartHost));
            serverThread.Start();

            Tick += TrackEpisodeProgress;

            stopwatch = Stopwatch.StartNew();
            currentEpisode = new GameEpisode(0);
        }

        void TrackEpisodeProgress(Object sender, EventArgs args)
        {
            // Add all non-void events to current state
            currentEpisode.Events.UnionWith(
                eventTrackers.ConvertAll(l => l.Value())
                .FindAll(e => e != DrivingEvent.NONE));

            // If episode complete, complete and push state object
            if (stopwatch.ElapsedMilliseconds > currentEpisode.EpisodeSize)
            {
                // Create in-game notifications
                UI.Notify("Episode Complete");
                UI.Notify(String.Format("[{0}] Avg Speed: {1}", currentEpisode.EpisodeID, metricTrackers["avg_speed"].Value()));
                UI.Notify(String.Format("[{0}] Avg Road Alignment: {1}", currentEpisode.EpisodeID, metricTrackers["avg_road_alignment"].Value()));
                foreach (var driveEvent in currentEpisode.Events)
                    UI.Notify(String.Format("[{0}] {1}", currentEpisode.EpisodeID, driveEvent.ToString()), true);

                // Update state object, and store
                currentEpisode.AvgSpeed = metricTrackers["avg_speed"].Value();
                currentEpisode.AvgRoadAlignment = metricTrackers["avg_road_alignment"].Value();

                EpisodeTracker.AddObservation(currentEpisode);
                currentEpisode = new GameEpisode(currentEpisode.EpisodeID + 1);

                // Trigger episode completion event
                stopwatch.Restart();
                EpisodeReset.Invoke(null, null);
            }
        }
    }

    // Tracks a specific statistic through Ticks
    interface IStatTracker<T> {
        void Tick(Object sender, EventArgs args);
        T Value();
        void ResetEpisode(Object sender, EventArgs args);
    }

    interface IDrivingEventTracker : IStatTracker<DrivingEvent> { };

    interface IDrivingMetricTracker : IStatTracker<double> { };

    class SpeedTracker : IDrivingMetricTracker
    {
        private Aggregator avgSpeed;

        public SpeedTracker(GameTrackerScript script)
        {
            avgSpeed = new Aggregator();
            script.Tick += Tick;
            script.EpisodeReset += ResetEpisode;
        }

        public void Tick(object sender, EventArgs args)
        {
            if (Game.Player.Character.IsInVehicle())
                avgSpeed.Observe(Game.Player.Character.CurrentVehicle.Speed);
            else
                avgSpeed.Reset();
        }

        public double Value()
        {
            return avgSpeed.Value();
        }

        public void ResetEpisode(object sender, EventArgs args)
        {
            avgSpeed.Reset();
        }
    }

    class TimeSinceEventTracker : IDrivingEventTracker
    {
        private bool eventOccurred = false;
        private Hash engineCallback;
        private DrivingEvent eventType;

        public TimeSinceEventTracker(GameTrackerScript script, Hash engineCallback, DrivingEvent eventType)
        {
            script.Tick += Tick;
            script.EpisodeReset += ResetEpisode;
            this.engineCallback = engineCallback;
            this.eventType = eventType;
        }

        public void Tick(object sender, EventArgs args)
        {
            eventOccurred = Function.Call<int>(engineCallback) == 0;
        }

        public string Stat()
        {
            return eventType.ToString().ToLower();
        }

        public DrivingEvent Value()
        {
            return eventOccurred ? eventType : DrivingEvent.NONE;
        }

        public void ResetEpisode(object sender, EventArgs args) { }

    }

    class CollisionTracker : IDrivingEventTracker
    {
        private bool collisionOccurred;
        private int lastHealth = int.MaxValue;

        public CollisionTracker(GameTrackerScript script)
        {
            script.Tick += Tick;
            script.EpisodeReset += ResetEpisode;
        }

        public void Tick(object sender, EventArgs args)
        {
            collisionOccurred = false;
            var character = Game.Player.Character;
            if (character.IsInVehicle())
            {
                // Test 1 - Check if vehicle has more damage than last tick
                lastHealth = Math.Min(lastHealth, character.CurrentVehicle.MaxHealth);
                if (character.CurrentVehicle.Health < lastHealth)
                {
                    collisionOccurred = true;
                    lastHealth = character.CurrentVehicle.Health;
                }

                // Test 2 - Check if any entity exists that has been damaged by player
                if (!collisionOccurred)
                    foreach (var entity in World.GetNearbyEntities(character.Position, 10.0f))
                        if (entity.HasBeenDamagedBy(character))
                            collisionOccurred = true;

                if (!collisionOccurred)
                    foreach (var prop in World.GetNearbyProps(character.Position, 10.0f))
                        if (prop.HasBeenDamagedBy(character))
                            collisionOccurred = true;
            }
        }

        public DrivingEvent Value()
        {
            return collisionOccurred ? DrivingEvent.HIT_OTHER : DrivingEvent.NONE;
        }

        public void ResetEpisode(object sender, EventArgs args)
        {
            var character = Game.Player.Character;
            lastHealth = character.IsInVehicle() ? character.CurrentVehicle.Health : int.MaxValue;
        }
    }

    class RoadAlignmentTracker : IDrivingMetricTracker
    {
        private Aggregator roadAlignment;
        
        public RoadAlignmentTracker(GameTrackerScript script)
        {
            roadAlignment = new Aggregator();
            script.EpisodeReset += ResetEpisode;
            script.Tick += Tick;
        }

        public void Tick(object sender, EventArgs args)
        {
            var character = Game.Player.Character;
            if (character.IsInVehicle())
            {
                var velocityVector = 14.0f * (0.9f * character.CurrentVehicle.Velocity.Normalized + 0.1f * character.CurrentVehicle.ForwardVector).Normalized;
                var pos0 = World.GetNextPositionOnStreet(character.Position);
                var pos1 = World.GetNextPositionOnStreet(character.Position + velocityVector);
                roadAlignment.Observe(Vector3.Dot((pos1 - pos0).Normalized, velocityVector.Normalized));
            }
        }

        public double Value()
        {
            return roadAlignment.Value();
        }

        public void ResetEpisode(object sender, EventArgs args)
        {
            roadAlignment.Reset();
        }
    };

    class RanLightsTracker : IDrivingEventTracker
    {
        private bool runningRedLight;
        private HashSet<Vehicle> alignedVehicles;

        public RanLightsTracker(GameTrackerScript script)
        {
            script.Tick += Tick;
            script.EpisodeReset += ResetEpisode;
            alignedVehicles = new HashSet<Vehicle>();
        }

        public void Tick(object sender, EventArgs args){
            var character = Game.Player.Character;
            alignedVehicles.RemoveWhere(ped => ped.Position.DistanceTo(character.Position) > 30);
            if (character.IsInVehicle())
            {
                var pVehicle = character.CurrentVehicle;

                /* Add all vehicles that:
                 * 1. Have a driver != player
                 * 2. Have same heading
                 * 3. Are behind the player
                 */

                var vehicles = new List<Vehicle>(
                    World.GetNearbyVehicles(character.Position, 20))
                    .FindAll(vehicle => vehicle.Driver.Exists() || vehicle.Driver != character)
                    .FindAll(vehicle => Vector3.Dot(Utils.Heading(pVehicle), Utils.Heading(vehicle)) >= Math.Cos(20))
                    .FindAll(vehicle => Vector3.Dot(Utils.Heading(pVehicle), (pVehicle.Position - vehicle.Position).Normalized) > Math.Cos(60));

                alignedVehicles.UnionWith(vehicles);
            }

            var votes = 0;
            foreach (var vehicle in alignedVehicles)
                if (Function.Call<bool>(Hash.IS_VEHICLE_STOPPED_AT_TRAFFIC_LIGHTS, vehicle))
                    votes += 1;

            runningRedLight = votes > 0.7 * alignedVehicles.Count;

        }

        public DrivingEvent Value()
        {
            return runningRedLight ? DrivingEvent.RUNNING_RED_LIGHT : DrivingEvent.NONE;
        }

        public void ResetEpisode(object sender, EventArgs args) { }

    }
}
