import { useParams, useNavigate } from "react-router-dom";
import { useEffect, useState } from "react";
import {
  getApiaryById,
  getUpcomingSprayings,
  getNearbyCultures,
} from "../../api/apiaries";
import { getHivesByApiary, deleteHive } from "../../api/hives";
import { registerDevice } from "../../api/devices";

import type {
  Apiary,
  Hive,
  UpcomingSprayingAlert,
  NearbyCulture,
} from "../../types";

import HiveCard from "../../components/hives/HiveCard";
import HiveForm from "../../components/hives/HiveForm";

import {
  Button,
  Modal,
  EmptyState,
  Spinner,
  ConfirmDialog,
  Badge,
  Alert,
} from "../../components/common/ui";

import { PageLayout } from "../../components/layout/PageLayout";
import { ToastContainer } from "../../components/common/ui";
import { useToast } from "../../hooks/useToast";
import MapView from "../../components/map/MapView";

export default function ApiaryDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [apiary, setApiary] = useState<Apiary | null>(null);
  const [hives, setHives] = useState<Hive[]>([]);
  const [alerts, setAlerts] = useState<UpcomingSprayingAlert[]>([]);
  const [cultures, setCultures] = useState<NearbyCulture[]>([]);
  const [loading, setLoading] = useState(true);

  const [showAddHive, setShowAddHive] = useState(false);
  const [editTarget, setEditTarget] = useState<Hive | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Hive | null>(null);

  const [registerTarget, setRegisterTarget] = useState<Hive | null>(null);
  const [serialNumber, setSerialNumber] = useState("");
  const [registering, setRegistering] = useState(false);

  const { toasts, addToast, removeToast } = useToast();

  const loadApiary = async () => {
    const res = await getApiaryById(id!);
    setApiary(res.data);
  };

  const loadHives = async () => {
    const res = await getHivesByApiary(id!);
    setHives(res.data);
  };

  const loadAlerts = async () => {
    const res = await getUpcomingSprayings(id!);
    setAlerts(res.data);
  };

  const loadCultures = async () => {
    const res = await getNearbyCultures(id!);
    setCultures(res.data);
  };

  useEffect(() => {
    const load = async () => {
      try {
        await Promise.all([
          loadApiary(),
          loadHives(),
          loadAlerts(),
          loadCultures(),
        ]);
      } catch {
        addToast("Failed to load apiary details.", "error");
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [id]);

  const handleDeleteHive = async (hiveId: string) => {
    await deleteHive(hiveId);
    addToast("Hive deleted.", "success");
    loadHives();
  };

  const handleRegisterDevice = async () => {
    try {
      setRegistering(true);
      await registerDevice(registerTarget!.id, serialNumber);
      addToast("Device registered!", "success");
      setRegisterTarget(null);
      setSerialNumber("");
      loadHives();
    } catch {
      addToast("Registration failed.", "error");
    } finally {
      setRegistering(false);
    }
  };

  if (loading || !apiary) {
    return (
      <PageLayout title="Loading...">
        <Spinner />
      </PageLayout>
    );
  }

  return (
    <PageLayout title={apiary.name}>
      <ToastContainer toasts={toasts} onRemove={removeToast} />

      {/* Back */}
      <div className="flex items-center gap-3 mb-6">
        <button
          onClick={() => navigate("/beekeeper/apiaries")}
          className="text-gray-400 hover:text-gray-600 transition text-sm flex items-center gap-1"
        >
          ← Back to Apiaries
        </button>
      </div>

      {/* Spraying alerts */}
      {alerts.length > 0 && (
        <div className="mb-6 space-y-3">
          {alerts.map((a) => (
            <Alert
              key={a.scheduleId}
              variant="warning"
              message={
                <span>
                  ⚠️ <strong>Upcoming spraying near your apiary!</strong>
                  <br />
                  Plot: <strong>{a.plotName}</strong>
                  <br />
                  Scheduled at:{" "}
                  <strong>
                    {new Date(a.scheduledAt).toLocaleString()}
                  </strong>
                  <br />
                  Duration: {a.durationHours}h
                  {a.preparation && <> · {a.preparation}</>}
                </span> as any
              }
            />
          ))}
        </div>
      )}

      {/* Apiary info */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-8">
        <div className="lg:col-span-1 bg-white rounded-2xl border shadow-sm overflow-hidden">
          {apiary.imageUrl ? (
            <img
              src={apiary.imageUrl}
              alt={apiary.name}
              className="w-full h-44 object-cover"
            />
          ) : (
            <div className="w-full h-44 bg-gradient-to-br from-amber-100 to-amber-200 flex items-center justify-center">
              <span className="text-6xl opacity-30">🏡</span>
            </div>
          )}

          <div className="p-5">
            <h2 className="text-xl font-bold">{apiary.name}</h2>
            <p className="text-xs text-gray-400">
              📍 {apiary.latitude.toFixed(5)},{" "}
              {apiary.longitude.toFixed(5)}
            </p>

            {apiary.description && (
              <p className="text-sm text-gray-600 mt-2">
                {apiary.description}
              </p>
            )}

            <div className="flex gap-2 mt-3">
              <Badge label={`${hives.length} hives`} variant="amber" />
              <Badge
                label={`${hives.filter((h) => h.isPaired).length} IoT paired`}
                variant="green"
              />
            </div>

            <div className="mt-4">
              <Button
                variant="secondary"
                size="sm"
                onClick={() =>
                  navigate(`/beekeeper/apiaries/${id}/telemetry`)
                }
              >
                Open Telemetry & Alerts
              </Button>
            </div>
          </div>
        </div>

        {/* Map */}
        <div className="lg:col-span-2">
          <MapView
            position={{
              lat: apiary.latitude,
              lng: apiary.longitude,
            }}
            nearbyCultures={cultures}
            height="300px"
            readOnly
          />
        </div>
      </div>

      {/* Hives */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-xl font-bold">Hives</h2>
          <p className="text-gray-500 text-sm">
            {hives.length} hives in this apiary
          </p>
        </div>
        <Button onClick={() => setShowAddHive(true)}>
          + Add Hive
        </Button>
      </div>

      {hives.length === 0 ? (
        <EmptyState
          icon="🍯"
          title="No hives yet"
          description="Add your first hive to start tracking this apiary."
        />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
          {hives.map((h) => (
            <HiveCard
              key={h.id}
              hive={h}
              onEdit={() => setEditTarget(h)}
              onDelete={() => setDeleteTarget(h)}
              onRegisterDevice={() => setRegisterTarget(h)}
            />
          ))}
        </div>
      )}

      {/* Add hive modal */}
      <Modal
        open={showAddHive}
        onClose={() => setShowAddHive(false)}
        title="Add New Hive"
      >
        <HiveForm
          apiaryId={id!}
          onSuccess={() => {
            setShowAddHive(false);
            addToast("Hive added!", "success");
            loadHives();
          }}
        />
      </Modal>

      {/* Edit hive modal */}
      <Modal
        open={!!editTarget}
        onClose={() => setEditTarget(null)}
        title="Edit Hive"
      >
        {editTarget && (
          <HiveForm
            apiaryId={id!}
            existing={editTarget}
            onSuccess={() => {
              setEditTarget(null);
              addToast("Hive updated!", "success");
              loadHives();
            }}
          />
        )}
      </Modal>

      {/* Register IoT modal */}
      <Modal
        open={!!registerTarget}
        onClose={() => {
          setRegisterTarget(null);
          setSerialNumber("");
        }}
        title="Register IoT Device"
      >
        <div className="flex flex-col gap-4">
          <p className="text-sm text-gray-500">
            Enter the serial number from the back of the smart scale.
          </p>

          <input
            className="border rounded-lg px-3 py-2 text-sm"
            placeholder="SA-2024-00123"
            value={serialNumber}
            onChange={(e) => setSerialNumber(e.target.value)}
          />

          <div className="flex gap-2 justify-end">
            <Button
              variant="secondary"
              onClick={() => setRegisterTarget(null)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleRegisterDevice}
              disabled={registering || !serialNumber}
            >
              {registering ? "Registering..." : "Register"}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Delete confirm */}
      <ConfirmDialog
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() =>
          deleteTarget && handleDeleteHive(deleteTarget.id)
        }
        title="Delete Hive"
        message={`Delete hive "${deleteTarget?.label}"? This cannot be undone.`}
        confirmLabel="Delete Hive"
      />
    </PageLayout>
  );
}