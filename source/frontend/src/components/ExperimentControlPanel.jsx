export default function ExperimentControlPanel({
  config,
  setConfig,
  onStart,
}) {
  return (
    <div style={styles.card}>
      <h3>Experiment Config</h3>

      <label>Name</label>
      <input
        style={styles.input}
        value={config.Name || ""}
        onChange={(e) =>
          setConfig({
            ...config,
            Name: e.target.value,
          })
        }
      />

      <label>Description</label>
      <textarea
        style={styles.textarea}
        value={config.Description || ""}
        onChange={(e) =>
          setConfig({
            ...config,
            Description: e.target.value,
          })
        }
      />

      <h4>Machine</h4>

      <label>Y Position</label>
      <input
        type="number"
        style={styles.input}
        value={config.MachineConfig?.Y ?? 0}
        onChange={(e) =>
          setConfig({
            ...config,
            MachineConfig: {
              ...config.MachineConfig,
              Y: Number(e.target.value),
            },
          })
        }
      />

      <h4>Measurement</h4>

      <label>Sample Rate</label>
      <input
        type="number"
        style={styles.input}
        value={config.MeasurementConfig?.SampleRate ?? 0}
        onChange={(e) =>
          setConfig({
            ...config,
            MeasurementConfig: {
              ...config.MeasurementConfig,
              SampleRate: Number(e.target.value),
            },
          })
        }
      />

      <label>Chunk Size</label>
      <select
        style={styles.input}
        value={config.MeasurementConfig?.ChunkSize ?? 4096}
        onChange={(e) =>
          setConfig({
            ...config,
            MeasurementConfig: {
              ...config.MeasurementConfig,
              ChunkSize: Number(e.target.value),
            },
          })
        }
      >
        {[256, 512, 1024, 2048, 4096, 8192].map((v) => (
          <option key={v} value={v}>
            {v}
          </option>
        ))}
      </select>

      <label>Group Name</label>
      <input
        style={styles.input}
        value={config.MeasurementConfig?.GroupName || ""}
        onChange={(e) =>
          setConfig({
            ...config,
            MeasurementConfig: {
              ...config.MeasurementConfig,
              GroupName: e.target.value,
            },
          })
        }
      />

      <label>Output TDMS Path</label>
      <input
        style={styles.input}
        value={config.MeasurementConfig?.OutputTDMSPath || ""}
        onChange={(e) =>
          setConfig({
            ...config,
            MeasurementConfig: {
              ...config.MeasurementConfig,
              OutputTDMSPath: e.target.value,
            },
          })
        }
      />

      <h4>Trigger</h4>

      <label>Sample Rate</label>
      <input
        type="number"
        style={styles.input}
        value={config.TriggerConfig?.SampleRate ?? 0}
        onChange={(e) =>
          setConfig({
            ...config,
            TriggerConfig: {
              ...config.TriggerConfig,
              SampleRate: Number(e.target.value),
            },
          })
        }
      />

      <label>Pre Trigger Window (ms)</label>
      <input
        type="number"
        style={styles.input}
        value={config.TriggerConfig?.PreTriggerWindowMs ?? 0}
        onChange={(e) =>
          setConfig({
            ...config,
            TriggerConfig: {
              ...config.TriggerConfig,
              PreTriggerWindowMs: Number(e.target.value),
            },
          })
        }
      />

      <label>Post Trigger Window (ms)</label>
      <input
        type="number"
        style={styles.input}
        value={config.TriggerConfig?.PostTriggerWindowMs ?? 0}
        onChange={(e) =>
          setConfig({
            ...config,
            TriggerConfig: {
              ...config.TriggerConfig,
              PostTriggerWindowMs: Number(e.target.value),
            },
          })
        }
      />

      <label>Threshold</label>
      <input
        type="number"
        step="0.1"
        style={styles.input}
        value={config.TriggerConfig?.Threshold ?? 0}
        onChange={(e) =>
          setConfig({
            ...config,
            TriggerConfig: {
              ...config.TriggerConfig,
              Threshold: Number(e.target.value),
            },
          })
        }
      />

      <h4>Analysis</h4>

      <label>Mode Prominence Threshold (dB)</label>
      <input
        type="number"
        style={styles.input}
        value={config.AnalysisConfig?.ModeProminenceThresholddB ?? 0}
        onChange={(e) =>
          setConfig({
            ...config,
            AnalysisConfig: {
              ...config.AnalysisConfig,
              ModeProminenceThresholddB: Number(e.target.value),
            },
          })
        }
      />

      <label>Damping Filter Bandwidth (%)</label>
      <input
        type="number"
        step="0.01"
        style={styles.input}
        value={config.AnalysisConfig?.DampingFilterBandwidthPercent ?? 0}
        onChange={(e) =>
          setConfig({
            ...config,
            AnalysisConfig: {
              ...config.AnalysisConfig,
              DampingFilterBandwidthPercent: Number(e.target.value),
            },
          })
        }
      />

      <label>Damping Start Peak (%)</label>
      <input
        type="number"
        step="0.01"
        style={styles.input}
        value={config.AnalysisConfig?.DampingStartPeakPercent ?? 0}
        onChange={(e) =>
          setConfig({
            ...config,
            AnalysisConfig: {
              ...config.AnalysisConfig,
              DampingStartPeakPercent: Number(e.target.value),
            },
          })
        }
      />

      <label>Damping End Peak (%)</label>
      <input
        type="number"
        step="0.01"
        style={styles.input}
        value={config.AnalysisConfig?.DampingEndPeakPercent ?? 0}
        onChange={(e) =>
          setConfig({
            ...config,
            AnalysisConfig: {
              ...config.AnalysisConfig,
              DampingEndPeakPercent: Number(e.target.value),
            },
          })
        }
      />

      <label>Use N Dominant Modes</label>
      <input
        type="number"
        style={styles.input}
        value={config.AnalysisConfig?.UseNDominantModes ?? 0}
        onChange={(e) =>
          setConfig({
            ...config,
            AnalysisConfig: {
              ...config.AnalysisConfig,
              UseNDominantModes: Number(e.target.value),
            },
          })
        }
      />

      <div style={{ marginTop: 16 }}>
        <button onClick={onStart} style={styles.startBtn}>
          Start Experiment
        </button>
      </div>
    </div>
  );
}

const styles = {
  card: {
    backgroundColor: "#1b1b1b",
    padding: "12px",
    borderRadius: "10px",
    color: "white",
    overflowY: "auto",
    maxHeight: "100%",
  },

  input: {
    width: "100%",
    padding: "8px",
    marginTop: "4px",
    marginBottom: "10px",
    backgroundColor: "#222",
    color: "white",
    border: "1px solid #444",
    borderRadius: "4px",
    boxSizing: "border-box",
  },

  textarea: {
    width: "100%",
    minHeight: "80px",
    padding: "8px",
    marginTop: "4px",
    marginBottom: "10px",
    backgroundColor: "#222",
    color: "white",
    border: "1px solid #444",
    borderRadius: "4px",
    resize: "vertical",
    boxSizing: "border-box",
  },

  startBtn: {
    width: "100%",
    padding: "12px",
    backgroundColor: "#00d4ff",
    color: "#000",
    fontWeight: "bold",
    border: "none",
    borderRadius: "6px",
    cursor: "pointer",
  },
};