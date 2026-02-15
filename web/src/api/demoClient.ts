import type {
  Animal,
  CreateAnimal,
  UpdateAnimal,
  Photo,
  DoorEvent,
  DoorConfiguration,
  UpdateDoorConfiguration,
} from '../types';
import { mockAnimals, mockPhotos, mockDoorEvents, mockConfig } from './mockData';

const delay = (ms = 300) => new Promise(r => setTimeout(r, ms));

const demoError = (): never => {
  throw new Error('Demo mode: read-only preview. Mutations are disabled.');
};

// Animals
export const getAnimals = async (): Promise<Animal[]> => {
  await delay();
  return [...mockAnimals];
};

export const getAnimal = async (id: number): Promise<Animal> => {
  await delay();
  const animal = mockAnimals.find(a => a.id === id);
  if (!animal) throw new Error('Not found');
  return { ...animal };
};

export const createAnimal = async (_data: CreateAnimal): Promise<Animal> => demoError();

export const updateAnimal = async (_id: number, _data: UpdateAnimal): Promise<Animal> => demoError();

export const deleteAnimal = async (_id: number) => demoError();

// Photos
export const getAnimalPhotos = async (animalId: number): Promise<Photo[]> => {
  await delay();
  return mockPhotos[animalId] ?? [];
};

export const uploadPhoto = async (_animalId: number, _file: File): Promise<Photo> => demoError();

export const getPhotoUrl = (_photoId: number) => '';

export const deletePhoto = async (_id: number) => demoError();

// Door
export const getDoorStatus = async (): Promise<DoorConfiguration> => {
  await delay();
  return { ...mockConfig };
};

export const updateDoorConfig = async (_data: UpdateDoorConfiguration): Promise<DoorConfiguration> => demoError();

// Access Logs
export const getAccessLogs = async (
  page = 1,
  pageSize = 20,
  eventType?: string,
  direction?: string,
): Promise<DoorEvent[]> => {
  await delay();
  let filtered = [...mockDoorEvents];
  if (eventType) filtered = filtered.filter(e => e.eventType === eventType);
  if (direction) filtered = filtered.filter(e => e.direction === direction);
  const start = (page - 1) * pageSize;
  return filtered.slice(start, start + pageSize);
};

export const getAccessLog = async (id: number): Promise<DoorEvent> => {
  await delay();
  const event = mockDoorEvents.find(e => e.id === id);
  if (!event) throw new Error('Not found');
  return { ...event };
};
