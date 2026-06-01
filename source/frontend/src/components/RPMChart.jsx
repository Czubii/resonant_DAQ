import { useEffect, useRef } from "react";
import Plotly from "plotly.js-dist-min";

export default function RPMChart() {
  const ref = useRef(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;

    Plotly.newPlot(
      el,
      [
        {
          x: [1000, 2000, 3000, 4000, 5000],
          y: [0.1, 0.4, 0.9, 0.5, 0.3],
          type: "scatter",
          mode: "lines+markers",
          line: { color: "#ffcc00" },
        },
      ],
      {
        title: "RPM Amplitude",

  
        paper_bgcolor: "#1b1b1b",
        plot_bgcolor: "#1b1b1b",

        font: { color: "#fff" },

        margin: { t: 40, l: 40, r: 20, b: 40 },

        xaxis: { title: "RPM" },
        yaxis: { title: "Amplitude" },

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