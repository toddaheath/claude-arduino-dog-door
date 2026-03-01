import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import type { Geofence } from '../types';

interface GeofenceMapProps {
  geofences: Geofence[];
  height?: number;
}

const RULE_COLORS = {
  allow: '#4caf50',
  deny: '#ef5350',
};

function parseGeoJSON(boundaryJson: string): { type: string; coordinates?: number[][]; center?: number[]; radius?: number } | null {
  try {
    const parsed = JSON.parse(boundaryJson);
    return parsed;
  } catch {
    return null;
  }
}

export default function GeofenceMap({ geofences, height = 400 }: GeofenceMapProps) {
  const mapRef = useRef<HTMLDivElement>(null);
  const leafletMap = useRef<L.Map | null>(null);

  useEffect(() => {
    if (!mapRef.current) return;

    if (!leafletMap.current) {
      leafletMap.current = L.map(mapRef.current).setView([33.45, -112.07], 14);
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors',
        maxZoom: 19,
      }).addTo(leafletMap.current);
    }

    const map = leafletMap.current;

    // Clear existing overlays
    map.eachLayer(layer => {
      if (layer instanceof L.Polygon || layer instanceof L.Circle || layer instanceof L.Polyline) {
        map.removeLayer(layer);
      }
    });

    const allBounds: L.LatLng[] = [];
    const color = (rule: string) => RULE_COLORS[rule as keyof typeof RULE_COLORS] || '#4fc3f7';

    geofences.forEach(fence => {
      const geo = parseGeoJSON(fence.boundaryJson);
      if (!geo) return;

      const c = color(fence.rule);

      if (fence.fenceType === 'polygon' && geo.coordinates) {
        const latlngs = geo.coordinates.map(([lat, lng]) => L.latLng(lat, lng));
        L.polygon(latlngs, {
          color: c,
          fillColor: c,
          fillOpacity: 0.15,
          weight: 2,
        }).bindPopup(`<strong>${fence.name}</strong><br/>Rule: ${fence.rule}`).addTo(map);
        allBounds.push(...latlngs);
      } else if (fence.fenceType === 'circle' && geo.center && geo.radius) {
        const center = L.latLng(geo.center[0], geo.center[1]);
        L.circle(center, {
          radius: geo.radius,
          color: c,
          fillColor: c,
          fillOpacity: 0.15,
          weight: 2,
        }).bindPopup(`<strong>${fence.name}</strong><br/>Rule: ${fence.rule}<br/>Radius: ${geo.radius}m`).addTo(map);
        allBounds.push(center);
      } else if (fence.fenceType === 'corridor' && geo.coordinates) {
        const latlngs = geo.coordinates.map(([lat, lng]) => L.latLng(lat, lng));
        L.polyline(latlngs, {
          color: c,
          weight: 4,
          opacity: 0.8,
        }).bindPopup(`<strong>${fence.name}</strong><br/>Rule: ${fence.rule}`).addTo(map);
        allBounds.push(...latlngs);
      }
    });

    if (allBounds.length > 0) {
      map.fitBounds(L.latLngBounds(allBounds), { padding: [40, 40] });
    }
  }, [geofences]);

  useEffect(() => {
    return () => {
      if (leafletMap.current) {
        leafletMap.current.remove();
        leafletMap.current = null;
      }
    };
  }, []);

  return <div ref={mapRef} style={{ height, borderRadius: 8, overflow: 'hidden' }} />;
}
