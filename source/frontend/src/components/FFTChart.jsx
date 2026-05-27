import { useEffect, useRef } from "react";
import Plotly from "plotly.js-dist-min";

export default function FFTChart() {
  const ref = useRef(null);

  useEffect(() => {
    const el = ref.current;

    if (!el) return;

    const data = [
      {
        x: [0, 1, 2, 3, 4],
        y: [0, 1, 4, 9, 16],
        type: "scatter",
        mode: "lines+markers",
      },
    ];

    const layout = {
      title: "FFT Chart",
      paper_bgcolor: "#111",
      plot_bgcolor: "#111",
      font: { color: "#fff" },
      margin: { t: 40, l: 40, r: 20, b: 40 },
    };

    Plotly.newPlot(el, data, layout, { responsive: true });

    return () => {
      if (ref.current) {
        Plotly.purge(ref.current);
      }
    };
  }, []);

  return <div ref={ref} style={{ width: "100%", height: "350px" }} />;
}