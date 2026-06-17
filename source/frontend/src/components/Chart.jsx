import { useEffect, useRef } from "react";
import Plotly from "plotly.js-dist-min";

export default function Chart({ type, report }) {
  const ref = useRef(null);

  useEffect(() => {
    if (!ref.current || !report) return;

    const { signalFFT, signalRaw } = report;
    let traces = [];
    let layout = {
      paper_bgcolor: "#1b1b1b",
      plot_bgcolor: "#1b1b1b",
      font: { color: "#fff" },
      margin: { t: 40, l: 40, r: 20, b: 40 },
      xaxis: { title: "Frequency (Hz)" },
      yaxis: { title: "Value" },
    };

    if (type === "fft" && signalFFT) {
      // FFT: Używamy pełnych danych z signalFFT
      traces = signalFFT.channels.map(ch => ({
        x: signalFFT.frequenciesHz,
        y: ch.magnitudes,
        type: "scatter",
        mode: "lines",
        name: ch.assignedChannelName
      }));
      layout.title = "FFT Spectrum";
    } 
    else if (type === "psd" && signalFFT) {
      // PSD: Używamy pełnych danych z signalFFT
      traces = signalFFT.channels.map(ch => ({
        x: signalFFT.frequenciesHz,
        y: ch.psdMagnitudes,
        type: "scatter",
        mode: "lines",
        name: ch.assignedChannelName
      }));
      layout.title = "PSD Analysis";
      layout.yaxis.type = "log"; // PSD najlepiej wygląda w log
    }
    else if (type === "time" && signalRaw) {
      // Time: Używamy surowych próbek
      const dt = 1 / signalRaw.sampleRateHz;
      const timeAxis = signalRaw.channels[0].samples.map((_, i) => i * dt);
      
      traces = signalRaw.channels.map(ch => ({
        x: timeAxis,
        y: ch.samples,
        type: "scatter",
        mode: "lines",
        name: ch.assignedChannelName
      }));
      layout.title = "Time Domain Signal";
      layout.xaxis.title = "Time (s)";
    }

    Plotly.react(ref.current, traces, layout, { responsive: true });
  }, [type, report]);

  return <div ref={ref} style={{ width: "100%", height: "350px" }} />;
}