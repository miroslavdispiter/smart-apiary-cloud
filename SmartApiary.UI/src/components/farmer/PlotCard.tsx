import type { Plot } from "../../types/farmer";
import { CULTURE_ICONS, CULTURE_TYPE_LABELS } from "../../types/farmer";
import { Badge, Button } from "../common/ui";

interface PlotCardProps {
  plot: Plot;
  onClick: () => void;
  onEdit: () => void;
  onDelete: () => void;
}

export default function PlotCard({ plot, onClick, onEdit, onDelete }: PlotCardProps) {
  return (
    <div
      className="bg-white rounded-2xl shadow-sm border border-gray-100 hover:shadow-md transition-shadow cursor-pointer"
      onClick={onClick}
    >
      <div className="p-5">
        {/* Header */}
        <div className="flex items-start justify-between mb-3">
          <div className="flex items-center gap-2">
            <span className="text-2xl">🌾</span>
            <div>
              <h3 className="font-semibold text-gray-800 text-base">{plot.name}</h3>
              <p className="text-xs text-gray-400">
                {plot.latitude.toFixed(4)}, {plot.longitude.toFixed(4)}
              </p>
            </div>
          </div>
        </div>

        {/* Culture badge */}
        {plot.culture ? (
          <div className="flex items-center gap-2 mb-4">
            <span className="text-xl">{CULTURE_ICONS[plot.culture.type]}</span>
            <div>
              <Badge label={CULTURE_TYPE_LABELS[plot.culture.type]} variant="green" />
              <p className="text-xs text-gray-400 mt-0.5">
                {new Date(plot.culture.bloomingStart).toLocaleDateString()} –{" "}
                {new Date(plot.culture.bloomingEnd).toLocaleDateString()}
              </p>
            </div>
          </div>
        ) : (
          <div className="mb-4">
            <Badge label="No culture" variant="gray" />
          </div>
        )}

        {/* Actions */}
        <div className="flex gap-2 pt-3 border-t border-gray-50" onClick={(e) => e.stopPropagation()}>
          <Button size="sm" variant="ghost" onClick={onEdit} className="flex-1">
            ✏️ Edit
          </Button>
          <Button size="sm" variant="danger" onClick={onDelete} className="flex-1">
            🗑️ Delete
          </Button>
        </div>
      </div>
    </div>
  );
}
