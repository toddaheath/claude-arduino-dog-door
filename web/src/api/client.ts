import axios from 'axios';
import type { AxiosError, InternalAxiosRequestConfig } from 'axios';
import type {
  Animal,
  CreateAnimal,
  UpdateAnimal,
  Photo,
  DoorEvent,
  DoorConfiguration,
  UpdateDoorConfiguration,
} from '../types';
import { authApi } from './auth';
import { getAccessToken, setAccessToken } from './tokenStore';

const api = axios.create({ baseURL: `${import.meta.env.VITE_API_URL || ''}/api/v1` });

api.interceptors.request.use(config => {
  const token = getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// --- Token refresh state (module-level to deduplicate concurrent 401s) ---
let isRefreshing = false;
let refreshPromise: Promise<string> | null = null;

function clearSessionAndRedirect(): void {
  setAccessToken(null);
  localStorage.removeItem('currentUser');
  window.location.replace('/login');
}

api.interceptors.response.use(
  response => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // Only attempt refresh on 401, and only once per request
    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    originalRequest._retry = true;

    // If a refresh is already in progress, wait for it instead of issuing another
    if (isRefreshing && refreshPromise) {
      try {
        const newAccessToken = await refreshPromise;
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
        return api(originalRequest);
      } catch {
        // Refresh failed; redirect already handled by the original refresh call
        return Promise.reject(error);
      }
    }

    // Start a new refresh
    isRefreshing = true;
    refreshPromise = authApi
      .refresh()
      .then(response => {
        setAccessToken(response.accessToken);
        localStorage.setItem('currentUser', JSON.stringify(response.user));
        return response.accessToken;
      })
      .catch(refreshError => {
        // Refresh failed — clear everything and redirect to login
        clearSessionAndRedirect();
        throw refreshError;
      })
      .finally(() => {
        isRefreshing = false;
        refreshPromise = null;
      });

    try {
      const newAccessToken = await refreshPromise;
      originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
      return api(originalRequest);
    } catch {
      return Promise.reject(error);
    }
  }
);

// Animals
const _getAnimals = () => api.get<Animal[]>('/animals').then(r => r.data);

const _getAnimal = (id: number) =>
  api.get<Animal>(`/animals/${id}`).then(r => r.data);

const _createAnimal = (data: CreateAnimal) =>
  api.post<Animal>('/animals', data).then(r => r.data);

const _updateAnimal = (id: number, data: UpdateAnimal) =>
  api.put<Animal>(`/animals/${id}`, data).then(r => r.data);

const _deleteAnimal = (id: number) => api.delete(`/animals/${id}`);

// Photos
const _getAnimalPhotos = (animalId: number) =>
  api.get<Photo[]>(`/photos/animal/${animalId}`).then(r => r.data);

const _uploadPhoto = (animalId: number, file: File) => {
  const form = new FormData();
  form.append('file', file);
  return api.post<Photo>(`/photos/upload/${animalId}`, form).then(r => r.data);
};

const _getPhotoUrl = (photoId: number) =>
  `${api.defaults.baseURL}/photos/${photoId}/file`;

const _deletePhoto = (id: number) => api.delete(`/photos/${id}`);

// Door
const _getDoorStatus = () =>
  api.get<DoorConfiguration>('/doors/status').then(r => r.data);

const _updateDoorConfig = (data: UpdateDoorConfiguration) =>
  api.put<DoorConfiguration>('/doors/configuration', data).then(r => r.data);

// Access Logs
const _getAccessLogs = (page = 1, pageSize = 20, eventType?: string, direction?: string) => {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (eventType) params.set('eventType', eventType);
  if (direction) params.set('direction', direction);
  return api.get<DoorEvent[]>(`/accesslogs?${params}`).then(r => r.data);
};

const _getAccessLog = (id: number) =>
  api.get<DoorEvent>(`/accesslogs/${id}`).then(r => r.data);

// Conditionally export demo or real client
import * as demo from './demoClient';

const isDemo = import.meta.env.VITE_DEMO_MODE === 'true';

export const getAnimals = isDemo ? demo.getAnimals : _getAnimals;
export const getAnimal = isDemo ? demo.getAnimal : _getAnimal;
export const createAnimal = isDemo ? demo.createAnimal : _createAnimal;
export const updateAnimal = isDemo ? demo.updateAnimal : _updateAnimal;
export const deleteAnimal = isDemo ? demo.deleteAnimal : _deleteAnimal;
export const getAnimalPhotos = isDemo ? demo.getAnimalPhotos : _getAnimalPhotos;
export const uploadPhoto = isDemo ? demo.uploadPhoto : _uploadPhoto;
export const getPhotoUrl = isDemo ? demo.getPhotoUrl : _getPhotoUrl;
export const deletePhoto = isDemo ? demo.deletePhoto : _deletePhoto;
export const getDoorStatus = isDemo ? demo.getDoorStatus : _getDoorStatus;
export const updateDoorConfig = isDemo ? demo.updateDoorConfig : _updateDoorConfig;
export const getAccessLogs = isDemo ? demo.getAccessLogs : _getAccessLogs;
export const getAccessLog = isDemo ? demo.getAccessLog : _getAccessLog;
