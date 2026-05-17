import api from "./axiosInstance";

export const getApiaries = () => api.get("/Apiaries");

export const getApiaryById = (id: string) =>
  api.get(`/Apiaries/${id}`);

export const createApiary = (data: {
  name: string;
  latitude: number;
  longitude: number;
  description?: string;
}) => api.post("/Apiaries", data);

export const updateApiary = (
  id: string,
  data: {
    name: string;
    latitude: number;
    longitude: number;
    description?: string;
  }
) => api.put(`/Apiaries/${id}`, data);

export const deleteApiary = (id: string) =>
  api.delete(`/Apiaries/${id}`);

export const uploadApiaryImage = (id: string, file: File) => {
  const form = new FormData();
  form.append("image", file);

  return api.post(`/Apiaries/${id}/image`, form, {
    headers: { "Content-Type": "multipart/form-data" },
  });
};

export const getUpcomingSprayings = (apiaryId: string) =>
  api.get(`/Apiaries/${apiaryId}/upcoming-sprayings`);

export const getNearbyCultures = (apiaryId: string) =>
  api.get(`/Apiaries/${apiaryId}/nearby-cultures`);