#define COMPLEX
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace GroetzelTest
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            const int sampleRate = 48000;
            const float tone = 69.3f;

            var decoder = CTCSSDecoder.Create(tone, sampleRate);

            using (var udpClient = new UdpClient(7355))
            {
                var buffer = new CircularBuffer.CircularBuffer<float>(decoder.FrameSize, new float[0]);
                var ctcssPrev = false;
                int debounce = 0;
                int debounceMax = 3;
                var slicer = new Slicer<float>(48000 / 50);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while(true)
                {
                    IPEndPoint recievedFrom = new IPEndPoint(IPAddress.Any, 0);
                    var data = udpClient.Receive(ref recievedFrom);
                    //Console.WriteLine(data.Length);
                    foreach(var slice in slicer.GetSlices(ToFloats(data)))
                    {
                        foreach (var sample in slice)
                        {
                            buffer.PushBack(sample);
                        }

                        if (!buffer.IsFull) continue;

                        var ctcss = decoder.HasCTCSS(buffer.ToArray(), out var level, out var ratio);
                        if(ctcss)
                        {
                            debounce = Math.Min(debounce + 1, debounceMax);
                        }
                        else
                        {
                            debounce = Math.Max(debounce - 1, 0);
                        }

                        Console.Clear();
                        Console.WriteLine($"{debounce >= debounceMax} {level} {ratio}");
                    }
                }
            }

            //int positive = 0;
            //int negative = 0;
            //var noiseThreshold = 0f;
            //for (int i = 0; i < 100; i++)
            //{
            //    var noise = i / 100f;
            //    var signal = SignalGenerator.Sinus(sampleRate, decoder.FrameSize, 67)
            //    .Select(f => f * 0.15f)
            //    .AddNoise(noise).ToArray();

            //    if (decoder.HasCTCSS(signal))
            //    {
            //        positive++;
            //        if(negative == 0)
            //             noiseThreshold = noise;
            //    }
            //    else
            //    {
            //        negative++;
            //    }

            //    Console.Clear();
            //    Console.WriteLine($"Positive {positive}");
            //    Console.WriteLine($"Negative {negative}");
            //}

            //Console.WriteLine(noiseThreshold);



         
            //float frequencyOfInterest = 67;
            //float binWidthInHz = 4f;
            //float numberOfSamplesForRequiredBinWidth = sampleRate / binWidthInHz;
            //float samplesRequiredForOnePEriod = sampleRate / frequencyOfInterest;
            //int framesSize = (int)Math.Round(numberOfSamplesForRequiredBinWidth + (numberOfSamplesForRequiredBinWidth % samplesRequiredForOnePEriod));

            //var signal = SignalGenerator.Sinus(sampleRate, framesSize, 69.3f).Select(f => f * 0.15f)/*.AddNoise(0.9f)*/.ToArray();
            //var frequenciesOfInterest = new[] { frequencyOfInterest - binWidthInHz, frequencyOfInterest, frequencyOfInterest + binWidthInHz };

            //var goertzel = Goertzel.Create(signal.Length, sampleRate, FFTWindowBuilder.Hann(signal.Length), frequenciesOfInterest);
            //var responses = goertzel.Evaluate(signal).ToArray();

            //foreach (var response in responses)
            //{
            //    Console.WriteLine(response);
            //}

            //Console.WriteLine("=============================");

            //var bla = (responses[0] + responses[2]) / 2f;
            //bla = responses[1] / bla;
            //Console.WriteLine(bla);

            //var file = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/test.csv";

            //if (File.Exists(file))
            //    File.Delete(file);

            //File.WriteAllLines(file, CompareFilterWindowsPhaseResponse());


            //const float frequency = 67;
            //var signal = SignalGenerator.GenerateNoise((int)(((1f / frequency) * 10f) * 48000f) + 1).ToArray(); 
            //var max = signal.Max();
            //var min = signal.Min();

            //var amp1f = GoertzelMag(signal, 48000, frequency);
            //var amp2f = GoertzelMag(signal, 48000, frequency * 2);
            //var amp4f = GoertzelMag(signal, 48000, frequency * 4);

            //Console.WriteLine(amp1f);
            //Console.WriteLine(amp2f);
            //Console.WriteLine(amp4f);
        }

        public static unsafe float[]ToFloats(byte[] bytes)
        {
            var ret = new float[(bytes.Length/2)];
            int j = 0;
            fixed(byte * ptr = bytes)
            {
                for (int i = 0; i < bytes.Length; i += 2)
                {
                    var s = *((short*)(void*)(ptr + i));
                    ret[j++] = s;
                }
            }
            return ret;
        }

        public static IEnumerable<string> CompareResponseWithNoiseLevel()
        {
            const float filterFrequency = 150;
            const float frameSizeInS = 0.1f;
            const int sampleRate = 48000;
            var signal = SignalGenerator.Sinus(sampleRate, frameSizeInS, filterFrequency).ToArray();
            var window = FFTWindowBuilder.Hann(signal.Length);

            for (int i = 0; i <= 100; i++)
            {
                var noisedSignal = signal.AddNoise(i / 100f).ToArray();
                var response = GoertzelMag(noisedSignal, sampleRate, filterFrequency, window, out _);
                yield return $"{i};{response.ToString(CultureInfo.InvariantCulture)}";
            }
        }

        public static IEnumerable<string> CompareFilterWindows()
        {
            const float filterFrequency = 150;
            const float frameSizeInS = 0.1f;
            const int sampleRate = 48000;
            int windowSize = (int)(frameSizeInS * sampleRate);
            var windows = new[]{
                FFTWindowBuilder.Blackmann(windowSize),
                FFTWindowBuilder.Hamming(windowSize),
                FFTWindowBuilder.Hann(windowSize),
                FFTWindowBuilder.Door(windowSize)
            };
            var responses = new float[windows.Length];

            for (int i = 1; i < 300; i++)
            {
                var signal = SignalGenerator.Sinus(sampleRate, frameSizeInS, i).ToArray();
                for (int j = 0; j < windows.Length; j++)
                {
                    responses[j] = GoertzelMag(signal, sampleRate, filterFrequency, windows[j], out _);
                }
                yield return $"{i};" + string.Join(";", responses.Select(r => r.ToString(CultureInfo.InvariantCulture)));
            }
        }

        public static IEnumerable<string> CompareFilterWindowsPhaseResponse()
        {
            const float filterFrequency = 150;
            const float frameSizeInS = (1f / filterFrequency) * 15;
            const int sampleRate = 48000;
            int windoSize = (int)(frameSizeInS * sampleRate);
            var windows = new[]{
                FFTWindowBuilder.Blackmann(windoSize),
                FFTWindowBuilder.Hamming(windoSize),
                FFTWindowBuilder.Hann(windoSize),
                FFTWindowBuilder.Door(windoSize)
            };

            var phases = new float[windows.Length];

            for (int i = 1; i < 300; i++)
            {
                var signal = SignalGenerator.Sinus(sampleRate, frameSizeInS, i, (float)(Math.PI / 2f)).ToArray();
                for (int j = 0; j < windows.Length; j++)
                {
                    GoertzelMag(signal, sampleRate, filterFrequency, windows[j], out phases[j]);
                }
                yield return $"{i};" + string.Join(";", phases.Select(r => r.ToString(CultureInfo.InvariantCulture)));
            }
        }

        public static IEnumerable<string> CompareSamplingRates()
        {
            const float filterFrequency = 150;
            const float frameSizeInS = 0.1f;
            var sampleRates = new[] { 600, 8000, 48000 };
            var hammings = sampleRates.Select(sr => FFTWindowBuilder.Hamming((int)(sr * frameSizeInS))).ToArray();
            var responses = new float[sampleRates.Length];

            for (int i = 1; i < 300; i++)
            {
                for (int j = 0; j < sampleRates.Length;j++)
                {
                    var signal = SignalGenerator.Sinus(sampleRates[j], frameSizeInS, i).ToArray();
                    responses[j] = GoertzelMag(signal, sampleRates[j], filterFrequency, hammings[j], out _);
                }

                yield return $"{i};" + string.Join(";", responses.Select(r => r.ToString(CultureInfo.InvariantCulture)));
            }
        }

        public static IEnumerable<string> CompareFrameSizes()
        {
            const float filterFrequency = 150;
            var frameSizesInS = new float[] { 0.1f, 0.5f };
            var sampleRate = 48000;
            var hammings = frameSizesInS.Select(fs => FFTWindowBuilder.Hamming((int)(fs * sampleRate))).ToArray();
            var responses = new float[frameSizesInS.Length];

            for (int i = 1; i < 300; i++)
            {
                for (int j = 0; j < frameSizesInS.Length; j++)
                {
                    var signal = SignalGenerator.Sinus(sampleRate, frameSizesInS[j], i).ToArray();
                    responses[j] = GoertzelMag(signal, sampleRate, filterFrequency, hammings[j], out _);
                }

                yield return $"{i};" + string.Join(";", responses.Select(r => r.ToString(CultureInfo.InvariantCulture)));
            }
        }

        public static float GoertzelMag(float[] data, int sampleRate, float frequency, float[] window, out float phase)
        {
            int k, i;
            float omega, sine, cosine, coeff, q0, q1, q2, magnitude;
#if COMPLEX
            float real, imag;
            float scalingFactor = data.Length / 2.0f;
#endif

            k = (int)(0.5f + ((data.Length * frequency) / (float)sampleRate));
            omega = (float)((2.0f * Math.PI * k) / data.Length);
            sine = (float)Math.Sin(omega);
            cosine = (float)Math.Cos(omega);
            coeff = 2.0f * cosine;
            q0 = 0;
            q1 = 0;
            q2 = 0;

            for (i = 0; i < data.Length; i++)
            {
                q0 = coeff * q1 - q2 + (data[i] * window[i]);
                q2 = q1;
                q1 = q0;
            }

#if COMPLEX
            // calculate the real and imaginary results
            // scaling appropriately
            real = (q1 * cosine - q2) / scalingFactor;
            imag = (q1 * sine) / scalingFactor;

            magnitude = (float)Math.Sqrt(real * real + imag * imag);
            phase = (float)(Math.Atan(imag / real) % (2.0 * Math.PI));
#else
            magnitude = (float)Math.Sqrt(q1 * q1 + q2 * q2 - (q1 * q2 * coeff));
#endif
            return magnitude;
        }
    }
}
