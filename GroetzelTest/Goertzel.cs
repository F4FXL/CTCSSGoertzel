using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace GroetzelTest
{
    public class Goertzel
    {
        private readonly int _sampleRate;
        private readonly float[] _window;
        private readonly int[] _ks;
        private readonly float[] _omegas;
        private readonly float[] _sines;
        private readonly float[] _cosines;
        private readonly float[] _coeffs;
        private float[] _q0s;
        private float[] _q1s;
        private float[] _q2s;
        private readonly int _dataLength;
        private readonly float[] _frequenciesOfInterest;
        private readonly float _windowCorr;

        public float WindowCorr => _windowCorr;

        private readonly Func<float, float> _samplePreprocess;

        private Goertzel(int dataLength, int sampleRate, float[] window, float[] frequenciesOfInterest, Func<float, float> samplePreprocess)
        {
            _samplePreprocess = samplePreprocess ?? ((arg) => arg);
            _frequenciesOfInterest = frequenciesOfInterest;
            _dataLength = dataLength;
            _window = window;
            _sampleRate = sampleRate;

            _ks = frequenciesOfInterest.Select(frequency => (int)(0.5f + ((dataLength * frequency) / sampleRate))).ToArray();
            _omegas = _ks.Select(k => (float)((2.0 * Math.PI * k) / dataLength)).ToArray();
            _sines = _omegas.Select(o => (float)Math.Sin(o)).ToArray();
            _cosines = _omegas.Select(o => (float)Math.Cos(o)).ToArray();
            _coeffs = _cosines.Select(c => 2.0f * c).ToArray();
            _windowCorr = window.Average();

            Reset();
        }

        public static Goertzel Create(int dataLength, int sampleRate, float[] window, Func<float, float> samplePreprocess, params float[] frequenciesOfInterest)
        {
            if (dataLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(dataLength), @"value must be greater than 0");

            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate), @"value must be greater than 0");

            if (window == null)
                throw new ArgumentNullException(nameof(window));

            if (frequenciesOfInterest == null)
                throw new ArgumentNullException(nameof(frequenciesOfInterest));

            if (frequenciesOfInterest.Length == 0)
                throw new InvalidOperationException($"{nameof(frequenciesOfInterest)} contains no elements");

            return new Goertzel(dataLength, sampleRate, window, frequenciesOfInterest, samplePreprocess);
        }

        public void Reset()
        {
            _q0s = Enumerable.Repeat(0f, _frequenciesOfInterest.Length).ToArray();
            _q1s = Enumerable.Repeat(0f, _frequenciesOfInterest.Length).ToArray();
            _q2s = Enumerable.Repeat(0f, _frequenciesOfInterest.Length).ToArray();
        }

        public IEnumerable<float> Evaluate(float[] samples)
        {
            if (samples == null)
                throw new ArgumentNullException(nameof(samples));

            if (samples.Length != _dataLength)
                throw new ArgumentException($"Invalid length, shall be equal to ({_dataLength}", nameof(samples));

            float scalingFactor = (samples.Length / 2.0f) * _windowCorr;
            float min = samples[0];
            float max = samples[0];

            for (int i = 0; i < samples.Length; i++)
            {
                var currentSample = _samplePreprocess(samples[i]);// * _window[i];

                if (currentSample < min) min = currentSample;
                if (currentSample > max) max = currentSample;

                for (int j = 0; j < _frequenciesOfInterest.Length; j++)
                {
                    _q0s[j] = _coeffs[j] * _q1s[j] - _q2s[j] + (currentSample * _window[i]);
                    _q2s[j] = _q1s[j];
                    _q1s[j] = _q0s[j];
                }
            }

            for (int i = 0; i < _q1s.Length; i++)
            {
                // calculate the real and imaginary results
                // scaling appropriately
                float real = (_q1s[i] * _cosines[i] - _q2s[i]) / scalingFactor;
                float imag = (_q1s[i] * _sines[i]) / scalingFactor;

                float magnitude = (float)(Math.Sqrt(real * real + imag * imag));
                magnitude /= (max - min);
                //float magnitude = (float)Math.Sqrt(_q1s[i] * _q1s[i] + _q2s[i] * _q2s[i] - (_q1s[i] * _q2s[i] * _coeffs[i]));

                yield return magnitude;
            }
        }
    }
}
