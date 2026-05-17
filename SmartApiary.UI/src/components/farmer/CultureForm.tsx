import { useState } from "react";
import { setCulture } from "../../api/farmer";
import { CultureType, CULTURE_TYPE_LABELS } from "../../types/farmer";
import { Button, Input, Select, Textarea } from "../common/ui";

interface CultureFormProps {
  plotId: string;
  onSuccess: () => void;
  onCancel: () => void;
}

const CULTURE_OPTIONS = Object.entries(CULTURE_TYPE_LABELS)
  .filter(([k]) => Number(k) !== CultureType.Unknown)
  .map(([value, label]) => ({ value: Number(value), label }));

export default function CultureForm({ plotId, onSuccess, onCancel }: CultureFormProps) {
  const [type, setType] = useState<number>(CultureType.Sunflower);
  const [bloomingStart, setBloomingStart] = useState("");
  const [bloomingEnd, setBloomingEnd] = useState("");
  const [additionalInfo, setAdditionalInfo] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async () => {
    if (!bloomingStart || !bloomingEnd) { setError("Blooming dates are required."); return; }
    if (bloomingEnd <= bloomingStart) { setError("End date must be after start date."); return; }

    setLoading(true);
    setError("");
    try {
      await setCulture(plotId, {
        type,
        bloomingStart: new Date(bloomingStart).toISOString(),
        bloomingEnd: new Date(bloomingEnd).toISOString(),
        additionalInfo: additionalInfo || undefined,
      });
      onSuccess();
    } catch (e: any) {
      setError(e?.response?.data?.message ?? "An error occurred.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col gap-4">
      <Select
        label="Culture Type"
        options={CULTURE_OPTIONS}
        value={type}
        onChange={(e) => setType(Number(e.target.value))}
      />
      <Input
        label="Blooming Start"
        type="date"
        value={bloomingStart}
        onChange={(e) => setBloomingStart(e.target.value)}
      />
      <Input
        label="Blooming End"
        type="date"
        value={bloomingEnd}
        onChange={(e) => setBloomingEnd(e.target.value)}
      />
      <Textarea
        label="Additional Info (optional)"
        placeholder="Any extra information about this culture..."
        value={additionalInfo}
        onChange={(e) => setAdditionalInfo(e.target.value)}
      />

      {error && <p className="text-sm text-red-500">{error}</p>}

      <div className="flex gap-3 justify-end pt-2">
        <Button variant="secondary" onClick={onCancel}>Cancel</Button>
        <Button loading={loading} onClick={handleSubmit}>Set Culture</Button>
      </div>
    </div>
  );
}
