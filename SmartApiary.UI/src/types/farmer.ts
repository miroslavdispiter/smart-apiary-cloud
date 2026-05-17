
export enum CultureType {
  Unknown = 0,
  Sunflower = 1,
  Rapeseed = 2,
  Lavender = 3,
  Clover = 4,
  Acacia = 5,
  Corn = 6,
  Wheat = 7,
  Other = 8,
}

export const CULTURE_TYPE_LABELS: Record<number, string> = {
  0: "Unknown",
  1: "Sunflower 🌻",
  2: "Rapeseed 🌼",
  3: "Lavender 💜",
  4: "Clover 🍀",
  5: "Acacia 🌿",
  6: "Corn 🌽",
  7: "Wheat 🌾",
  8: "Other",
};

export const CULTURE_ICONS: Record<number, string> = {
  0: "🌱",
  1: "🌻",
  2: "🌼",
  3: "💜",
  4: "🍀",
  5: "🌿",
  6: "🌽",
  7: "🌾",
  8: "🌱",
};

export enum SprayingStatus {
  Unknown = 0,
  Scheduled = 1,
  Completed = 2,
  Cancelled = 3,
}

export const SPRAYING_STATUS_LABELS: Record<number, string> = {
  0: "Unknown",
  1: "Scheduled",
  2: "Completed",
  3: "Cancelled",
};

export interface PlotCulture {
  id: string;
  type: CultureType;
  typeName: string;
  bloomingStart: string;
  bloomingEnd: string;
  additionalInfo?: string;
}

export interface Plot {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  createdAt: string;
  culture?: PlotCulture;
}

export interface SprayingSchedule {
  id: string;
  plotId: string;
  plotName: string;
  scheduledAt: string;
  durationHours: number;
  preparation?: string;
  status: SprayingStatus;
  statusName: string;
  notifiedBeekeeperCount: number;
  createdAt: string;
}
