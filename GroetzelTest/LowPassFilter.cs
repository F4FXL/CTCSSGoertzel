using System;
namespace GroetzelTest
{
    public class LowPassFilter
    {
        private readonly float _dt;
        private readonly float _rc;
        private bool _firstCall;
        private float _previousOutput;
        private float _alpha;

        public LowPassFilter(int sampleRate, float cutoffFrequencyInHz)
        {
            _rc = (float)(1.0 / (cutoffFrequencyInHz * 2.0 * Math.PI));
            _dt = 1f / sampleRate;
            _alpha = _dt / (_rc + _dt);
            _firstCall = true;
        }

        public float Process(float sample)
        {
            if(_firstCall)
            {
                _firstCall = false;
                _previousOutput = sample;
                return sample;
            }

            var output = _previousOutput + (_alpha * (sample - _previousOutput));
            _previousOutput = output;
            return output;
        }
    }
}
