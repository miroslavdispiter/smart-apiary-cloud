import type { Hive } from "../../types";
import { HIVE_TYPE_LABELS } from "../../types";
import { Badge, Button } from "../common/ui";
import { useNavigate } from "react-router-dom";

interface HiveCardProps {
  hive: Hive;
  onRegisterDevice: () => void;
  onEdit: () => void;
  onDelete: () => void;
}

const typeVariant = (
  type: number
): "amber" | "blue" | "green" | "purple" | "gray" => {
  const map: Record<
    number,
    "amber" | "blue" | "green" | "purple" | "gray"
  > = {
    1: "amber",
    2: "blue",
    3: "green",
    4: "purple",
    5: "gray",
  };

  return map[type] ?? "gray";
};

export default function HiveCard({
  hive,
  onRegisterDevice,
  onEdit,
  onDelete,
}: HiveCardProps) {
  const navigate = useNavigate();

  return (
    <div className="bg-white rounded-2xl border border-gray-100 shadow-sm hover:shadow-md transition-all duration-200 p-5 flex flex-col gap-4">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-full bg-amber-100 flex items-center justify-center text-xl">
            🍯
          </div>
          <div>
            <h4 className="font-bold text-gray-800 leading-tight">
              {hive.label}
            </h4>
            <Badge
              label={HIVE_TYPE_LABELS[hive.type] ?? "Unknown"}
              variant={typeVariant(hive.type)}
            />
          </div>
        </div>

        {hive.isPaired && (
          <span className="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded-full font-medium">
            ⚡ IoT Paired
          </span>
        )}
      </div>

      {/* Details */}
      <div className="grid grid-cols-2 gap-2 text-sm">
        {hive.boxColor && (
          <div className="flex items-center gap-2 text-gray-600">
            <span className="text-base">🎨</span>
            <span>{hive.boxColor}</span>
          </div>
        )}

        {hive.queenAge !== undefined &&
          hive.queenAge !== null && (
            <div className="flex items-center gap-2 text-gray-600">
              <span className="text-base">👑</span>
              <span>Queen: {hive.queenAge}y</span>
            </div>
          )}

        {hive.note && (
          <div className="col-span-2 flex items-start gap-2 text-gray-500 bg-gray-50 rounded-lg p-2">
            <span className="text-base">📝</span>
            <span className="text-xs line-clamp-2">
              {hive.note}
            </span>
          </div>
        )}
      </div>

      {/* Actions */}
      <div className="flex gap-2 pt-2 border-t border-gray-50">
        <Button
          variant="ghost"
          size="sm"
          onClick={() =>
            navigate(`/beekeeper/hives/${hive.id}/diary`)
          }
        >
          Dnevnik
        </Button>

        {!hive.isPaired && (
          <Button variant="ghost" size="sm" onClick={onRegisterDevice}>Register Device
        </Button>
)}

        <Button
          variant="secondary"
          size="sm"
          className="flex-1"
          onClick={onEdit}
        >
          Edit
        </Button>

        <Button
          variant="danger"
          size="sm"
          onClick={onDelete}
        >
          Delete
        </Button>
      </div>
    </div>
  );
}