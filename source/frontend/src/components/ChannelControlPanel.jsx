export default function ChannelControlPanel({
  channels=[],
  setChannels,
}) {
  function update(i, field, value) {
    const copy = [...channels];
    copy[i] = {
      ...copy[i],
      [field]: value,
    };
    setChannels(copy);
  }

  return (
    <div style={styles.card}>
      <h3 style={styles.title}>Channel Configuration</h3>

      {channels.map((ch, i) => (
        <div key={ch.physicalChannelName || i} style={styles.box}>
          <div style={styles.header}>
            <div style={styles.channelTitle}>
              {ch.physicalChannelName}
            </div>

            <label style={styles.toggle}>
              <input
                type="checkbox"
                checked={!!ch.enabled}
                onChange={(e) =>
                  update(i, "enabled", e.target.checked)
                }
              />
              <span>
                {ch.enabled ? "On" : "Off"}
              </span>
            </label>
          </div>

          {ch.enabled && (
            <>
              <label style={styles.label}>
                Channel Name
              </label>
              <input
                style={styles.input}
                value={ch.channelName}
                onChange={(e) =>
                  update(i, "channelName", e.target.value)
                }
              />

              <label style={styles.label}>
                Min Range
              </label>
              <input
                type="number"
                style={styles.input}
                value={ch.minRange ?? ""}
                onChange={(e) =>
                  update(
                    i,
                    "minRange",
                    Number(e.target.value)
                  )
                }
              />

              <label style={styles.label}>
                Max Range
              </label>
              <input
                type="number"
                style={styles.input}
                value={ch.maxRange ?? ""}
                onChange={(e) =>
                  update(
                    i,
                    "maxRange",
                    Number(e.target.value)
                  )
                }
              />

              <label style={styles.label}>
                Sensitivity
              </label>
              <input
                type="number"
                style={styles.input}
                value={ch.sensitivity ?? ""}
                onChange={(e) =>
                  update(
                    i,
                    "sensitivity",
                    Number(e.target.value)
                  )
                }
              />
            </>
          )}
        </div>
      ))}
    </div>
  );
}

const styles = {
  card: {
    backgroundColor: "#1b1b1b",
    padding: "10px",
    borderRadius: "10px",
    color: "white",
  },

  title: {
    color: "white",
    marginBottom: "12px",
  },

  box: {
    border: "1px solid #333",
    borderRadius: "8px",
    padding: "10px",
    marginTop: "8px",
  },

  header: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
  },

  channelTitle: {
    fontWeight: 600,
    fontSize: "14px",
  },

  toggle: {
    display: "flex",
    alignItems: "center",
    gap: "6px",
    cursor: "pointer",
  },

  label: {
    display: "block",
    marginTop: "10px",
    color: "white",
  },

  input: {
    width: "100%",
    padding: "6px",
    marginTop: "6px",
    backgroundColor: "#222",
    color: "white",
    border: "1px solid #444",
    boxSizing: "border-box",
  },
};
