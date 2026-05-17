import { useState } from "react";
import { scheduleSpraying, rescheduleSpraying } from "../../api/farmer";
import type { SprayingSchedule } from "../../types/farmer";
import { Button, Input } from "../common/ui";

interface SprayingFormProps {
  plotId: string;
  existing?: SprayingSchedule; // if set → reschedule mode
  onSuccess: () => void;
  onCancel: () => void;
}

export default function SprayingForm({ plotId, existing, onSuccess, onCancel }: SprayingFormProps) {
  const [scheduledAt, setScheduledAt] = useState(
    existing ? existing.scheduledAt.slice(0, 16) : ""
  );
  const [durationHours, setDurationHours] = useState(existing?.durationHours ?? 1);
  const [preparation, setPreparation] = useState(existing?.preparation ?? "");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [warning, setWarning] = useState("");

  const handleSubmit = async () => {
    if (!scheduledAt) { setError("Scheduled date and time is required."); return; }
    if (durationHours < 1) { setError("Duration must be at least 1 hour."); return; }

    setLoading(true);
    setError("");
    setWarning("");
    try {
      if (existing) {
        await rescheduleSpraying(plotId, existing.id, {
          newScheduledAt: new Date(scheduledAt).toISOString(),
          newDurationHours: durationHours,
        });
      } else {
        await scheduleSpraying(plotId, {
          scheduledAt: new Date(scheduledAt).toISOString(),
          durationHours,
          preparation: preparation || undefined,
        });
      }
      onSuccess();
    } catch (e: any) {
      const msg: string = e?.response?.data?.message ?? "";
      // 422 weather warning
      if (e?.response?.status === 422 || msg.toLowerCase().includes("weather") || msg.toLowerCase().includes("wind")) {
        setWarning(msg);
      } else {
        setError(msg || "An error occurred.");
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col gap-4">
      <Input
        label="Date and Time"
        type="datetime-local"
        value={scheduledAt}
        onChange={(e) => setScheduledAt(e.target.value)}
      />
      <Input
        label="Duration (hours)"
        type="number"
        min={1}
        max={24}
        value={durationHours}
        onChange={(e) => setDurationHours(Number(e.target.value))}
      />
      {!existing && (
        <Input
          label="Preparation (optional)"
          placeholder="e.g. Roundup, Herbicide X..."
          value={preparation}
          onChange={(e) => setPreparation(e.target.value)}
        />
      )}

      {warning && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg px-4 py-3 text-sm text-yellow-800">
          ⚠️ {warning}
        </div>
      )}
      {error && <p className="text-sm text-red-500">{error}</p>}

      <div className="flex gap-3 justify-end pt-2">
        <Button variant="secondary" onClick={onCancel}>Cancel</Button>
        <Button loading={loading} onClick={handleSubmit}>
          {existing ? "Reschedule" : "Schedule Spraying"}
        </Button>
      </div>
    </div>
  );
}
