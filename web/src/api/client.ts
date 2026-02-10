import axios from 'axios';
import type {
  Animal,
  CreateAnimal,
  UpdateAnimal,
  Photo,
  DoorEvent,
  DoorConfiguration,
  UpdateDoorConfiguration,
} from '../types';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5001/api',
});

// Animals
export const getAnimals = () => api.get<Animal[]>('/animals').then(r => r.data);

export const getAnimal = (id: number) =>
  api.get<Animal>(`/animals/${id}`).then(r => r.data);

export const createAnimal = (data: CreateAnimal) =>
  api.post<Animal>('/animals', data).then(r => r.data);

export const updateAnimal = (id: number, data: UpdateAnimal) =>
  api.put<Animal>(`/animals/${id}`, data).then(r => r.data);

export const deleteAnimal = (id: number) => api.delete(`/animals/${id}`);

// Photos
export const getAnimalPhotos = (animalId: number) =>
  api.get<Photo[]>(`/photos/animal/${animalId}`).then(r => r.data);

export const uploadPhoto = (animalId: number, file: File) => {
  const form = new FormData();
  form.append('file', file);
  return api.post<Photo>(`/photos/upload/${animalId}`, form).then(r => r.data);
};

export const getPhotoUrl = (photoId: number) =>
  `${api.defaults.baseURL}/photos/${photoId}/file`;

export const deletePhoto = (id: number) => api.delete(`/photos/${id}`);

// Door
export const getDoorStatus = () =>
  api.get<DoorConfiguration>('/doors/status').then(r => r.data);

export const updateDoorConfig = (data: UpdateDoorConfiguration) =>
  api.put<DoorConfiguration>('/doors/configuration', data).then(r => r.data);

// Access Logs
export const getAccessLogs = (page = 1, pageSize = 20, eventType?: string) => {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (eventType) params.set('eventType', eventType);
  return api.get<DoorEvent[]>(`/accesslogs?${params}`).then(r => r.data);
};

export const getAccessLog = (id: number) =>
  api.get<DoorEvent>(`/accesslogs/${id}`).then(r => r.data);
