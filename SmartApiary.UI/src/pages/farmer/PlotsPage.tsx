import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getPlots, deletePlot } from "../../api/farmer";
import type { Plot } from "../../types/farmer";
import PlotCard from "../../components/farmer/PlotCard";
import PlotForm from "../../components/farmer/PlotForm";
import { Button, Modal, EmptyState, Spinner, ConfirmDialog, Input } from "../../components/common/ui";
import { ToastContainer } from "../../components/common/ui";
import { PageLayout } from "../../components/layout/PageLayout";
import { useToast } from "../../hooks/useToast";

export default function PlotsPage() {
  const navigate = useNavigate();
  const [plots, setPlots] = useState<Plot[]>([]);
  const [loading, setLoading] = useState(true);
  const [showAdd, setShowAdd] = useState(false);
  const [editTarget, setEditTarget] = useState<Plot | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Plot | null>(null);
  const [search, setSearch] = useState("");
  const { toasts, addToast, removeToast } = useToast();

  const load = async () => {
    try {
      const res = await getPlots();
      setPlots(res.data);
    } catch {
      addToast("Failed to load plots.", "error");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const handleDelete = async (id: string) => {
    try {
      await deletePlot(id);
      addToast("Plot deleted.", "success");
      load();
    } catch {
      addToast("Delete failed.", "error");
    }
  };

  const filtered = plots.filter(
    (p) => !search || p.name.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <PageLayout title="My Plots">
      <ToastContainer toasts={toasts} onRemove={removeToast} />

      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">My Plots</h1>
          <p className="text-gray-500 text-sm mt-1">{plots.length} plots registered</p>
        </div>
        <div className="flex gap-3">
          <Button variant="secondary" onClick={() => navigate("/farmer/spraying/history")}>
            📋 Spraying History
          </Button>
          <Button onClick={() => setShowAdd(true)} icon={<span>+</span>}>
            Add Plot
          </Button>
        </div>
      </div>

      {/* Search */}
      {plots.length > 0 && (
        <div className="mb-6">
          <Input
            placeholder="Search plots..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            icon={<span>🔍</span>}
          />
        </div>
      )}

      {/* Content */}
      {loading ? (
        <Spinner />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon="🌾"
          title={search ? "No plots match your search" : "No plots yet"}
          description={
            search
              ? "Try a different search term."
              : "Add your first plot to start managing spraying schedules."
          }
          action={
            !search && (
              <Button onClick={() => setShowAdd(true)} icon={<span>+</span>}>
                Add First Plot
              </Button>
            )
          }
        />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {filtered.map((p) => (
            <PlotCard
              key={p.id}
              plot={p}
              onClick={() => navigate(`/farmer/plots/${p.id}`)}
              onEdit={() => setEditTarget(p)}
              onDelete={() => setDeleteTarget(p)}
            />
          ))}
        </div>
      )}

      {/* Add modal */}
      <Modal open={showAdd} onClose={() => setShowAdd(false)} title="Add New Plot" maxWidth="max-w-2xl">
        <PlotForm
          onSuccess={() => { setShowAdd(false); addToast("Plot added!", "success"); load(); }}
          onCancel={() => setShowAdd(false)}
        />
      </Modal>

      {/* Edit modal */}
      <Modal open={!!editTarget} onClose={() => setEditTarget(null)} title="Edit Plot" maxWidth="max-w-2xl">
        {editTarget && (
          <PlotForm
            existing={editTarget}
            onSuccess={() => { setEditTarget(null); addToast("Plot updated!", "success"); load(); }}
            onCancel={() => setEditTarget(null)}
          />
        )}
      </Modal>

      {/* Delete confirm */}
      <ConfirmDialog
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => deleteTarget && handleDelete(deleteTarget.id)}
        title="Delete Plot"
        message={`Delete "${deleteTarget?.name}"? All spraying schedules will also be removed. This cannot be undone.`}
        confirmLabel="Delete Plot"
      />
    </PageLayout>
  );
}
