import { useState } from "react";
import { createPlot, updatePlot } from "../../api/farmer";
import type { Plot } from "../../types/farmer";
import { Button, Input } from "../common/ui";
import MapView, { type LatLng } from "../map/MapView";

interface PlotFormProps {
  existing?: Plot;
  onSuccess: () => void;
  onCancel: () => void;
}

export default function PlotForm({ existing, onSuccess, onCancel }: PlotFormProps) {
  const [name, setName] = useState(existing?.name ?? "");
  const [position, setPosition] = useState<LatLng | null>(
    existing ? { lat: existing.latitude, lng: existing.longitude } : null
  );
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async () => {
    if (!name.trim()) { setError("Plot name is required."); return; }
    if (!position) { setError("Please select a location on the map."); return; }

    setLoading(true);
    setError("");
    try {
      const data = { name: name.trim(), latitude: position.lat, longitude: position.lng };
      if (existing) {
        await updatePlot(existing.id, data);
      } else {
        await createPlot(data);
      }
      onSuccess();
    } catch (e: any) {
      setError(e?.response?.data?.message ?? "An error occurred.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col gap-5">
      <Input
        label="Plot Name"
        placeholder="e.g. North Field"
        value={name}
        onChange={(e) => setName(e.target.value)}
      />

      <div>
        <label className="text-sm font-medium text-gray-700 mb-2 block">Location</label>
        <MapView position={position} setPosition={setPosition} height="280px" />
        {position && (
          <p className="text-xs text-gray-400 mt-1">
            {position.lat.toFixed(6)}, {position.lng.toFixed(6)}
          </p>
        )}
      </div>

      {error && <p className="text-sm text-red-500">{error}</p>}

      <div className="flex gap-3 justify-end pt-2">
        <Button variant="secondary" onClick={onCancel}>Cancel</Button>
        <Button loading={loading} onClick={handleSubmit}>
          {existing ? "Save Changes" : "Add Plot"}
        </Button>
      </div>
    </div>
  );
}
