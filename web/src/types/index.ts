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
