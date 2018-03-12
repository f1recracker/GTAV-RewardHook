using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAVRewardHook
{
    // TODO Can we make this class generic?
    public class Aggregator
    {
        public Double Sum { get; private set; }
        public int Observations { get; private set; }

        public Aggregator()
        {
            Reset();
        }

        private Aggregator(Double sum, int observations)
        {
            Sum = sum;
            Observations = observations;
        }

        public void Observe(double observation)
        {
            Sum += observation;
            Observations += 1;
        }

        public Double Value()
        {
            if (Observations == 0)
                return 0;
            return Sum / Observations;
        }

        public void Reset()
        {
            Sum = 0;
            Observations = 0;
        }

        public static Aggregator operator +(Aggregator left, Double observation)
        {
            return new Aggregator(left.Sum + observation, left.Observations + 1);
        }

        public static Aggregator operator +(Aggregator left, Aggregator right)
        {
            return new Aggregator(left.Sum + right.Sum, left.Observations + right.Observations);
        }
    }

    public class RollingTimeSeries<T>
    {
        public int HistorySize { get; private set; }

        private long[] timestamps;
        private T[] dataFrames;
        private int index;

        public RollingTimeSeries(int historySize) {
            HistorySize = historySize;
            timestamps = new long[historySize];
            dataFrames = new T[historySize];
            index = 0;
        }

        public void AddObservation(T observation)
        {
            dataFrames[index] = observation;
            timestamps[index] = new DateTimeOffset().ToUnixTimeMilliseconds();
            index = (index + 1) % HistorySize;
        }

        public T Get(long time)
        {
            int index1 = Array.BinarySearch(timestamps, 0, this.index, time);
            int index2 = Array.BinarySearch(timestamps, this.index, HistorySize - this.index, time);
            int index = Math.Abs(timestamps[index1] - time) < Math.Abs(timestamps[index2] - time) ? index1 : index2;
            return dataFrames[index];
        }
    }
}
