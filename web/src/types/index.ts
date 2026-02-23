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
  refreshToken: string;
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
}

export type UpdateNotificationPreferences = Partial<NotificationPreferences>;
