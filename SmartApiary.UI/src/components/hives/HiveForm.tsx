import { useState } from "react";
import { Input, Select, Textarea, Button, Alert } from "../common/ui";
import { createHive, updateHive } from "../../api/hives";
import type { Hive } from "../../types";

const HIVE_TYPE_OPTIONS = [
  { value: "LR", label: "LR" },
  { value: "DB", label: "DB" },
  { value: "Poloshka", label: "Poloshka" },
  { value: "Dadant", label: "Dadant" },
  { value: "Other", label: "Other" },
];

interface HiveFormProps {
  apiaryId: string;
  existing?: Hive;
  onSuccess: () => void;
  onCancel?: () => void;
}

export default function HiveForm({ apiaryId, existing, onSuccess, onCancel }: HiveFormProps) {
  const [label, setLabel] = useState(existing?.label ?? "");
  const [type, setType] = useState<string>(existing?.type ?? "LR");
  const [boxColor, setBoxColor] = useState(existing?.boxColor ?? "");
  const [queenAge, setQueenAge] = useState<string>(existing?.queenAge?.toString() ?? "");
  const [note, setNote] = useState(existing?.note ?? "");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    setError("");
    if (!label.trim()) return setError("Hive label is required.");

    setLoading(true);
    try {
      const payload = {
        label: label.trim(),
        type,
        boxColor: boxColor.trim() || undefined,
        queenAge: queenAge !== "" ? Number(queenAge) : undefined,
        note: note.trim() || undefined,
      };

      if (existing) {
        await updateHive(existing.id, payload);
      } else {
        await createHive({ ...payload, apiaryId });
      }
      onSuccess();
    } catch (err: any) {
      setError(err.response?.data?.message || "Failed to save hive.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-4">
      <Input
        label="Hive Label *"
        placeholder="e.g. H-01, Slot A..."
        value={label}
        onChange={(e) => setLabel(e.target.value)}
      />

      <Select
        label="Hive Type *"
        value={type}
        onChange={(e) => setType(e.target.value)}
        options={HIVE_TYPE_OPTIONS}
      />

      <div className="grid grid-cols-2 gap-4">
        <Input
          label="Box Color"
          placeholder="e.g. Blue, Yellow..."
          value={boxColor}
          onChange={(e) => setBoxColor(e.target.value)}
        />
        <Input
          label="Queen Age (years)"
          type="number"
          min={0}
          max={10}
          placeholder="e.g. 2"
          value={queenAge}
          onChange={(e) => setQueenAge(e.target.value)}
        />
      </div>

      <Textarea
        label="Notes"
        placeholder="Varoa treatment, added cake, etc..."
        value={note}
        onChange={(e) => setNote(e.target.value)}
        rows={3}
      />

      {error && <Alert message={error} variant="error" />}

      <div className="flex gap-3 pt-1">
        <Button onClick={handleSubmit} loading={loading} className="flex-1">
          {existing ? "Update Hive" : "Add Hive"}
        </Button>
        {onCancel && (
          <Button variant="secondary" onClick={onCancel}>
            Cancel
          </Button>
        )}
      </div>
    </div>
  );
}
