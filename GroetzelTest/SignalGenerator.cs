using System;
using System.Collections.Generic;
using System.Linq;

namespace GroetzelTest
{
    public static class SignalGenerator
    {
        public static IEnumerable<float> Sinus(int sampleRate, float durationInS, float frequency, float phaseShiftInRadian = 0)
        {
            return Sinus(sampleRate, (int)(sampleRate * durationInS), frequency, phaseShiftInRadian);
        }

        public static IEnumerable<float> Sinus(int sampleRate, int numberOfSamples, float frequency, float phaseShiftInRadian = 0)
        {
            var numSamples = numberOfSamples;

            for (int i = 0; i < numSamples; i++)
            {
                yield return (float)Math.Sin((Math.PI * 2.0 * frequency * (i / (double)sampleRate)) + phaseShiftInRadian);
            }
        }


        public static IEnumerable<float> AddNoise(this IEnumerable<float> samples, float noiseAmplitude)
        {
            if (noiseAmplitude <= 0f)
                return samples;
            
            var rand = new Random();
            return samples.Select(s => s * (1f - noiseAmplitude)  + rand.NextFloat(-1, 1) * noiseAmplitude);
        }

        public static IEnumerable<float> GenerateNoise(int length)
        {
            var rand = new Random();
            return rand.NextFloats(length, -1, 1);
        }
    }
}
