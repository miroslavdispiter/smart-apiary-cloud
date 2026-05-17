import api from "./axiosInstance";

export const getHivesByApiary = (apiaryId: string) =>
  api.get(`/Hives?apiaryId=${apiaryId}`);

export const getHiveById = (id: string) => api.get(`/Hives/${id}`);

export const createHive = (data: {
  apiaryId: string;
  label: string;
  type: string;
  boxColor?: string;
  queenAge?: number;
  note?: string;
}) => api.post("/Hives", data);

export const updateHive = (
  id: string,
  data: {
    label: string;
    type: string;
    boxColor?: string;
    queenAge?: number;
    note?: string;
  }
) => api.put(`/Hives/${id}`, data);

export const deleteHive = (id: string) => api.delete(`/Hives/${id}`);
