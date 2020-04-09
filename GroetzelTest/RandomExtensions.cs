using System;
using System.Collections.Generic;

namespace GroetzelTest
{
    public static class RandomExtensions
    {
        public static float NextFloat(this Random rnd, float min = 0, float max = 1)
        {
            var amplitude = max - min;
            var val = rnd.NextDouble() * amplitude + min;
            return (float)val;
        }

        public static IEnumerable<float> NextFloats(this Random rnd, int length, float min = 0, float max = 1)
        {
            for (int i = 0; i < length; i++)
            {
                yield return rnd.NextFloat(min, max);
            }
        }
    }
}
