function ControlPanel() {
  return (
    <div>
      <h2>Measurement Controls</h2>

      <div style={styles.grid}>
        <div>
          <label>Number of Samples</label>

          <input
            type="number"
            defaultValue={1024}
            style={styles.input}
          />
        </div>

        <div>
          <label>Window Type</label>

          <select style={styles.input}>
            <option>Hanning</option>
            <option>Hamming</option>
            <option>Blackman</option>
          </select>
        </div>

        <div>
          <label>Sampling Rate</label>

          <select style={styles.input}>
            <option>1000 kS/s</option>
            <option>5000 kS/s</option>
            <option>10000 kS/s</option>
          </select>
        </div>
      </div>

      <button style={styles.button}>
        Take Measurement
      </button>
    </div>
  );
}

const styles = {
  grid: {
    display: "grid",
    gridTemplateColumns: "1fr 1fr 1fr",
    gap: "20px",
    marginTop: "20px",
    marginBottom: "20px",
  },

  input: {
    width: "100%",
    padding: "10px",
    marginTop: "5px",
    backgroundColor: "#222",
    color: "white",
    border: "1px solid #444",
  },

  button: {
    padding: "12px 20px",
    backgroundColor: "#ff8800",
    border: "none",
    color: "white",
    cursor: "pointer",
    borderRadius: "5px",
  },
};

export default ControlPanel;