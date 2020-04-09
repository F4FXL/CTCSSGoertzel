using System;
using System.Linq;

namespace GroetzelTest
{
    public static class FFTWindowBuilder
    {
        public static float[] Hamming(int numberOfSamples)
        {
            var window = new float[numberOfSamples];
            var numberOfSamplesMinus1 = numberOfSamples - 1;
            for (int i = 0; i < numberOfSamples; i++)
            {
                window[i] = (float)(0.54 - 0.46 * Math.Cos(2 * Math.PI * i / numberOfSamplesMinus1));
            }
            return window;
        }

        public static float[] Blackmann(int numberOfSamples)
        {
            var window = new float[numberOfSamples];
            var numberOfSamplesMinus1 = numberOfSamples - 1;
            for (int i = 0; i < numberOfSamples; i++)
            {
                window[i] = (float)(0.42 - 0.5 * Math.Cos(2.0 * Math.PI * i / numberOfSamplesMinus1) + 0.08 * Math.Cos(4.0 * Math.PI * i / numberOfSamplesMinus1));
            }
            return window;
        }

        public static float[] Hann(int numberOfSamples)
        {
            var window = new float[numberOfSamples];
            var numberOfSamplesMinus1 = numberOfSamples - 1;
            for (int i = 0; i < numberOfSamples; i++)
            {
                window[i] = (float)(0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / numberOfSamplesMinus1)));
            }
            return window;
        }

        public static float[] Door(int numberOfSamples)
        {
            return Enumerable.Repeat(1f, numberOfSamples).ToArray();
        }
    }
}
