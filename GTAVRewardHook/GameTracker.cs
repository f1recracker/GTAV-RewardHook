
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
            };
            metricTrackers = new Dictionary<String, IDrivingMetricTracker>
            {
                {"avg_speed", new SpeedTracker(this)},
                {"avg_road_alignment", new RoadAlignmentTracker(this)}
            };

            //Tick += Test;
            Tick += TrackEpisodeProgress;

            stopwatch = Stopwatch.StartNew();
            currentEpisode = new GameEpisode(0);
        }

        // Dummy code for red-light detection
        // It sort of works, but it depends if any other NPC is also parked

        //private ISet<Ped> copyCats = new HashSet<Ped>();

        //private void Test(object sender, EventArgs e)
        //{
        //    var pChar = Game.Player.Character;
        //    var distantPeds = new HashSet<Ped>();
        //    foreach (var copyCat in copyCats)
        //    {
        //        if (Vector3.Distance(copyCat.Position, pChar.Position) > 30)
        //        {
        //            copyCat.CurrentBlip.Remove();
        //            distantPeds.Add(copyCat);
        //        }
        //    }
        //    copyCats.ExceptWith(distantPeds);

        //    if (pChar.IsInVehicle())
        //    {
        //        var pVehicle = pChar.CurrentVehicle;
        //        var pPosition = pChar.CurrentVehicle.Position;
        //        var pVelocity = (0.5f * pVehicle.Velocity.Normalized + 0.5f * pVehicle.ForwardVector.Normalized).Normalized;
        //        foreach (var oVehicle in World.GetNearbyVehicles(pChar.Position, 20))
        //        {
        //            var oPosition = oVehicle.Position;
        //            var oVelocty = (0.5f * oVehicle.Velocity.Normalized + 0.5f * oVehicle.ForwardVector.Normalized).Normalized;
        //            if (oVehicle.Driver.Exists() && oVehicle.Driver != pChar && Vector3.Dot(oVelocty, pVelocity) > 0.7 && Vector3.Dot(pVelocity, (pPosition - oPosition).Normalized) > 0.5f && !copyCats.Contains(oVehicle.Driver))
        //            {
        //                copyCats.Add(oVehicle.Driver);
        //                oVehicle.Driver.AddBlip();
        //                var sequence = new TaskSequence();
        //                sequence.AddTask.CruiseWithVehicle(pVehicle, pVehicle.Speed, 786603);
        //                oVehicle.Driver.Task.PerformSequence(sequence);
        //            }
        //        }
        //    }
        //    var votes = 0;
        //    foreach (var copyCat in copyCats)
        //    {
        //        Utils.DrawBox(copyCat.Position + new Vector3(0, 0, 2), 0.3f, Color.Cyan);
        //        Utils.DrawLine(copyCat.Position, pChar.Position, Color.HotPink);
        //        if (Function.Call<bool>(Hash.IS_VEHICLE_STOPPED_AT_TRAFFIC_LIGHTS, copyCat.CurrentVehicle))
        //            votes += 1;
        //    }
        //    if (votes > 0.5f * copyCats.Count)
        //    {
        //        UI.ShowSubtitle("Running Red!");
        //    }
        //}

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

}
