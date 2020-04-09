using System;
using System.Linq;
using System.Collections.Generic;

namespace GroetzelTest
{
    public class CTCSSDecoder
    {
        //needs to be in ascending order
        private static IList<float> ValidTones = new float[]{67f,
                                                        69.3f,
                                                        71.9f,
                                                        74.4f,
                                                        77f,
                                                        79.7f,
                                                        82.5f,
                                                        85.4f,
                                                        88.5f,
                                                        91.5f,
                                                        94.8f,
                                                        97.4f,
                                                        100f,
                                                        103.5f,
                                                        107.2f,
                                                        110.9f,
                                                        114.8f,
                                                        123f,
                                                        127.3f,
                                                        131.8f,
                                                        136.5f,
                                                        141.3f,
                                                        146.2f,
                                                        150f,
                                                        151.4f,
                                                        156.7f,
                                                        159.8f,
                                                        162.2f,
                                                        165.5f,
                                                        167.9f,
                                                        171.3f,
                                                        173.8f,
                                                        177.3f,
                                                        179.9f,
                                                        183.5f,
                                                        186.2f,
                                                        188.8f,
                                                        189.9f,
                                                        192.8f,
                                                        196.6f,
                                                        199.5f,
                                                        203.5f,
                                                        206.5f,
                                                        210.7f,
                                                        213.8f,
                                                        218.1f,
                                                        221.3f,
                                                        225.7f,
                                                        229.1f,
                                                        233.6f,
                                                        237.1f,
                                                        241.8f,
                                                        245.5f,
                                                        250.3f,
                                                        254.1f};
        private readonly Goertzel _goertzel;
        private readonly int _frameSize;
        private readonly float _binWidthInHz;

        public int FrameSize => _frameSize;


        private CTCSSDecoder(Goertzel goertzel, int frameSize, float binWidthInHz)
        {
            _binWidthInHz = binWidthInHz;
            _goertzel = goertzel;
            _frameSize = frameSize;
        }

        public static CTCSSDecoder Create(float tone, int sampleRate)
        {
            if (!ValidTones.Contains(tone))
                throw new ArgumentOutOfRangeException(nameof(tone), @"is not a valid CTCSS tone");

            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate), @"value must be greater than 0");

            float binWidthInHz = CalculateBinWidthForTone(tone);
            float numberOfSamplesForRequiredBinWidth = sampleRate / binWidthInHz;
            float samplesRequiredForOnePEriod = sampleRate / tone;
            int frameSize = (int)Math.Round(numberOfSamplesForRequiredBinWidth + (numberOfSamplesForRequiredBinWidth % samplesRequiredForOnePEriod));
            var frequenciesOfInterest = new[] { tone - binWidthInHz, tone, tone + binWidthInHz };

            var lowPass = new LowPassFilter(sampleRate, tone + 10);

            var goertzel = Goertzel.Create(frameSize, sampleRate, FFTWindowBuilder.Hann(frameSize), lowPass.Process, frequenciesOfInterest);

            return new CTCSSDecoder(goertzel, frameSize, binWidthInHz);
        }

        private static float CalculateBinWidthForTone(float tone)
        {
            //int indexOfTone = ValidTones.IndexOf(tone);
            //float binWidth = 0f;

            //if (indexOfTone == 0)
            //    binWidth = ValidTones[indexOfTone + 1] - ValidTones[indexOfTone];
            //else if (indexOfTone == ValidTones.Count - 1)
            //    binWidth = ValidTones[indexOfTone] - ValidTones[indexOfTone - 1];
            //else
            //binWidth = Math.Min(ValidTones[indexOfTone + 1] - ValidTones[indexOfTone], ValidTones[indexOfTone] - ValidTones[indexOfTone - 1]);

            float binWidth = ValidTones[1] - ValidTones[0];
            for (int i = 2; i < ValidTones.Count; i++)
            {
                var temp = ValidTones[i] - ValidTones[i - 1];
                if (temp < binWidth) binWidth = temp;
            }

            //return binWidth;
            return binWidth * 6f/3f;
            return (float)Math.Floor(binWidth);
        }

        public bool HasCTCSS(float[] samples, out float level, out float ratio)
        {
            _goertzel.Reset();
            var responses = _goertzel.Evaluate(samples).ToArray();
            var integrate = responses.Sum();
            var max = responses.Max();
            ratio = (responses[1] / integrate) / _goertzel.WindowCorr;
            level = responses[1];
            bool res = responses[1] == max && responses[1] >= 0.01 && ratio >= 0.9f;
            return res;

            //var sum = responses.Sum();
            //var max = responses.Max();
            //ratio = (responses[1] / sum);
            //bool res = responses[1] == max && ratio >= 0.6f;
            //return res;
        }
    }
}
