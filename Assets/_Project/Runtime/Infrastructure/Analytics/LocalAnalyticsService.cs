using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PlanetIO.Infrastructure.Analytics
{
    public readonly struct AnalyticsRecord
    {
        public readonly string Name;
        public readonly (string Key, object Value)[] Parameters;
        public readonly float Time;

        public AnalyticsRecord(string name, (string Key, object Value)[] parameters, float time)
        {
            Name = name;
            Parameters = parameters;
            Time = time;
        }
    }

    public sealed class LocalAnalyticsService : IAnalyticsService
    {
        public const int Capacity = 200;

        private readonly Queue<AnalyticsRecord> _records = new();

        public IReadOnlyCollection<AnalyticsRecord> Records => _records;

        public void Track(string eventName, params (string Key, object Value)[] parameters)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            while (_records.Count >= Capacity)
            {
                _records.Dequeue();
            }

            (string Key, object Value)[] copy = parameters ?? System.Array.Empty<(string, object)>();
            _records.Enqueue(new AnalyticsRecord(eventName, copy, Time.realtimeSinceStartup));

            if (Debug.isDebugBuild)
            {
                Debug.Log(Format(eventName, copy));
            }
        }

        private static string Format(string eventName, (string Key, object Value)[] parameters)
        {
            var builder = new StringBuilder("[Analytics] ").Append(eventName);
            foreach ((string key, object value) in parameters)
            {
                builder.Append(' ').Append(key).Append('=').Append(value);
            }

            return builder.ToString();
        }
    }
}
