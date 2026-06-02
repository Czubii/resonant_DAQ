
using CncMeasurement.Core.models;

namespace CncMeasurement.MockHardware
{
    public sealed class ModalWaveformGenerator
    {
        private double _time;

        // Persisted across chunks (fixes discontinuities)
        private double[]? _lpState;
        private double[]? _lpAlpha;
        private int _lpChannelCount;
        private double _lpSampleRate;

        // Persisted across chunks (fixes random changes between chunks)
        private double[,]? _participation;
        private int _partChannelCount;
        private int _partModeCount;

        // Optional: stable RNG for repeatability (instead of Random.Shared)
        private readonly Random _rand = new Random(12345);

        public sealed record Mode(
            double FnHz,              // natural frequency (Hz)
            double DampingRatio,      // zeta (0..1), typical 0.002..0.05
            double ModalAmplitude,    // amplitude scale
            double PhaseRad = 0.0     // phase at impact
        );

        public sealed record ChannelModel(
            double Gain = 1.0,
            double DelaySeconds = 0.0,
            double NoiseStd = 0.02,
            double DcOffset = 0.0,
            double DriftPerSecond = 0.0,
            double LowpassHz = 0.0    // 0 = disabled
        );

        /// <summary>
        /// Generates one chunk of multi-channel impact response.
        /// Important: This generator is stateful. Keep the same instance to get continuous time-series.
        /// </summary>
        public double[,] GenerateWaveform(
            AcquisitionConfig config,
            double impactTimeSeconds = 2.0,        // absolute time in generator timeline
            double impactWidthSeconds = 0.0008,    // gaussian sigma-ish
            double impactAmplitude = 20.0,
            IReadOnlyList<Mode>? modes = null,
            IReadOnlyList<ChannelModel>? channels = null,
            double crossTalk = 0.03
        )
        {
            int channelCount = config.ChannelConfigs.Count;
            int count = config.ChunkSize;

            double[,] samples = new double[channelCount, count];
            double sampleRate = config.SampleRate;
            double dt = 1.0 / sampleRate;

            modes ??= new[]
            {
                new Mode(FnHz: 200,  DampingRatio: 0.005, ModalAmplitude: 34, PhaseRad: 0.7),
                new Mode(FnHz: 400,  DampingRatio: 0.0001, ModalAmplitude: 34, PhaseRad: 0.7),
                new Mode(FnHz: 1000,  DampingRatio: 0.01, ModalAmplitude: 25, PhaseRad: 0.2),
                new Mode(FnHz: 3200,  DampingRatio: 0.09, ModalAmplitude: 34, PhaseRad: 0.7),
                new Mode(FnHz: 4269,  DampingRatio: 0.002, ModalAmplitude: 21, PhaseRad: -0.2),
    
            };

            channels ??= BuildDefaultChannels(channelCount);

            EnsureLowpassState(channelCount, sampleRate, channels, dt);
            EnsureParticipation(channelCount, modes.Count);

            // Generate
            for (int i = 0; i < count; i++)
            {
                double t = _time + i * dt;

                // Channel loop
                for (int ch = 0; ch < channelCount; ch++)
                {
                    var chModel = channels[ch];

                    // Apply arrival delay (time-of-flight)
                    double tc = t - chModel.DelaySeconds;

                    // Impact (gaussian pulse)
                    double impact = impactAmplitude * chModel.Gain *
                                    GaussianPulse(tc, impactTimeSeconds, impactWidthSeconds);

                    // Modal ringdown
                    double ringdown = 0.0;
                    if (tc >= impactTimeSeconds)
                    {
                        double dT = tc - impactTimeSeconds;

                        for (int m = 0; m < modes.Count; m++)
                        {
                            var mode = modes[m];

                            // Clamp damping ratio to valid underdamped range
                            double zeta = Math.Clamp(mode.DampingRatio, 0.0001, 0.99);

                            double wn = 2.0 * Math.PI * mode.FnHz;
                            double wd = wn * Math.Sqrt(1.0 - zeta * zeta);

                            // envelope decay: exp(-zeta*wn*t)
                            double envelope = Math.Exp(-zeta * wn * dT);

                            double A = mode.ModalAmplitude * _participation![ch, m];

                            ringdown += A * envelope * Math.Sin(wd * dT + mode.PhaseRad);
                        }
                    }

                    // Sensor bias/drift/noise
                    double drift = chModel.DriftPerSecond * t;
                    double noise = NextGaussian(_rand) * chModel.NoiseStd;

                    double x = impact + ringdown + chModel.DcOffset + drift + noise;

                    // Optional bandwidth limit (one-pole low-pass) -- STATEFUL across chunks
                    if (chModel.LowpassHz > 0)
                    {
                        _lpState![ch] = _lpState[ch] + _lpAlpha![ch] * (x - _lpState[ch]);
                        x = _lpState[ch];
                    }

                    samples[ch, i] = x;
                }

                // Cross-talk mixing (small)
                if (crossTalk > 0)
                {
                    double mean = 0.0;
                    for (int ch = 0; ch < channelCount; ch++)
                        mean += samples[ch, i];
                    mean /= channelCount;

                    for (int ch = 0; ch < channelCount; ch++)
                        samples[ch, i] = (1.0 - crossTalk) * samples[ch, i] + crossTalk * mean;
                }
            }

            _time += count * dt;
            return samples;
        }

        /// <summary>
        /// Resets the generator timeline and internal filter state.
        /// Call this when starting a new run.
        /// </summary>
        public void Reset(double startTimeSeconds = 0.0)
        {
            _time = startTimeSeconds;

            if (_lpState != null)
                Array.Clear(_lpState, 0, _lpState.Length);

            // Participation intentionally NOT cleared (structure doesn't change).
            // If you want new random participation between runs, set _participation = null here.
        }

        private void EnsureLowpassState(
            int channelCount,
            double sampleRate,
            IReadOnlyList<ChannelModel> channels,
            double dt)
        {
            if (_lpState == null || _lpAlpha == null ||
                _lpChannelCount != channelCount || _lpSampleRate != sampleRate)
            {
                _lpState = new double[channelCount];
                _lpAlpha = new double[channelCount];
                _lpChannelCount = channelCount;
                _lpSampleRate = sampleRate;
            }

            for (int ch = 0; ch < channelCount; ch++)
            {
                double fc = channels[ch].LowpassHz;
                _lpAlpha![ch] = (fc > 0) ? OnePoleLowpassAlpha(fc, dt) : 0.0;
            }
        }

        private void EnsureParticipation(int channelCount, int modeCount)
        {
            if (_participation != null &&
                _partChannelCount == channelCount &&
                _partModeCount == modeCount)
            {
                return;
            }

            _participation = BuildParticipation(channelCount, modeCount);
            _partChannelCount = channelCount;
            _partModeCount = modeCount;
        }

        private IReadOnlyList<ChannelModel> BuildDefaultChannels(int channelCount)
        {
            var list = new ChannelModel[channelCount];

            for (int ch = 0; ch < channelCount; ch++)
            {
                double gain = 1.0 - 0.08 * ch;
                double delay = ch * 120e-6;

                double noise = 0.015 + 0.005 * ch;
                double offset = (ch - (channelCount - 1) / 2.0) * 0.01;
                double drift = (ch % 2 == 0 ? 1 : -1) * 0.001;

                double lowpass = 5000; // Hz; set to 0 to disable

                list[ch] = new ChannelModel(
                    Gain: gain,
                    DelaySeconds: delay,
                    NoiseStd: noise,
                    DcOffset: offset,
                    DriftPerSecond: drift,
                    LowpassHz: lowpass
                );
            }

            return list;
        }

        private double[,] BuildParticipation(int channelCount, int modeCount)
        {
            // Stable participation (uses this._rand)
            var p = new double[channelCount, modeCount];

            for (int ch = 0; ch < channelCount; ch++)
            {
                for (int m = 0; m < modeCount; m++)
                {
                    double mag = 0.3 + 0.9 * _rand.NextDouble();
                    double sign = ((ch + 2 * m) % 3 == 0) ? -1.0 : 1.0;
                    p[ch, m] = sign * mag;
                }
            }

            // Normalize per channel
            for (int ch = 0; ch < channelCount; ch++)
            {
                double rms = 0.0;
                for (int m = 0; m < modeCount; m++)
                    rms += p[ch, m] * p[ch, m];

                rms = Math.Sqrt(rms / Math.Max(1, modeCount));

                double target = 0.9;
                double scale = target / Math.Max(rms, 1e-9);

                for (int m = 0; m < modeCount; m++)
                    p[ch, m] *= scale;
            }

            return p;
        }

        private static double GaussianPulse(double t, double t0, double widthSeconds)
        {
            double sigma = Math.Max(widthSeconds, 1e-12);
            double x = (t - t0) / sigma;
            return Math.Exp(-(x * x));
        }

        private static double OnePoleLowpassAlpha(double cutoffHz, double dt)
        {
            double rc = 1.0 / (2.0 * Math.PI * cutoffHz);
            return dt / (rc + dt);
        }

        private static double NextGaussian(Random rand)
        {
            // Box-Muller
            double u1 = Math.Max(rand.NextDouble(), 1e-12);
            double u2 = rand.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}