import type { Animal, Photo, DoorEvent, DoorConfiguration } from '../types';

export const mockAnimals: Animal[] = [
  { id: 1, name: 'Buddy', breed: 'Golden Retriever', isAllowed: true, createdAt: '2025-06-01T10:00:00Z', updatedAt: null, photoCount: 3 },
  { id: 2, name: 'Luna', breed: 'German Shepherd', isAllowed: true, createdAt: '2025-06-05T14:30:00Z', updatedAt: null, photoCount: 2 },
  { id: 3, name: 'Stray Cat', breed: null, isAllowed: false, createdAt: '2025-07-10T08:00:00Z', updatedAt: '2025-07-10T09:00:00Z', photoCount: 1 },
];

export const mockPhotos: Record<number, Photo[]> = {
  1: [
    { id: 1, animalId: 1, fileName: 'buddy_1.jpg', fileSize: 150000, uploadedAt: '2025-06-01T10:05:00Z' },
    { id: 2, animalId: 1, fileName: 'buddy_2.jpg', fileSize: 180000, uploadedAt: '2025-06-01T10:06:00Z' },
  ],
  2: [
    { id: 3, animalId: 2, fileName: 'luna_1.jpg', fileSize: 200000, uploadedAt: '2025-06-05T14:35:00Z' },
  ],
  3: [],
};

const now = new Date();
const hoursAgo = (h: number) => new Date(now.getTime() - h * 3600000).toISOString();

export const mockDoorEvents: DoorEvent[] = [
  { id: 1, animalId: 1, animalName: 'Buddy', eventType: 'ExitGranted', confidenceScore: 0.92, notes: null, timestamp: hoursAgo(1), side: 'Inside', direction: 'Exiting' },
  { id: 2, animalId: 2, animalName: 'Luna', eventType: 'EntryGranted', confidenceScore: 0.88, notes: null, timestamp: hoursAgo(2), side: 'Outside', direction: 'Entering' },
  { id: 3, animalId: null, animalName: null, eventType: 'UnknownAnimal', confidenceScore: 0.25, notes: 'Animal not recognized', timestamp: hoursAgo(3), side: 'Outside', direction: 'Entering' },
  { id: 4, animalId: 1, animalName: 'Buddy', eventType: 'EntryGranted', confidenceScore: 0.95, notes: null, timestamp: hoursAgo(4), side: 'Outside', direction: 'Entering' },
  { id: 5, animalId: 3, animalName: 'Stray Cat', eventType: 'EntryDenied', confidenceScore: 0.82, notes: 'Animal not allowed', timestamp: hoursAgo(5), side: 'Outside', direction: 'Entering' },
  { id: 6, animalId: 2, animalName: 'Luna', eventType: 'ExitGranted', confidenceScore: 0.91, notes: null, timestamp: hoursAgo(6), side: 'Inside', direction: 'Exiting' },
  { id: 7, animalId: 1, animalName: 'Buddy', eventType: 'AccessGranted', confidenceScore: 0.89, notes: null, timestamp: hoursAgo(12), side: null, direction: null },
  { id: 8, animalId: null, animalName: null, eventType: 'ManualOverride', confidenceScore: null, notes: 'Manual door open', timestamp: hoursAgo(18), side: null, direction: null },
  { id: 9, animalId: 2, animalName: 'Luna', eventType: 'ExitGranted', confidenceScore: 0.87, notes: null, timestamp: hoursAgo(20), side: 'Inside', direction: 'Exiting' },
  { id: 10, animalId: 1, animalName: 'Buddy', eventType: 'EntryGranted', confidenceScore: 0.93, notes: null, timestamp: hoursAgo(22), side: 'Outside', direction: 'Entering' },
];

export const mockConfig: DoorConfiguration = {
  isEnabled: true,
  autoCloseEnabled: true,
  autoCloseDelaySeconds: 10,
  minConfidenceThreshold: 0.7,
  nightModeEnabled: false,
  nightModeStart: null,
  nightModeEnd: null,
};
