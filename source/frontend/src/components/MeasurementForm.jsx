import { useState } from "react";

function MeasurementForm({ onMeasurementReceived }) {
  const [samples, setSamples] = useState(1024);

  const handleMeasurement = async () => {
    console.log("Sending request...");

    // FAKE backend response for now
    const fakeVoltage = (Math.random() * 5).toFixed(2);

    onMeasurementReceived(fakeVoltage);
  };

  return (
    <div>
      <h2>Take Measurement</h2>

      <input
        type="number"
        value={samples}
        onChange={(e) => setSamples(e.target.value)}
      />

      <button onClick={handleMeasurement}>
        Take Measurement
      </button>
    </div>
  );
}

export default MeasurementForm;