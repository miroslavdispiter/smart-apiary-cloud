import api from "./axiosInstance";

export const registerDevice = (hiveId: string, serialNumber: string) => {
  return api.post("/Devices/register", { hiveId, serialNumber });
}