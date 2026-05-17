import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
    getPlots, cancelSpraying, removeCulture, getSprayingHistory,
} from "../../api/farmer";
import type { Plot, SprayingSchedule } from "../../types/farmer";
import { SprayingStatus, SPRAYING_STATUS_LABELS, CULTURE_ICONS, CULTURE_TYPE_LABELS } from "../../types/farmer";
import SprayingForm from "../../components/farmer/SprayingForm";
import CultureForm from "../../components/farmer/CultureForm";
import {
    Button, Modal, Spinner, ConfirmDialog, Badge, Card, EmptyState,
} from "../../components/common/ui";
import { ToastContainer } from "../../components/common/ui";
import { PageLayout } from "../../components/layout/PageLayout";
import { useToast } from "../../hooks/useToast";
import MapView from "../../components/map/MapView";

const statusVariant = (s: SprayingStatus) => {
    if (s === SprayingStatus.Scheduled) return "yellow";
    if (s === SprayingStatus.Completed) return "green";
    if (s === SprayingStatus.Cancelled) return "red";
    return "gray";
};

export default function PlotDetailsPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { toasts, addToast, removeToast } = useToast();

    const [plot, setPlot] = useState<Plot | null>(null);
    const [schedules, setSchedules] = useState<SprayingSchedule[]>([]);
    const [loading, setLoading] = useState(true);

    const [showSchedule, setShowSchedule] = useState(false);
    const [rescheduleTarget, setRescheduleTarget] = useState<SprayingSchedule | null>(null);
    const [cancelTarget, setCancelTarget] = useState<SprayingSchedule | null>(null);
    const [showCulture, setShowCulture] = useState(false);
    const [removeCultureConfirm, setRemoveCultureConfirm] = useState(false);

    const load = async () => {
        if (!id) return;
        try {
            const [plotsRes, historyRes] = await Promise.all([
                getPlots(),
                getSprayingHistory({ plotId: id, completedOnly: false }),
            ]);
            const found = (plotsRes.data as Plot[]).find((p) => p.id === id);
            if (!found) { navigate("/farmer/plots"); return; }
            setPlot(found);

            
            const raw = historyRes.data;
            const arr: SprayingSchedule[] = Array.isArray(raw) ? raw : raw?.value ?? raw?.data ?? [];
            setSchedules(arr);
        } catch (e) {
            console.error("Load error:", e);
            addToast("Failed to load plot details.", "error");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { load(); }, [id]);

    const handleCancel = async (plotId: string, scheduleId: string) => {
        try {
            await cancelSpraying(plotId, scheduleId);
            addToast("Spraying cancelled. Beekeepers notified.", "success");
            load();
        } catch {
            addToast("Failed to cancel spraying.", "error");
        }
    };

    const handleRemoveCulture = async () => {
        if (!plot) return;
        try {
            await removeCulture(plot.id);
            addToast("Culture removed.", "success");
            load();
        } catch {
            addToast("Failed to remove culture.", "error");
        }
    };

    if (loading) return <PageLayout title="Plot Details"><Spinner /></PageLayout>;
    if (!plot) return null;

    const scheduled = schedules.filter(
        (s) => s.status === SprayingStatus.Scheduled || (s.status as any) === "Scheduled"
    );
    const history = schedules.filter(
        (s) => s.status !== SprayingStatus.Scheduled && (s.status as any) !== "Scheduled"
    );

    return (
        <PageLayout title={plot.name}>
            <ToastContainer toasts={toasts} onRemove={removeToast} />

            {/* Back */}
            <button
                onClick={() => navigate("/farmer/plots")}
                className="text-sm text-amber-600 hover:text-amber-700 mb-6 flex items-center gap-1"
            >
                ← Back to Plots
            </button>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

                {/* Left column */}
                <div className="lg:col-span-1 flex flex-col gap-6">

                    {/* Plot info card */}
                    <Card className="p-5">
                        <h2 className="font-bold text-gray-800 text-lg mb-4">📍 {plot.name}</h2>
                        <MapView
                            position={{ lat: plot.latitude, lng: plot.longitude }}
                            setPosition={() => { }}
                            readOnly
                            height="200px"
                        />
                        <p className="text-xs text-gray-400 mt-2">
                            {plot.latitude.toFixed(6)}, {plot.longitude.toFixed(6)}
                        </p>
                    </Card>

                    {/* Culture card */}
                    <Card className="p-5">
                        <div className="flex items-center justify-between mb-4">
                            <h2 className="font-bold text-gray-800">🌱 Culture</h2>
                            {plot.culture ? (
                                <div className="flex gap-2">
                                    <Button size="sm" variant="secondary" onClick={() => setShowCulture(true)}>
                                        Change
                                    </Button>
                                    <Button size="sm" variant="danger" onClick={() => setRemoveCultureConfirm(true)}>
                                        Remove
                                    </Button>
                                </div>
                            ) : (
                                <Button size="sm" onClick={() => setShowCulture(true)}>+ Set Culture</Button>
                            )}
                        </div>

                        {plot.culture ? (
                            <div className="flex items-start gap-3">
                                <span className="text-3xl">{CULTURE_ICONS[plot.culture.type]}</span>
                                <div>
                                    <p className="font-semibold text-gray-800">
                                        {CULTURE_TYPE_LABELS[plot.culture.type]}
                                    </p>
                                    <p className="text-xs text-gray-500 mt-0.5">
                                        Blooming: {new Date(plot.culture.bloomingStart).toLocaleDateString()} –{" "}
                                        {new Date(plot.culture.bloomingEnd).toLocaleDateString()}
                                    </p>
                                    {plot.culture.additionalInfo && (
                                        <p className="text-xs text-gray-400 mt-1">{plot.culture.additionalInfo}</p>
                                    )}
                                </div>
                            </div>
                        ) : (
                            <p className="text-sm text-gray-400">No culture set on this plot.</p>
                        )}
                    </Card>
                </div>

                {/* Right column */}
                <div className="lg:col-span-2 flex flex-col gap-6">

                    {/* Scheduled spraying */}
                    <Card className="p-5">
                        <div className="flex items-center justify-between mb-4">
                            <h2 className="font-bold text-gray-800">💧 Scheduled Spraying</h2>
                            <Button size="sm" onClick={() => setShowSchedule(true)}>
                                + Schedule
                            </Button>
                        </div>

                        {scheduled.length === 0 ? (
                            <EmptyState
                                icon="💧"
                                title="No scheduled spraying"
                                description="Schedule a pesticide treatment for this plot."
                            />
                        ) : (
                            <div className="flex flex-col gap-3">
                                {scheduled.map((s) => (
                                    <div
                                        key={s.id}
                                        className="border border-gray-100 rounded-xl p-4 flex items-start justify-between gap-4"
                                    >
                                        <div>
                                            <div className="flex items-center gap-2 mb-1">
                                                <Badge label={SPRAYING_STATUS_LABELS[s.status]} variant={statusVariant(s.status)} />
                                                <span className="text-xs text-gray-400">
                                                    {s.notifiedBeekeeperCount} beekeeper{s.notifiedBeekeeperCount !== 1 ? "s" : ""} notified
                                                </span>
                                            </div>
                                            <p className="font-medium text-gray-800 text-sm">
                                                {new Date(s.scheduledAt).toLocaleString()}
                                            </p>
                                            <p className="text-xs text-gray-500">
                                                Duration: {s.durationHours}h
                                                {s.preparation && ` · ${s.preparation}`}
                                            </p>
                                        </div>
                                        <div className="flex gap-2 shrink-0">
                                            <Button size="sm" variant="warning" onClick={() => setRescheduleTarget(s)}>
                                                Reschedule
                                            </Button>
                                            <Button size="sm" variant="danger" onClick={() => setCancelTarget(s)}>
                                                Cancel
                                            </Button>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )}
                    </Card>

                    {/* History */}
                    <Card className="p-5">
                        <div className="flex items-center justify-between mb-4">
                            <h2 className="font-bold text-gray-800">📋 Spraying History</h2>
                            <Button
                                size="sm"
                                variant="secondary"
                                onClick={() => navigate("/farmer/spraying/history")}
                            >
                                Full History
                            </Button>
                        </div>

                        {history.length === 0 ? (
                            <p className="text-sm text-gray-400">No history yet.</p>
                        ) : (
                            <div className="flex flex-col gap-2">
                                {history.slice(0, 5).map((s) => (
                                    <div
                                        key={s.id}
                                        className="flex items-center justify-between py-2 border-b border-gray-50 last:border-0"
                                    >
                                        <div>
                                            <p className="text-sm font-medium text-gray-700">
                                                {new Date(s.scheduledAt).toLocaleDateString()}
                                            </p>
                                            <p className="text-xs text-gray-400">
                                                {s.durationHours}h{s.preparation && ` · ${s.preparation}`}
                                            </p>
                                        </div>
                                        <Badge label={SPRAYING_STATUS_LABELS[s.status]} variant={statusVariant(s.status)} />
                                    </div>
                                ))}
                            </div>
                        )}
                    </Card>
                </div>
            </div>

            {/* Modals */}
            <Modal open={showSchedule} onClose={() => setShowSchedule(false)} title="Schedule Spraying">
                <SprayingForm
                    plotId={plot.id}
                    onSuccess={() => {
                        setShowSchedule(false);
                        addToast("Spraying scheduled! Beekeepers notified.", "success");
                        load();
                    }}
                    onCancel={() => setShowSchedule(false)}
                />
            </Modal>

            <Modal open={!!rescheduleTarget} onClose={() => setRescheduleTarget(null)} title="Reschedule Spraying">
                {rescheduleTarget && (
                    <SprayingForm
                        plotId={plot.id}
                        existing={rescheduleTarget}
                        onSuccess={() => {
                            setRescheduleTarget(null);
                            addToast("Spraying rescheduled! Beekeepers notified.", "success");
                            load();
                        }}
                        onCancel={() => setRescheduleTarget(null)}
                    />
                )}
            </Modal>

            <Modal open={showCulture} onClose={() => setShowCulture(false)} title={plot.culture ? "Change Culture" : "Set Culture"}>
                <CultureForm
                    plotId={plot.id}
                    onSuccess={() => {
                        setShowCulture(false);
                        addToast("Culture updated!", "success");
                        load();
                    }}
                    onCancel={() => setShowCulture(false)}
                />
            </Modal>

            <ConfirmDialog
                open={!!cancelTarget}
                onClose={() => setCancelTarget(null)}
                onConfirm={() => cancelTarget && handleCancel(plot.id, cancelTarget.id)}
                title="Cancel Spraying"
                message="Cancel this spraying? All nearby beekeepers will be notified of the cancellation."
                confirmLabel="Cancel Spraying"
            />

            <ConfirmDialog
                open={removeCultureConfirm}
                onClose={() => setRemoveCultureConfirm(false)}
                onConfirm={handleRemoveCulture}
                title="Remove Culture"
                message="Remove the current culture from this plot? This is typically done after harvest."
                confirmLabel="Remove Culture"
                variant="warning"
            />
        </PageLayout>
    );
}
