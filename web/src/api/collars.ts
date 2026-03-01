import axios from 'axios';
import type {
  CollarDevice,
  CreateCollarDevice,
  UpdateCollarDevice,
  CollarPairingResult,
  LocationPoint,
  CurrentLocation,
  ActivitySummary,
  Geofence,
  CreateGeofence,
  UpdateGeofence,
  GeofenceEvent,
  FirmwareRelease,
} from '../types';
import { getAccessToken } from './tokenStore';

const api = axios.create({ baseURL: `${import.meta.env.VITE_API_URL || ''}/api/v1` });

api.interceptors.request.use(config => {
  const token = getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ── Collar Devices ──────────────────────────────────────────

export const getCollars = () =>
  api.get<CollarDevice[]>('/collars').then(r => r.data);

export const getCollar = (id: number) =>
  api.get<CollarDevice>(`/collars/${id}`).then(r => r.data);

export const registerCollar = (data: CreateCollarDevice) =>
  api.post<CollarPairingResult>('/collars', data).then(r => r.data);

export const updateCollar = (id: number, data: UpdateCollarDevice) =>
  api.put<CollarDevice>(`/collars/${id}`, data).then(r => r.data);

export const deleteCollar = (id: number) =>
  api.delete(`/collars/${id}`);

// ── Location ────────────────────────────────────────────────

export const getLocationHistory = (collarId: number, from?: string, to?: string) => {
  const params = new URLSearchParams();
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  return api.get<LocationPoint[]>(`/collars/${collarId}/locations?${params}`).then(r => r.data);
};

export const getCurrentLocation = (collarId: number) =>
  api.get<CurrentLocation>(`/collars/${collarId}/location/current`).then(r => r.data);

// ── Geofences ───────────────────────────────────────────────

export const getGeofences = () =>
  api.get<Geofence[]>('/geofences').then(r => r.data);

export const getGeofence = (id: number) =>
  api.get<Geofence>(`/geofences/${id}`).then(r => r.data);

export const createGeofence = (data: CreateGeofence) =>
  api.post<Geofence>('/geofences', data).then(r => r.data);

export const updateGeofence = (id: number, data: UpdateGeofence) =>
  api.put<Geofence>(`/geofences/${id}`, data).then(r => r.data);

export const deleteGeofence = (id: number) =>
  api.delete(`/geofences/${id}`);

// ── Geofence Events ─────────────────────────────────────────

export const getGeofenceEvents = (geofenceId?: number, from?: string, to?: string) => {
  const params = new URLSearchParams();
  if (geofenceId) params.set('geofenceId', String(geofenceId));
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  return api.get<GeofenceEvent[]>(`/geofences/events?${params}`).then(r => r.data);
};

// ── Activity ──────────────────────────────────────────────

export const getActivitySummary = (collarId: number, from?: string, to?: string) => {
  const params = new URLSearchParams();
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  return api.get<ActivitySummary>(`/collars/${collarId}/activity?${params}`).then(r => r.data);
};

// ── Firmware ───────────────────────────────────────────────

export const getFirmwareReleases = () =>
  api.get<FirmwareRelease[]>('/collars/firmware').then(r => r.data);

export const uploadFirmware = (version: string, releaseNotes: string, file: File) => {
  const form = new FormData();
  form.append('version', version);
  form.append('releaseNotes', releaseNotes);
  form.append('file', file);
  return api.post<FirmwareRelease>('/collars/firmware', form).then(r => r.data);
};
