import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import type * as signalR from "@microsoft/signalr";
import { getApiaryById } from "../../api/apiaries";
import { getHivesByApiary } from "../../api/hives";
import {
  createTelemetryHubConnection,
  getLatestTelemetry,
  getNectarDelta,
  getTelemetryByHive,
  type NectarDelta,
  type TelemetryAlert,
  type TelemetrySample,
} from "../../api/telemetry";
import type { Apiary, Hive } from "../../types";
import { Badge, Button, Card, Spinner } from "../../components/common/ui";
import { PageLayout } from "../../components/layout/PageLayout";
import { useToast } from "../../hooks/useToast";
import { ToastContainer } from "../../components/common/ui";
import { BarChart, LineChart } from "./telemetry/SimpleCharts";

const daysAgoIso = (days: number) => new Date(Date.now() - days * 24 * 60 * 60 * 1000).toISOString();
const nowIso = () => new Date().toISOString();

export default function TelemetryPage() {
  const { apiaryId } = useParams<{ apiaryId: string }>();
  const navigate = useNavigate();
  const [apiary, setApiary] = useState<Apiary | null>(null);
  const [hives, setHives] = useState<Hive[]>([]);
  const [activeHiveId, setActiveHiveId] = useState<string | null>(null);
  const [samples, setSamples] = useState<TelemetrySample[]>([]);
  const [latest, setLatest] = useState<TelemetrySample | null>(null);
  const [deltas, setDeltas] = useState<NectarDelta[]>([]);
  const [alerts, setAlerts] = useState<TelemetryAlert[]>([]);
  const [loading, setLoading] = useState(true);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const { toasts, addToast, removeToast } = useToast();

  const activeHive = useMemo(
    () => hives.find((h) => h.id === activeHiveId) ?? null,
    [hives, activeHiveId]
  );

  useEffect(() => {
    const loadInitial = async () => {
      if (!apiaryId) return;

      try {
        const [apiaryRes, hivesRes] = await Promise.all([
          getApiaryById(apiaryId),
          getHivesByApiary(apiaryId),
        ]);
        setApiary(apiaryRes.data);
        setHives(hivesRes.data);
        setActiveHiveId(hivesRes.data[0]?.id ?? null);
      } catch {
        addToast("Failed to load telemetry page.", "error");
      } finally {
        setLoading(false);
      }
    };

    loadInitial();
  }, [apiaryId]);

  useEffect(() => {
    if (!apiaryId) return;

    const hub = createTelemetryHubConnection();

    hub.on("TelemetryReceived", async (sample: TelemetrySample) => {
      if (sample.hiveId !== activeHiveId) return;
        setLatest(sample);
        setSamples((current) =>
        [...current.filter((s) => s.id !== sample.id), sample]
        .sort((a, b) => new Date(a.recordedAt).getTime() - new Date(b.recordedAt).getTime())
      );
      try {
        const deltaRes = await getNectarDelta(sample.hiveId, daysAgoIso(14), nowIso());
        setDeltas(deltaRes.data);
      } catch {

      }
    });

    hub.on("TelemetryAlert", (alert: TelemetryAlert) => {
      setAlerts((current) => [alert, ...current].slice(0, 8));
      addToast(alert.message, alert.severity === "Critical" ? "error" : "warning");
    });

    hub
      .start()
      .then(() => hub.invoke("JoinApiaryGroup", apiaryId))

    setConnection(hub);

    return () => {
      hub.invoke("LeaveApiaryGroup", apiaryId).finally(() => hub.stop());
    };
  }, [apiaryId, activeHiveId]);

  useEffect(() => {
    const loadTelemetry = async () => {
      if (!activeHiveId) return;

      try {
        const from = daysAgoIso(7);
        const to = nowIso();
        const [samplesRes, latestRes, deltaRes] = await Promise.allSettled([
          getTelemetryByHive(activeHiveId, from, to),
          getLatestTelemetry(activeHiveId),
          getNectarDelta(activeHiveId, daysAgoIso(14), to),
        ]);

        if (samplesRes.status === "fulfilled") setSamples(samplesRes.value.data);
        if (latestRes.status === "fulfilled") setLatest(latestRes.value.data);
        else setLatest(null);
        if (deltaRes.status === "fulfilled") setDeltas(deltaRes.value.data);
      } catch {
        addToast("Failed to load telemetry data.", "error");
      }
    };

    loadTelemetry();
  }, [activeHiveId]);

  const switchHive = async (hiveId: string) => {
    if (!apiaryId) return;

    if (connection?.state === "Connected") {
      await connection.invoke("LeaveApiaryGroup", apiaryId).catch(() => undefined);
      await connection.invoke("JoinApiaryGroup", apiaryId).catch(() => undefined);
    }

    setActiveHiveId(hiveId);
  };

  return (
    <PageLayout title={`Telemetry${apiary ? ` - ${apiary.name}` : ""}`}>
      <ToastContainer toasts={toasts} onRemove={removeToast} />

      <div className="flex items-center gap-3 mb-6">
        <button
          onClick={() => navigate(`/beekeeper/apiaries/${apiaryId}`)}
          className="text-gray-400 hover:text-gray-600 transition text-sm flex items-center gap-1"
        >
          ← Back to Apiary
        </button>
        <Badge label={connection?.state === "Connected" ? "Real-time connected" : "Manual refresh"} variant={connection?.state === "Connected" ? "green" : "yellow"} />
      </div>

      {loading ? (
        <Spinner />
      ) : hives.length === 0 ? (
        <Card className="p-8 text-center text-gray-500">This apiary has no hives yet.</Card>
      ) : (
        <>
          <div className="flex flex-wrap gap-2 mb-6">
            {hives.map((hive) => (
              <Button
                key={hive.id}
                variant={hive.id === activeHiveId ? "primary" : "secondary"}
                onClick={() => switchHive(hive.id)}
              >
                {hive.label}
              </Button>
            ))}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
            <StatusCard title="Weight" value={latest ? `${latest.weightKg.toFixed(1)} kg` : "—"} />
            <StatusCard title="Battery" value={latest ? `${latest.batteryPercentage}%` : "—"} danger={!!latest && latest.batteryPercentage < 15} />
            <StatusCard title="Temperature" value={latest ? `${latest.temperature.toFixed(1)} °C` : "—"} />
            <StatusCard title="Humidity" value={latest ? `${latest.humidity.toFixed(1)}%` : "—"} />
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-2 gap-6 mb-6">
            <Card className="p-5">
              <div className="mb-4">
                <h2 className="text-lg font-bold text-gray-800">Temperature and Humidity</h2>
                <p className="text-sm text-gray-500">Line chart for selected hive{activeHive ? ` (${activeHive.label})` : ""}</p>
              </div>
              <LineChart samples={samples} />
            </Card>

            <Card className="p-5">
              <div className="mb-4">
                <h2 className="text-lg font-bold text-gray-800">Daily nectar intake / consumption</h2>
                <p className="text-sm text-gray-500">Δ = Weight20h - Weight8h</p>
              </div>
              <BarChart deltas={deltas} />
            </Card>
          </div>

          <Card className="p-5">
            <div className="mb-4 flex items-center justify-between">
              <div>
                <h2 className="text-lg font-bold text-gray-800">Real-time alerts</h2>
                <p className="text-sm text-gray-500">Sudden weight drop and low battery warnings</p>
              </div>
              <Button variant="secondary" onClick={() => activeHiveId && setActiveHiveId(activeHiveId)}>Refresh</Button>
            </div>
            {alerts.length === 0 ? (
              <p className="text-sm text-gray-500">No alerts received during this session.</p>
            ) : (
              <div className="space-y-3">
                {alerts.map((alert, index) => (
                  <div key={`${alert.timestamp}-${index}`} className="border rounded-xl p-3 bg-red-50 border-red-100">
                    <div className="font-semibold text-red-700">{alert.type}</div>
                    <div className="text-sm text-red-700">{alert.message}</div>
                    <div className="text-xs text-red-400 mt-1">{new Date(alert.timestamp).toLocaleString()}</div>
                  </div>
                ))}
              </div>
            )}
          </Card>
        </>
      )}
    </PageLayout>
  );
}

function StatusCard({ title, value, danger = false }: { title: string; value: string; danger?: boolean }) {
  return (
    <Card className={`p-4 ${danger ? "border-red-200 bg-red-50" : ""}`}>
      <p className="text-xs uppercase tracking-wide text-gray-400 font-semibold">{title}</p>
      <p className={`text-2xl font-bold mt-1 ${danger ? "text-red-600" : "text-gray-800"}`}>{value}</p>
    </Card>
  );
}
