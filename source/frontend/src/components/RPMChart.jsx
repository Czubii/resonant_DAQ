import { useEffect, useRef } from "react";
import Plotly from "plotly.js-dist-min";

export default function RPMChart(props) {
  const ref = useRef(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;

    const signal = props?.data;
    if (!signal) return;

    const channels = signal.channels;
    if (!Array.isArray(channels) || channels.length === 0) return;

    const sampleRate = signal.sampleRateHz;
    if (!sampleRate || sampleRate <= 0) return;

    const dt = 1 / sampleRate;

    const traces = channels
      .filter((ch) => ch && Array.isArray(ch.samples))
      .map((ch) => {
        const samples = ch.samples;

        return {
          name: ch.assignedChannelName ?? "Unnamed channel",
          x: samples.map((_, i) => i * dt),
          y: samples,
          type: "scatter",
          mode: "lines",
        };
      })
      .filter((t) => Array.isArray(t.y) && t.y.length > 0);

    if (traces.length === 0) return;

    Plotly.react(
      el,
      traces,
      {
        title: "Time Waveform",

        paper_bgcolor: "#1b1b1b",
        plot_bgcolor: "#1b1b1b",

        font: { color: "#fff" },

        margin: { t: 40, l: 40, r: 20, b: 40 },

        xaxis: { title: "Time (s)" },
        yaxis: { title: "Amplitude" },

        showlegend: true,
      },
      { responsive: true }
    );

    return () => {
      if (ref.current) Plotly.purge(ref.current);
    };
  }, [props.data]);

  return <div ref={ref} style={{ width: "100%", height: "350px" }} />;
}