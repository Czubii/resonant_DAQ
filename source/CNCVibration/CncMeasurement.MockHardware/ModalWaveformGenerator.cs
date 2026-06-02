using CncMeasurement.Core.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CncMeasurement.MockHardware
{
    public sealed class ModalWaveformGenerator
    {
        private double _time;

        // Simple mode definition (you can extend with more realism later)
        public sealed record Mode(
            double FnHz,              // natural frequency
            double DampingRatio,      // zeta (e.g. 0.01 = 1%)
            double ModalAmplitude,    // base amplitude scale
            double PhaseRad = 0.0     // phase at impact
        );

        public sealed record ChannelModel(
            double Gain = 1.0,
            double DelaySeconds = 0.0,      // time-of-flight / arrival delay
            double NoiseStd = 0.02,         // white noise std dev
            double DcOffset = 0.0,          // sensor bias
            double DriftPerSecond = 0.0,    // slow drift
            double LowpassHz = 0.0          // 0 = disabled
        );

        /// <summary>
        /// Generate multi-channel impact test data.
        /// Realism knobs:
        /// - multiple modes with damping ratio ζ
        /// - per-channel modal participation factors
        /// - arrival delays
        /// - DC offsets + drift + noise
        /// - optional per-channel low-pass bandwidth
        /// </summary>
        public double[,] GenerateWaveform(
            AcquisitionConfig config,
            double impactTimeSeconds = 2.0,
            double impactWidthSeconds = 0.0008,   // contact duration scale (smaller => sharper)
            double impactAmplitude = 20.0,
            IReadOnlyList<Mode>? modes = null,
            IReadOnlyList<ChannelModel>? channels = null,
            double crossTalk = 0.03               // 0..0.2 typical; small sensor coupling
        )
        {
            int channelCount = config.ChannelConfigs.Count;
            int count = config.ChunkSize;

            double[,] samples = new double[channelCount, count];
            double dt = 1.0 / config.SampleRate;

            // Default modes: adjust to your structure (example: a few resonances)
            modes ??= new[]
            {
            new Mode(FnHz: 180, DampingRatio: 0.015, ModalAmplitude: 10, PhaseRad: 0.2),
            new Mode(FnHz: 520, DampingRatio: 0.010, ModalAmplitude: 18, PhaseRad: 0.0),
            new Mode(FnHz: 950, DampingRatio: 0.020, ModalAmplitude: 7,  PhaseRad: -0.3),
            new Mode(FnHz: 1450,DampingRatio: 0.030, ModalAmplitude: 3,  PhaseRad: 0.7),
        };

            // Default channel models
            channels ??= BuildDefaultChannels(channelCount);

            // Per-channel modal participation factors (how strongly each mode shows up on each sensor)
            // This is a big part of "different channels look different".
            double[,] participation = BuildParticipation(channelCount, modes.Count);

            // Optional: per-channel one-pole lowpass state
            var lpState = new double[channelCount];
            var lpAlpha = new double[channelCount];
            for (int ch = 0; ch < channelCount; ch++)
            {
                double fc = channels[ch].LowpassHz;
                lpAlpha[ch] = (fc > 0) ? OnePoleLowpassAlpha(fc, dt) : 0.0;
            }

            var rand = Random.Shared;

            // Generate
            for (int i = 0; i < count; i++)
            {
                double t = _time + i * dt;

                // Impact force-ish input (Gaussian-like contact pulse).
                // Real impact pulses are not perfect Gaussians; this is a reasonable approximation.
                double impact = impactAmplitude * GaussianPulse(t, impactTimeSeconds, impactWidthSeconds);

                // For each channel: apply delay, modal response, sensor effects
                for (int ch = 0; ch < channelCount; ch++)
                {
                    var chModel = channels[ch];
                    double tc = t - chModel.DelaySeconds;

                    // Channel-specific impact arrival
                    double impactCh = impactAmplitude * chModel.Gain * GaussianPulse(tc, impactTimeSeconds, impactWidthSeconds);

                    // Modal ring-down starts at impact time (per channel arrival)
                    double ringdown = 0.0;
                    if (tc >= impactTimeSeconds)
                    {
                        double dT = tc - impactTimeSeconds;

                        for (int m = 0; m < modes.Count; m++)
                        {
                            var mode = modes[m];

                            // Guard against invalid damping ratios
                            double zeta = Math.Clamp(mode.DampingRatio, 0.0001, 0.99);

                            double wn = 2.0 * Math.PI * mode.FnHz;
                            double wd = wn * Math.Sqrt(1.0 - zeta * zeta);
                            double envelope = Math.Exp(-zeta * wn * dT);

                            // Channel sees this mode scaled by participation factor
                            double A = mode.ModalAmplitude * participation[ch, m];

                            ringdown += A * envelope * Math.Sin(wd * dT + mode.PhaseRad);
                        }
                    }

                    // Bias/drift/noise
                    double drift = chModel.DriftPerSecond * t;
                    double noise = NextGaussian(rand) * chModel.NoiseStd;

                    double x = impactCh + ringdown + chModel.DcOffset + drift + noise;

                    // Optional sensor bandwidth (low-pass)
                    if (chModel.LowpassHz > 0)
                    {
                        // y[n] = y[n-1] + alpha*(x - y[n-1])
                        lpState[ch] = lpState[ch] + lpAlpha[ch] * (x - lpState[ch]);
                        x = lpState[ch];
                    }

                    samples[ch, i] = x;
                }

                // Small cross-talk mixing: each channel leaks a bit of the mean of the others
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

        private static IReadOnlyList<ChannelModel> BuildDefaultChannels(int channelCount)
        {
            var list = new ChannelModel[channelCount];

            // Example: sensors at different distances / mount stiffness
            for (int ch = 0; ch < channelCount; ch++)
            {
                double gain = 1.0 - 0.08 * ch;

                // A few hundred microseconds delay differences can matter at kHz modes
                double delay = ch * 120e-6;

                double noise = 0.015 + 0.005 * ch;
                double offset = (ch - (channelCount - 1) / 2.0) * 0.01; // small bias spread
                double drift = (ch % 2 == 0 ? 1 : -1) * 0.001;          // tiny drift

                // If you're simulating accelerometers, a bandwidth limit is realistic
                // (set to 0 to disable)
                double lowpass = 5000; // Hz

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

        private static double[,] BuildParticipation(int channelCount, int modeCount)
        {
            var rand = Random.Shared;
            var p = new double[channelCount, modeCount];

            // Participation: positive/negative signs + different magnitudes per sensor.
            // This approximates mode shapes (some sensors near nodes see less, some see more).
            for (int ch = 0; ch < channelCount; ch++)
            {
                for (int m = 0; m < modeCount; m++)
                {
                    // Base 0.3..1.2
                    double mag = 0.3 + 0.9 * rand.NextDouble();

                    // Alternate sign patterns across channels/modes
                    double sign = ((ch + 2 * m) % 3 == 0) ? -1.0 : 1.0;

                    p[ch, m] = sign * mag;
                }
            }

            // Normalize per channel so amplitudes don't explode when modeCount grows
            for (int ch = 0; ch < channelCount; ch++)
            {
                double rms = 0.0;
                for (int m = 0; m < modeCount; m++) rms += p[ch, m] * p[ch, m];
                rms = Math.Sqrt(rms / modeCount);

                double target = 0.9; // desired RMS participation
                double scale = target / Math.Max(rms, 1e-9);

                for (int m = 0; m < modeCount; m++) p[ch, m] *= scale;
            }

            return p;
        }

        private static double GaussianPulse(double t, double t0, double widthSeconds)
        {
            // widthSeconds is like sigma
            double x = (t - t0) / Math.Max(widthSeconds, 1e-9);
            return Math.Exp(-(x * x));
        }

        private static double OnePoleLowpassAlpha(double cutoffHz, double dt)
        {
            // RC lowpass: alpha = dt / (RC + dt), RC = 1/(2πfc)
            double rc = 1.0 / (2.0 * Math.PI * cutoffHz);
            return dt / (rc + dt);
        }

        // Box-Muller: standard normal
        private static double NextGaussian(Random rand)
        {
            double u1 = Math.Max(rand.NextDouble(), 1e-12);
            double u2 = rand.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
