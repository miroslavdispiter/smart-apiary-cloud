import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getPlots, getSprayingHistory } from "../../api/farmer";
import type { Plot, SprayingSchedule } from "../../types/farmer";
import { SprayingStatus, SPRAYING_STATUS_LABELS, CULTURE_TYPE_LABELS } from "../../types/farmer";
import {
  Button, Spinner, Badge, Card, Select, Input, EmptyState,
} from "../../components/common/ui";
import { ToastContainer } from "../../components/common/ui";
import { PageLayout } from "../../components/layout/PageLayout";
import { useToast } from "../../hooks/useToast";

const statusVariant = (s: SprayingStatus) => {
  if (s === SprayingStatus.Scheduled) return "yellow";
  if (s === SprayingStatus.Completed) return "green";
  if (s === SprayingStatus.Cancelled) return "red";
  return "gray";
};

export default function SprayingHistoryPage() {
  const navigate = useNavigate();
  const { toasts, addToast, removeToast } = useToast();

  const [plots, setPlots] = useState<Plot[]>([]);
  const [schedules, setSchedules] = useState<SprayingSchedule[]>([]);
  const [loading, setLoading] = useState(true);

  // Filters
  const [plotFilter, setPlotFilter] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [completedOnly, setCompletedOnly] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const [plotsRes, historyRes] = await Promise.all([
        getPlots(),
        getSprayingHistory({
          plotId: plotFilter || undefined,
          from: from ? new Date(from).toISOString() : undefined,
          to: to ? new Date(to).toISOString() : undefined,
          completedOnly,
        }),
      ]);
      setPlots(plotsRes.data);
      setSchedules(historyRes.data);
    } catch {
      addToast("Failed to load history.", "error");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const plotOptions = [
    { value: "", label: "All plots" },
    ...plots.map((p) => ({ value: p.id, label: p.name })),
  ];

  return (
    <PageLayout title="Spraying History">
      <ToastContainer toasts={toasts} onRemove={removeToast} />

      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div>
          <button
            onClick={() => navigate("/farmer/plots")}
            className="text-sm text-amber-600 hover:text-amber-700 mb-2 flex items-center gap-1"
          >
            ← Back to Plots
          </button>
          <h1 className="text-2xl font-bold text-gray-800">Spraying History</h1>
          <p className="text-gray-500 text-sm mt-1">Digital spraying card — all pesticide treatments</p>
        </div>
      </div>

      {/* Filters */}
      <Card className="p-5 mb-6">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <Select
            label="Plot"
            options={plotOptions}
            value={plotFilter}
            onChange={(e) => setPlotFilter(e.target.value)}
          />
          <Input
            label="From"
            type="date"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
          />
          <Input
            label="To"
            type="date"
            value={to}
            onChange={(e) => setTo(e.target.value)}
          />
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Status</label>
            <label className="flex items-center gap-2 mt-2 cursor-pointer">
              <input
                type="checkbox"
                checked={completedOnly}
                onChange={(e) => setCompletedOnly(e.target.checked)}
                className="w-4 h-4 accent-amber-500"
              />
              <span className="text-sm text-gray-600">Completed only</span>
            </label>
          </div>
        </div>
        <div className="flex gap-3 mt-4">
          <Button onClick={load}>Apply Filters</Button>
          <Button
            variant="secondary"
            onClick={() => {
              setPlotFilter("");
              setFrom("");
              setTo("");
              setCompletedOnly(false);
            }}
          >
            Clear
          </Button>
        </div>
      </Card>

      {/* Table */}
      {loading ? (
        <Spinner />
      ) : schedules.length === 0 ? (
        <EmptyState
          icon="📋"
          title="No spraying records found"
          description="Try adjusting the filters or schedule a spraying treatment."
        />
      ) : (
        <Card className="overflow-hidden">
          {/* Summary */}
          <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
            <p className="text-sm text-gray-500">
              {schedules.length} record{schedules.length !== 1 ? "s" : ""} found
            </p>
            <p className="text-xs text-gray-400 italic">
              💡 PDF export available via browser print (Ctrl+P)
            </p>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-gray-50 text-left">
                  <th className="px-6 py-3 font-semibold text-gray-600">Plot</th>
                  <th className="px-6 py-3 font-semibold text-gray-600">Date & Time</th>
                  <th className="px-6 py-3 font-semibold text-gray-600">Duration</th>
                  <th className="px-6 py-3 font-semibold text-gray-600">Preparation</th>
                  <th className="px-6 py-3 font-semibold text-gray-600">Status</th>
                  <th className="px-6 py-3 font-semibold text-gray-600">Notified</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {schedules.map((s) => (
                  <tr key={s.id} className="hover:bg-gray-50 transition">
                    <td className="px-6 py-4 font-medium text-gray-800">{s.plotName}</td>
                    <td className="px-6 py-4 text-gray-600">
                      {new Date(s.scheduledAt).toLocaleString()}
                    </td>
                    <td className="px-6 py-4 text-gray-600">{s.durationHours}h</td>
                    <td className="px-6 py-4 text-gray-500">
                      {s.preparation ?? <span className="text-gray-300">—</span>}
                    </td>
                    <td className="px-6 py-4">
                      <Badge
                        label={SPRAYING_STATUS_LABELS[s.status]}
                        variant={statusVariant(s.status)}
                      />
                    </td>
                    <td className="px-6 py-4 text-gray-500">
                      {s.notifiedBeekeeperCount} 🐝
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}
    </PageLayout>
  );
}
