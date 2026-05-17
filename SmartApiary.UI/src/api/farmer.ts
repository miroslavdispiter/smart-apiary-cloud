import api from "./axiosInstance";
import type { CultureType } from "../types/farmer";

// ── Plots ─────────────────────────────────────────────────────────────────────
export const getPlots = () => api.get("/Plots");

export const createPlot = (data: { name: string; latitude: number; longitude: number }) =>
  api.post("/Plots", data);

export const updatePlot = (
  id: string,
  data: { name: string; latitude: number; longitude: number }
) => api.put(`/Plots/${id}`, data);

export const deletePlot = (id: string) => api.delete(`/Plots/${id}`);

// ── Culture ───────────────────────────────────────────────────────────────────
export const setCulture = (
  plotId: string,
  data: {
    type: CultureType;
    bloomingStart: string;
    bloomingEnd: string;
    additionalInfo?: string;
  }
) => api.post(`/Plots/${plotId}/culture`, data);

export const removeCulture = (plotId: string) => api.delete(`/Plots/${plotId}/culture`);

// ── Spraying ──────────────────────────────────────────────────────────────────
export const scheduleSpraying = (
  plotId: string,
  data: { scheduledAt: string; durationHours: number; preparation?: string }
) => api.post(`/Spraying/${plotId}`, data);

export const cancelSpraying = (plotId: string, scheduleId: string) =>
  api.delete(`/Spraying/${plotId}/${scheduleId}`);

export const rescheduleSpraying = (
  plotId: string,
  scheduleId: string,
  data: { newScheduledAt: string; newDurationHours: number }
) => api.put(`/Spraying/${plotId}/${scheduleId}/reschedule`, data);

export const getSprayingHistory = (params?: {
  plotId?: string;
  from?: string;
  to?: string;
  completedOnly?: boolean;
}) => api.get("/Spraying/history", { params });
