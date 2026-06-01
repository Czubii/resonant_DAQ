import { useState } from "react";

import Sidebar from "./components/Sidebar";
import FFTChart from "./components/FFTChart";
import RPMChart from "./components/RPMChart";

export default function App() {
  const [menu, setMenu] = useState("idle");
  const [tab, setTab] = useState("fft");

  const [config, setConfig] = useState({
    experimentName: "",
    sampleRate: 10000,
    chunkSize: 4096,
    channels: [
      {
        physicalChannelName: "",
        channelName: "",
      },
    ],
  });

  const backendChannels = [
    "cDAQMod1/ai0",
    "cDAQMod1/ai1",
    "cDAQMod2/ai0",
  ];

  function startMeasurement() {
    console.log("START MEASUREMENT", config);
    alert("Measurement started (mock)");
  }

  function saveExperiment() {
    console.log("SAVE EXPERIMENT", config);
    alert("Experiment saved (mock)");
  }

  return (
    <div style={styles.app}>
      <Sidebar onSelect={setMenu} />

      <div style={styles.main}>

        {/* ===== CHART ===== */}
        <div style={styles.chartCard}>

          <div style={styles.tabs}>
            <button
              onClick={() => setTab("fft")}
              style={tab === "fft" ? styles.tabActive : styles.tab}
            >
              FFT Spectrum
            </button>

            <button
              onClick={() => setTab("rpm")}
              style={tab === "rpm" ? styles.tabActive : styles.tab}
            >
              RPM Amplitude
            </button>
          </div>

          <h3 style={styles.title}>
            {tab === "fft"
              ? "FFT Spectrum Analysis"
              : "RPM Amplitude Analysis"}
          </h3>

          <div style={styles.chartBox}>
            {tab === "fft" ? <FFTChart /> : <RPMChart />}
          </div>
        </div>

        {/* ===== CONFIG ===== */}
        <div style={styles.grid}>

          {/* EXPERIMENT */}
          {menu === "new-config" && (
            <div style={styles.card}>
              <h3>Experiment Config</h3>

              <label>Experiment Name</label>
              <input style={styles.input} />

               <label>Sample Rate (10 000 - 50 000 Hz)</label>
    <input
      type="number"
      min={10000}
      max={50000}
      step={1000}
      style={styles.input}
      placeholder="e.g. 10240"
    />

              <label>Chunk Size</label>
              <select style={styles.input}>
                {[256, 512, 1024, 2048, 4096].map((v) => (
                  <option key={v}>{v}</option>
                ))}
              </select>

              {/* BUTTONS RESTORED */}
              <div style={styles.buttonRow}>
                <button style={styles.startBtn} onClick={startMeasurement}>
                  Start Measurement
                </button>

                <button style={styles.saveBtn} onClick={saveExperiment}>
                  Save Experiment
                </button>
              </div>
            </div>
          )}

          {/* CHANNELS */}
          {menu === "new-config" && (
            <div style={styles.card}>
              <h3>Channel Configurations</h3>

              {config.channels.map((ch, i) => (
                <div key={i} style={styles.channelCard}>

                  <label>Physical Channel Name</label>
                  <select
                    style={styles.input}
                    value={ch.physicalChannelName}
                    onChange={(e) => {
                      const updated = [...config.channels];
                      updated[i].physicalChannelName = e.target.value;
                      setConfig({ ...config, channels: updated });
                    }}
                  >
                    <option value="">Select channel</option>
                    {backendChannels.map((c) => (
                      <option key={c} value={c}>
                        {c}
                      </option>
                    ))}
                  </select>

                  <label>Channel Name</label>
                  <input
                    style={styles.input}
                    value={ch.channelName}
                    onChange={(e) => {
                      const updated = [...config.channels];
                      updated[i].channelName = e.target.value;
                      setConfig({ ...config, channels: updated });
                    }}
                  />
                </div>
              ))}
            </div>
          )}

          {/* LOAD */}
          {menu === "load" && (
            <div style={styles.card}>
              <h3>Load Experiment</h3>
              <p style={{ color: "#ffcc00" }}>
                Work in progress...
              </p>
            </div>
          )}

          {/* IDLE */}
          {menu === "idle" && (
            <div style={styles.card}>
              Select New or Load Experiment
            </div>
          )}

        </div>
      </div>
    </div>
  );
}

const styles = {
  app: {
    display: "flex",
    backgroundColor: "#111",
    minHeight: "100vh",
    color: "white",
    fontFamily: "Arial",
  },

  main: {
    marginLeft: "220px",
    width: "100%",
    padding: "10px",
  },

  chartCard: {
    backgroundColor: "#1b1b1b",
    padding: "10px",
    borderRadius: "10px",
  },

  tabs: {
    display: "flex",
    gap: "10px",
  },

  tab: {
    padding: "8px 14px",
    backgroundColor: "#222",
    color: "#aaa",
    border: "1px solid #333",
    borderRadius: "6px",
    cursor: "pointer",
  },

  tabActive: {
    padding: "8px 14px",
    backgroundColor: "#00d4ff",
    color: "#000",
    border: "1px solid #00d4ff",
    borderRadius: "6px",
    fontWeight: "bold",
    cursor: "pointer",
  },

  title: {
    margin: "10px 0",
  },

  chartBox: {
    backgroundColor: "#111",
    borderRadius: "10px",
    padding: "10px",
  },

  grid: {
    display: "grid",
    gridTemplateColumns: "1fr 1fr",
    gap: "10px",
    marginTop: "10px",
  },

  card: {
    backgroundColor: "#1b1b1b",
    padding: "10px",
    borderRadius: "10px",
  },

  channelCard: {
    border: "1px solid #333",
    padding: "8px",
    marginTop: "8px",
  },

  input: {
    width: "100%",
    padding: "6px",
    marginTop: "6px",
    marginBottom: "6px",
    backgroundColor: "#222",
    color: "white",
    border: "1px solid #444",
  },

  buttonRow: {
    display: "flex",
    gap: "10px",
    marginTop: "10px",
  },

  startBtn: {
    flex: 1,
    padding: "10px",
    backgroundColor: "#00d4ff",
    border: "none",
    color: "#000",
    fontWeight: "bold",
    borderRadius: "6px",
    cursor: "pointer",
  },

  saveBtn: {
    flex: 1,
    padding: "10px",
    backgroundColor: "#2a2a2a",
    border: "1px solid #444",
    color: "white",
    borderRadius: "6px",
    cursor: "pointer",
  },
};