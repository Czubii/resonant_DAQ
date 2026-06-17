import { useEffect, useRef } from "react";
import Plotly from "plotly.js-dist-min";

export default function FFTChart() {
  const ref = useRef(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;

    Plotly.newPlot(
      el,
      [
        {
          x: [0, 10, 20, 30, 40, 50],
          y: [0, 0.2, 0.6, 0.3, 0.8, 0.4],
          type: "scatter",
          mode: "lines",
          line: { color: "#00d4ff" },
        },
      ],
      {
        title: "FFT Spectrum",

        paper_bgcolor: "#1b1b1b",
        plot_bgcolor: "#1b1b1b",

        font: { color: "#fff" },


        margin: { t: 40, l: 40, r: 20, b: 40 },

        showlegend: false,
      },
      { responsive: true }
    );

    return () => {
      if (ref.current) Plotly.purge(ref.current);
    };
  }, []);

  return <div ref={ref} style={{ width: "100%", height: "350px" }} />;
}