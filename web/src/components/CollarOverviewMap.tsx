import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import type { CollarDevice } from '../types';

// Fix Leaflet default marker icon paths (broken by bundlers)
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

interface CollarOverviewMapProps {
  collars: CollarDevice[];
  height?: number;
}

const batteryColor = (pct: number | null) => {
  if (pct == null) return '#999';
  if (pct > 50) return '#4caf50';
  if (pct > 15) return '#ff9800';
  return '#ef5350';
};

export default function CollarOverviewMap({ collars, height = 300 }: CollarOverviewMapProps) {
  const mapRef = useRef<HTMLDivElement>(null);
  const leafletMap = useRef<L.Map | null>(null);

  const collarsWithLocation = collars.filter(c => c.lastLatitude != null && c.lastLongitude != null);

  useEffect(() => {
    if (!mapRef.current || collarsWithLocation.length === 0) return;

    if (!leafletMap.current) {
      leafletMap.current = L.map(mapRef.current).setView(
        [collarsWithLocation[0].lastLatitude!, collarsWithLocation[0].lastLongitude!], 14
      );
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors',
        maxZoom: 19,
      }).addTo(leafletMap.current);
    }

    const map = leafletMap.current;

    // Clear existing markers
    map.eachLayer(layer => {
      if (layer instanceof L.Marker || layer instanceof L.Circle) {
        map.removeLayer(layer);
      }
    });

    const bounds: [number, number][] = [];

    collarsWithLocation.forEach(collar => {
      const lat = collar.lastLatitude!;
      const lng = collar.lastLongitude!;
      bounds.push([lat, lng]);

      L.marker([lat, lng])
        .bindPopup(
          `<strong>${collar.name}</strong>` +
          (collar.animalName ? `<br/>${collar.animalName}` : '') +
          `<br/>Battery: <span style="color:${batteryColor(collar.batteryPercent)}">${collar.batteryPercent != null ? Math.round(collar.batteryPercent) + '%' : '--'}</span>` +
          (collar.lastSeenAt ? `<br/>Last seen: ${new Date(collar.lastSeenAt).toLocaleString()}` : '') +
          `<br/><a href="#/collars/${collar.id}">View details</a>`
        )
        .addTo(map);

      // Accuracy circle if available
      if (collar.lastAccuracy) {
        L.circle([lat, lng], {
          radius: collar.lastAccuracy,
          color: collar.isActive ? '#4fc3f7' : '#999',
          fillColor: collar.isActive ? '#4fc3f7' : '#999',
          fillOpacity: 0.08,
          weight: 1,
        }).addTo(map);
      }
    });

    if (bounds.length > 1) {
      map.fitBounds(L.latLngBounds(bounds), { padding: [40, 40] });
    } else if (bounds.length === 1) {
      map.setView(bounds[0], 16);
    }
  }, [collarsWithLocation]);

  useEffect(() => {
    return () => {
      if (leafletMap.current) {
        leafletMap.current.remove();
        leafletMap.current = null;
      }
    };
  }, []);

  if (collarsWithLocation.length === 0) return null;

  return <div ref={mapRef} style={{ height, borderRadius: 8, overflow: 'hidden' }} />;
}
