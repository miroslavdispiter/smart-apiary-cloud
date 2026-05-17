export interface AuthUser {
  token: string;
  userId: string;
  email: string;
  role: string;
}

export interface Apiary {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  description?: string;
  imageUrl?: string;
  thumbnailUrl?: string;
  hiveCount?: number;
}

export interface Hive {
  id: string;
  apiaryId: string;
  label: string;
  type: HiveType;
  boxColor?: string;
  queenAge?: number;
  note?: string;
  isPaired?: boolean;
  createdAt?: string;
}

export enum HiveType {
  Unknown = 0,
  LR = 1,
  DB = 2,
  Poloshka = 3,
  Dadant = 4,
  Other = 5,
}

export const HIVE_TYPE_LABELS: Record<number, string> = {
  0: "Unknown",
  1: "LR",
  2: "DB",
  3: "Poloshka",
  4: "Dadant",
  5: "Other",
};

export interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  role: string;
  status: string;
  createdAt: string;
}

export interface ApiResponse<T> {
  data: T;
  message?: string;
}

export interface HiveInspection {
  id: string;
  hiveId: string;
  inspectionDate: string;
  bottomBoardColor: string;
  honeyFrames: number;
  honeyKg: number;
  broodFrames: number;
  queenPresent: boolean;
  notes?: string;
  createdAt: string;
}

export interface UpcomingSprayingAlert {
  scheduleId: string;
  plotId: string;
  plotName: string;
  scheduledAt: string;
  durationHours: number;
  preparation?: string;
}

export interface NearbyCulture {
  plotId: string;
  plotName: string;
  latitude: number;
  longitude: number;
  cultureType: number;
  bloomingStart: string;
  bloomingEnd: string;
  additionalInfo?: string;
}