export default function ExperimentConfig({ config, setConfig }) {
  function update(field, value) {
    setConfig({ ...config, [field]: value });
  }

  return (
    <div style={{ color: "white" }}>
      <h2>Experiment Configuration</h2>

      {/* GENERAL */}
      <label>Experiment Name</label>
      <input
        value={config.experimentName}
        onChange={(e) => update("experimentName", e.target.value)}
        style={styles.input}
      />

      <label>Sample Rate (Hz)</label>
      <select
        value={config.sampleRate}
        onChange={(e) => update("sampleRate", Number(e.target.value))}
        style={styles.input}
      >
        <option value={1000}>1000 Hz</option>
        <option value={5000}>5000 Hz</option>
        <option value={10240}>10240 Hz</option>
      </select>

      <label>Chunk Size (FFT window)</label>
      <select
        value={config.chunkSize}
        onChange={(e) => update("chunkSize", Number(e.target.value))}
        style={styles.input}
      >
        {[256, 512, 1024, 2048, 4096, 8192, 16384].map((v) => (
          <option key={v} value={v}>
            {v}
          </option>
        ))}
      </select>

      {/* CHANNELS */}
      <h3>Channels</h3>

      {config.channels.map((ch, i) => (
        <div key={i} style={styles.box}>
          <label>Physical Channel</label>
          <input
            value={ch.physicalChannelName}
            onChange={(e) => {
              const copy = [...config.channels];
              copy[i].physicalChannelName = e.target.value;
              setConfig({ ...config, channels: copy });
            }}
            style={styles.input}
          />

          <label>Channel Name</label>
          <input
            value={ch.channelName}
            onChange={(e) => {
              const copy = [...config.channels];
              copy[i].channelName = e.target.value;
              setConfig({ ...config, channels: copy });
            }}
            style={styles.input}
          />

          <label>Sensitivity</label>
          <input
            type="number"
            value={ch.sensitivity}
            onChange={(e) => {
              const copy = [...config.channels];
              copy[i].sensitivity = Number(e.target.value);
              setConfig({ ...config, channels: copy });
            }}
            style={styles.input}
          />

          <label>Min Range</label>
          <input
            type="number"
            value={ch.minRange}
            onChange={(e) => {
              const copy = [...config.channels];
              copy[i].minRange = Number(e.target.value);
              setConfig({ ...config, channels: copy });
            }}
            style={styles.input}
          />

          <label>Max Range</label>
          <input
            type="number"
            value={ch.maxRange}
            onChange={(e) => {
              const copy = [...config.channels];
              copy[i].maxRange = Number(e.target.value);
              setConfig({ ...config, channels: copy });
            }}
            style={styles.input}
          />
        </div>
      ))}
    </div>
  );
}

const styles = {
  input: {
    width: "100%",
    padding: "8px",
    marginBottom: "10px",
    backgroundColor: "#222",
    color: "white",
    border: "1px solid #444",
  },

  box: {
    border: "1px solid #333",
    padding: "10px",
    marginBottom: "10px",
  },
};