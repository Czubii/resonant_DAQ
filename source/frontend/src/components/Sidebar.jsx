import { useState } from "react";

export default function Sidebar({ onSelect }) {
  const [hover, setHover] = useState(null);

  return (
    <div style={styles.sidebar}>
      <h2 style={{ color: "white" }}>Menu</h2>

      {/* NEW EXPERIMENT */}
      <div
        style={styles.item}
        onMouseEnter={() => setHover("new")}
      >
        New Experiment
      </div>

      {hover === "new" && (
        <div style={styles.flyout}>
          <div
            style={styles.subItem}
            onClick={() => onSelect("new-config")}
          >
            Modal response
          </div>
        </div>
      )}

      {/* LOAD EXPERIMENT */}
      <div
        style={styles.item}
        onMouseEnter={() => setHover("load")}
      >
        Load Experiment
      </div>

      {hover === "load" && (
        <div style={styles.flyout}>
          <div
            style={styles.subItem}
            onClick={() => onSelect("load")}
          >
            Load experiment
          </div>
        </div>
      )}
    </div>
  );
}

const styles = {
  sidebar: {
    width: "220px",
    height: "100vh",
    backgroundColor: "#0f0f0f",
    padding: "10px",
    position: "fixed",
    left: 0,
    top: 0,
  },

  item: {
    padding: "10px",
    marginTop: "10px",
    backgroundColor: "#222",
    color: "white",
    borderRadius: "6px",
    cursor: "pointer",
  },

  flyout: {
    marginLeft: "10px",
    marginTop: "5px",
    padding: "8px",
    backgroundColor: "#1b1b1b",
    border: "1px solid #333",
    borderRadius: "8px",
  },

  subItem: {
    padding: "8px",
    backgroundColor: "#2a2a2a",
    marginTop: "5px",
    borderRadius: "6px",
    cursor: "pointer",
    color: "#aaa",
  },
};