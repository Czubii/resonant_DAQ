import { useState } from "react";

import FFTChart from "./components/FFTChart";
import ControlPanel from "./components/ControlPanel";

function App() {
  const [measurement, setMeasurement] = useState(null);

  return (
    <div style={styles.app}>
      <div style={styles.header}>
        <h1 style={styles.title}>
          CNC Vibration Dashboard
        </h1>

        <div style={styles.status}>
          SYSTEM ONLINE
        </div>
      </div>

      <div style={styles.chartSection}>
        <FFTChart />
      </div>

      <div style={styles.bottomSection}>
        <div style={styles.controlsCard}>
          <ControlPanel
            onMeasurement={setMeasurement}
          />
        </div>

        <div style={styles.resultCard}>
          <h2>Measurement Result</h2>

          {measurement ? (
            <>
              <p>
                Voltage:
                {" "}
                {measurement.voltage}
                {" "}
                V
              </p>

              <p>
                Samples:
                {" "}
                {measurement.samples}
              </p>

              <p>
                Window:
                {" "}
                {measurement.windowType}
              </p>

              <p>
                Sampling:
                {" "}
                {measurement.samplingRate}
              </p>
            </>
          ) : (
            <p>No measurement yet.</p>
          )}
        </div>
      </div>
    </div>
  );
}

const styles = {
  app: {
    backgroundColor: "#111",
    minHeight: "100vh",
    color: "white",
    padding: "10px",
    fontFamily: "Arial",
  },

  header: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: "10px",
  },

  title: {
    margin: 0,
    fontSize: "28px",
  },

  status: {
    backgroundColor: "#1e4620",
    color: "#7dff88",
    padding: "8px 14px",
    borderRadius: "8px",
    fontSize: "14px",
  },

  chartSection: {
    backgroundColor: "#1b1b1b",
    borderRadius: "12px",
    padding: "10px",
    marginBottom: "10px",
  },

  bottomSection: {
    display: "grid",
    gridTemplateColumns: "2fr 1fr",
    gap: "10px",
  },

  controlsCard: {
    backgroundColor: "#1b1b1b",
    borderRadius: "12px",
    padding: "12px",
  },

  resultCard: {
    backgroundColor: "#1b1b1b",
    borderRadius: "12px",
    padding: "12px",
  },
};

export default App;