export interface Animal {
  id: number;
  name: string;
  breed: string | null;
  isAllowed: boolean;
  createdAt: string;
  updatedAt: string | null;
  photoCount: number;
}

export interface CreateAnimal {
  name: string;
  breed?: string;
  isAllowed?: boolean;
}

export interface UpdateAnimal {
  name?: string;
  breed?: string;
  isAllowed?: boolean;
}

export interface Photo {
  id: number;
  animalId: number;
  fileName: string | null;
  fileSize: number;
  uploadedAt: string;
}

export interface DoorEvent {
  id: number;
  animalId: number | null;
  animalName: string | null;
  eventType: string;
  confidenceScore: number | null;
  notes: string | null;
  timestamp: string;
  side: string | null;
  direction: string | null;
  imageUrl: string | null;
}

export interface DoorConfiguration {
  isEnabled: boolean;
  autoCloseEnabled: boolean;
  autoCloseDelaySeconds: number;
  minConfidenceThreshold: number;
  nightModeEnabled: boolean;
  nightModeStart: string | null;
  nightModeEnd: string | null;
}

export interface UpdateDoorConfiguration {
  isEnabled?: boolean;
  autoCloseEnabled?: boolean;
  autoCloseDelaySeconds?: number;
  minConfidenceThreshold?: number;
  nightModeEnabled?: boolean;
  nightModeStart?: string;
  nightModeEnd?: string;
}

export interface AccessResponse {
  allowed: boolean;
  animalId: number | null;
  animalName: string | null;
  confidenceScore: number | null;
  reason: string | null;
  direction: string | null;
}

// Auth
export interface UserSummary {
  id: number;
  email: string;
  firstName: string | null;
  lastName: string | null;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: UserSummary;
}

export interface UserProfile {
  id: number;
  email: string;
  firstName: string | null;
  lastName: string | null;
  phone: string | null;
  mobilePhone: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  state: string | null;
  postalCode: string | null;
  country: string | null;
  emailVerified: boolean;
  createdAt: string;
}

export interface Guest {
  userId: number;
  email: string;
  firstName: string | null;
  lastName: string | null;
  invitedAt: string;
  acceptedAt: string | null;
}

export interface Invitation {
  id: number;
  inviteeEmail: string;
  createdAt: string;
  expiresAt: string;
  isAccepted: boolean;
}

export interface NotificationPreferences {
  emailEnabled: boolean;
  smsEnabled: boolean;
  animalApproachInside: boolean;
  animalApproachOutside: boolean;
  unknownAnimalInside: boolean;
  unknownAnimalOutside: boolean;
  doorOpened: boolean;
  doorClosed: boolean;
  doorFailedOpen: boolean;
  doorFailedClose: boolean;
  powerDisconnected: boolean;
  powerRestored: boolean;
  batteryLow: boolean;
  batteryCharged: boolean;
  geofenceBreach: boolean;
  geofenceEnteredExited: boolean;
  collarBatteryLow: boolean;
  collarDisconnected: boolean;
}

export type UpdateNotificationPreferences = Partial<NotificationPreferences>;

// ── Collar Types ────────────────────────────────────────────

export interface CollarDevice {
  id: number;
  collarId: string;
  name: string;
  animalId: number | null;
  animalName: string | null;
  firmwareVersion: string | null;
  batteryPercent: number | null;
  batteryVoltage: number | null;
  lastSeenAt: string | null;
  lastLatitude: number | null;
  lastLongitude: number | null;
  lastAccuracy: number | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateCollarDevice {
  name: string;
  animalId?: number;
}

export interface UpdateCollarDevice {
  name?: string;
  animalId?: number;
  isActive?: boolean;
}

export interface CollarPairingResult {
  id: number;
  collarId: string;
  sharedSecret: string;
  name: string;
}

export interface LocationPoint {
  latitude: number;
  longitude: number;
  altitude: number | null;
  accuracy: number | null;
  speed: number | null;
  heading: number | null;
  satellites: number | null;
  timestamp: string;
}

export interface CurrentLocation {
  latitude: number;
  longitude: number;
  accuracy: number | null;
  speed: number | null;
  timestamp: string;
  batteryPercent: number | null;
  activityState: string | null;
}

export interface Geofence {
  id: number;
  name: string;
  fenceType: string;
  rule: string;
  boundaryJson: string;
  buzzerPattern: number;
  isActive: boolean;
  version: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateGeofence {
  name: string;
  fenceType: string;
  rule: string;
  boundaryJson: string;
  buzzerPattern?: number;
}

export interface UpdateGeofence {
  name?: string;
  rule?: string;
  boundaryJson?: string;
  buzzerPattern?: number;
  isActive?: boolean;
}

export interface GeofenceEvent {
  id: number;
  geofenceId: number;
  geofenceName: string;
  collarDeviceId: number;
  collarName: string;
  eventType: string;
  latitude: number;
  longitude: number;
  timestamp: string;
}

export interface ActivitySummary {
  totalDistanceMeters: number;
  activeMinutes: number;
  maxSpeedMps: number;
  avgSpeedMps: number;
  locationPointCount: number;
  periodStart: string;
  periodEnd: string;
}

export interface FirmwareRelease {
  id: number;
  version: string;
  fileSize: number;
  sha256Hash: string | null;
  releaseNotes: string | null;
  isActive: boolean;
  createdAt: string;
}
