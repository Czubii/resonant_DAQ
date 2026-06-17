import { useEffect, useState, useMemo } from "react";
import Sidebar from "./components/Sidebar";
import Chart from "./components/Chart";
import ExperimentControlPanel from "./components/ExperimentControlPanel";
import ChannelControlPanel from "./components/ChannelControlPanel";

const defaultConfig = {
  Name: "New Experiment",
  Description: "",

  MachineConfig: {
    Y: 25,
  },

  MeasurementConfig: {
    SampleRate: 7500,
    ChunkSize: 4096,
    GroupName: "test",
    OutputTDMSPath: "testoutput.tdms",
  },

  TriggerConfig: {
    SampleRate: 7500,
    PreTriggerWindowMs: 250,
    PostTriggerWindowMs: 250,
    Threshold: 1.0,
  },

  AnalysisConfig: {
    ModeProminenceThresholddB: 2,
    DampingFilterBandwidthPercent: 0.1,
    DampingStartPeakPercent: 0.95,
    DampingEndPeakPercent: 0.15,
    UseNDominantModes: 8,
  },

  // UI-only (not sent to backend config root)
  channels: [],
};

export default function App() {
  const [menu, setMenu] = useState("idle");
  const [tab, setTab] = useState("fft");
  const [devices, setDevices] = useState([]);
  const [experiments, setExperiments] = useState([]);
  const [experimentId, setExperimentId] = useState(null);
  const [runStatus, setRunStatus] = useState("");
  const [selectedExperiment, setSelectedExperiment] = useState(null);
  const [saveStatus, setSaveStatus] = useState("");
  const [config, setConfig] = useState(defaultConfig);

  useEffect(() => {
    fetch("http://localhost:5078/GetDevices")
      .then((r) => r.json())
      .then(setDevices)
      .catch(console.error);
  }, []);

  const backendChannels = useMemo(() => {
  return devices?.flatMap(d => d.aiChannels ?? []) ?? [];
}, [devices]);

    useEffect(() => {
  if (!backendChannels.length) return;

  setConfig(prev => ({
    ...prev,
    channels: backendChannels.map(name => {
      const existing = prev.channels.find(
        c => c.physicalChannelName === name
      );

      return (
        existing || {
          physicalChannelName: name,
          enabled: false,
          channelName: "",
          minRange: -50,
          maxRange: 50,
          sensitivity: 100,
        }
      );
    }),
  }));
}, [backendChannels]);

 
  function openNewExperiment() {
  setConfig({
    ...defaultConfig,
    channels: backendChannels.map(name => ({
      physicalChannelName: name,
      enabled: false,
      channelName: "",
      minRange: -50,
      maxRange: 50,
      sensitivity: 100,
    })),
  });

  setSelectedExperiment(null);
  setExperimentId(null);
  setRunStatus("");
  setSaveStatus("");
  setMenu("new-config");
}

  async function loadExperiments() {
    try {
      const res = await fetch("http://localhost:5078/GetAllExperiments/");
      const data = await res.json();
      setExperiments(data);
    } catch (err) {
      console.error(err);
    }
  }


  async function startExperiment() {
  try {
    setRunStatus("Experiment is starting...");

    const channelConfigs = config.channels
      .filter((ch) => ch.enabled)
      .map((ch) => ({
        PhysicalChannelName: ch.physicalChannelName,
        NameToAssignToChannel: ch.channelName,
        MinRange: Number(ch.minRange),
        MaxRange: Number(ch.maxRange),
        Sensitivity: Number(ch.sensitivity),
      }));

    const payload = {
      Name: config.Name,
      Description: config.Description,

      MachineConfig: {
        Y: config.MachineConfig.Y,
      },

      MeasurementConfig: {
        SampleRate: config.MeasurementConfig.SampleRate,
        ChunkSize: config.MeasurementConfig.ChunkSize,
        GroupName: config.MeasurementConfig.GroupName,
        OutputTDMSPath: config.MeasurementConfig.OutputTDMSPath,
        ChannelConfigs: channelConfigs,
      },

      TriggerConfig: {
        SampleRate: config.TriggerConfig.SampleRate,
        ChannelConfigs: channelConfigs,
        PreTriggerWindowMs: config.TriggerConfig.PreTriggerWindowMs,
        PostTriggerWindowMs: config.TriggerConfig.PostTriggerWindowMs,
        Threshold: config.TriggerConfig.Threshold,
      },

      AnalysisConfig: {
        ModeProminenceThresholddB:
          config.AnalysisConfig.ModeProminenceThresholddB,

        DampingFilterBandwidthPercent:
          config.AnalysisConfig.DampingFilterBandwidthPercent,

        DampingStartPeakPercent:
          config.AnalysisConfig.DampingStartPeakPercent,

        DampingEndPeakPercent:
          config.AnalysisConfig.DampingEndPeakPercent,

        UseNDominantModes:
          config.AnalysisConfig.UseNDominantModes,
      },
    };

    const res = await fetch(
      "http://localhost:5078/UploadModalExperiment",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify(payload),
      }
    );

    const data = await res.json();

    if (!res.ok) {
      throw new Error(
        `HTTP error ${res.status}: ${JSON.stringify(data)}`
      );
    }

    const id = data.id;

    setExperimentId(id);
    setRunStatus("Experiment is running");

    if (id) pollStatus(id);
  } catch (err) {
    console.error(err);
    setRunStatus("Failed to start experiment");
  }
}

  function pollStatus(id) {
    let attempts = 0;

    const interval = setInterval(async () => {
      attempts++;

      try {
        const res = await fetch(
          `http://localhost:5078/CheckExperimentStatus/`
        );

        const text = await res.text();
        setRunStatus(text);

        const t = text.toLowerCase();

        if (
          t.includes("completed") ||
          t.includes("done") ||
          t.includes("finish")
        ) {
          clearInterval(interval);

          setRunStatus("Experiment completed");
          loadExperiment(id);
        }
      } catch (err) {
        clearInterval(interval);
        setRunStatus("Error checking status");
      }

      if (attempts > 60) {
        clearInterval(interval);
        setRunStatus("Timeout");
      }
    }, 2000);
  }

async function loadExperiment(id) {
  try {
    const res = await fetch(
      `http://localhost:5078/GetExperiment/${id}`
    );
    const data = await res.json();

    setSelectedExperiment(data);



  } catch (err) {
    console.error(err);
  }
}

  return (
    <div style={styles.app}>
      <Sidebar onSelect={setMenu} />

      <div style={styles.main}>
        {/* CHART */}
        <div style={styles.chartCard}>
          <div style={styles.tabs}>
            <button
              onClick={() => setTab("fft")}
              style={tab === "fft" ? styles.buttonActive : styles.button}
            >
              FFT
            </button>
            <button
              onClick={() => setTab("time")}
              style={tab === "time" ? styles.buttonActive : styles.button}
            >
              Time
            </button>
            <button
              onClick={() => setTab("psd")}
              style={tab === "psd" ? styles.buttonActive : styles.button}
            >
              PSD
            </button>
          </div>

          <div style={styles.chartBox}>
            {selectedExperiment?.report ? (
              <Chart type={tab} report={selectedExperiment.report} />
            ) : (
              <p style={{ textAlign: "center", paddingTop: "150px" }}>
                Choose an experiment
              </p>
            )}
          </div>
        </div>

        {/* NEW CONFIG */}
        {menu === "new-config" && (
          <>
            {saveStatus && (
              <div style={styles.status}>{saveStatus}</div>
            )}

            {runStatus && (
              <div style={styles.status}>{runStatus}</div>
            )}

            <div style={{ display: "flex", gap: 20 }}>
              <div style={{ flex: 1 }}>
                <ExperimentControlPanel
                  config={config}
                  setConfig={setConfig}
                  onStart={startExperiment}
                />
              </div>

              <div style={{ flex: 1 }}>
                <ChannelControlPanel
                  channels={config.channels}
                  setChannels={(ch) =>
                    setConfig({ ...config, channels: ch })
                  }
                
                />
              </div>
            </div>
          </>
        )}

        {/* LOAD */}
        {menu === "load" && (
          <div style={styles.card}>
            <h3>Saved Experiments</h3>

            <button style={styles.actionBtn} onClick={loadExperiments}>
              Refresh List
            </button>

            <div style={{ marginTop: 15 }}>
              {experiments.map((exp) => (
                <div key={exp.id} style={styles.expCard}>
                  <div>
                    <b>{exp.name}</b>
                    <div style={styles.small}>ID: {exp.id}</div>
                  </div>

                  <button
                    style={styles.actionBtn}
                    onClick={() => loadExperiment(exp.id)}
                  >
                    Load
                  </button>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

const styles = {
  app: {
    display: "flex",
    background: "#111",
    color: "white",
    minHeight: "100vh",
  },
  main: { marginLeft: 220, width: "100%", padding: 10 },
  chartCard: {
    background: "#1b1b1b",
    padding: 10,
    borderRadius: 10,
    marginBottom: 10,
  },
  chartBox: { background: "#000", minHeight: 350 },
  tabs: { display: "flex", gap: 10, marginBottom: 10 },

  button: {
    padding: "8px 16px",
    background: "#222",
    color: "#aaa",
    border: "1px solid #333",
    borderRadius: 6,
  },
  buttonActive: {
    padding: "8px 16px",
    background: "#00d4ff",
    color: "#000",
    border: "1px solid #00d4ff",
    borderRadius: 6,
    fontWeight: "bold",
  },

  card: {
    background: "#1b1b1b",
    padding: 10,
    borderRadius: 10,
  },

  expCard: {
    padding: 10,
    background: "#222",
    marginBottom: 5,
    display: "flex",
    justifyContent: "space-between",
  },

  actionBtn: {
    padding: "8px 16px",
    background: "#00d4ff",
    color: "#000",
    border: "none",
    borderRadius: 6,
    fontWeight: "bold",
  },

  status: {
    margin: "10px 0",
    color: "#00d4ff",
    fontWeight: "bold",
  },

  small: {
    fontSize: "0.85em",
    color: "#888",
  },
};