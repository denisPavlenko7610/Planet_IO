using System.Collections;
using UnityEngine;

namespace PlanetIO.Tests
{
    internal static class AwaitableTestRunner
    {
        public static IEnumerator Run(Awaitable operation)
        {
            var awaiter = operation.GetAwaiter();
            while (!awaiter.IsCompleted)
            {
                yield return null;
            }

            awaiter.GetResult();
        }
    }
}
