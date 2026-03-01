import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import type { LocationPoint, CurrentLocation, Geofence } from '../types';

// Fix Leaflet default marker icon paths (broken by bundlers)
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

interface CollarMapProps {
  currentLocation?: CurrentLocation | null;
  history?: LocationPoint[];
  geofences?: Geofence[];
  height?: number;
}

const FENCE_COLORS = { allow: '#4caf50', deny: '#ef5350' };

export default function CollarMap({ currentLocation, history, geofences, height = 350 }: CollarMapProps) {
  const mapRef = useRef<HTMLDivElement>(null);
  const leafletMap = useRef<L.Map | null>(null);

  useEffect(() => {
    if (!mapRef.current) return;

    // Determine center from current location or first history point
    const center: [number, number] = currentLocation
      ? [currentLocation.latitude, currentLocation.longitude]
      : history && history.length > 0
        ? [history[0].latitude, history[0].longitude]
        : [33.45, -112.07]; // Default: Phoenix, AZ

    if (!leafletMap.current) {
      leafletMap.current = L.map(mapRef.current).setView(center, 16);
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors',
        maxZoom: 19,
      }).addTo(leafletMap.current);
    } else {
      leafletMap.current.setView(center, leafletMap.current.getZoom());
    }

    const map = leafletMap.current;

    // Clear existing layers (except tile layer)
    map.eachLayer(layer => {
      if (layer instanceof L.Marker || layer instanceof L.Polyline ||
          layer instanceof L.CircleMarker || layer instanceof L.Polygon ||
          layer instanceof L.Circle) {
        map.removeLayer(layer);
      }
    });

    // Draw geofence boundaries (under the trail)
    if (geofences) {
      geofences.forEach(fence => {
        try {
          const geo = JSON.parse(fence.boundaryJson);
          if (!geo.type) return;
          const c = FENCE_COLORS[fence.rule as keyof typeof FENCE_COLORS] || '#4fc3f7';
          const opts = { color: c, fillColor: c, fillOpacity: 0.1, weight: 2, dashArray: '6 4' };

          if (geo.type === 'Polygon' && geo.coordinates) {
            const latlngs = geo.coordinates.map(([lat, lng]: number[]) => L.latLng(lat, lng));
            L.polygon(latlngs, opts)
              .bindPopup(`<strong>${fence.name}</strong><br/>Rule: ${fence.rule}`)
              .addTo(map);
          } else if (geo.type === 'Circle' && geo.center && geo.radius) {
            L.circle(L.latLng(geo.center[0], geo.center[1]), { ...opts, radius: geo.radius })
              .bindPopup(`<strong>${fence.name}</strong><br/>Rule: ${fence.rule}<br/>Radius: ${geo.radius}m`)
              .addTo(map);
          } else if (geo.type === 'Corridor' && geo.coordinates) {
            const latlngs = geo.coordinates.map(([lat, lng]: number[]) => L.latLng(lat, lng));
            L.polyline(latlngs, { color: c, weight: 4, opacity: 0.6, dashArray: '8 6' })
              .bindPopup(`<strong>${fence.name}</strong><br/>Rule: ${fence.rule}`)
              .addTo(map);
          }
        } catch { /* invalid boundary JSON */ }
      });
    }

    // Draw history trail
    if (history && history.length > 1) {
      const trail = history.map(pt => [pt.latitude, pt.longitude] as [number, number]);
      L.polyline(trail, { color: '#4fc3f7', weight: 3, opacity: 0.7 }).addTo(map);

      // Small dots for each history point
      history.forEach(pt => {
        L.circleMarker([pt.latitude, pt.longitude], {
          radius: 3,
          fillColor: '#4fc3f7',
          color: '#2196f3',
          weight: 1,
          fillOpacity: 0.8,
        }).bindPopup(
          `<strong>${new Date(pt.timestamp).toLocaleTimeString()}</strong><br/>` +
          `Accuracy: ${pt.accuracy?.toFixed(1) ?? '--'}m<br/>` +
          `Speed: ${pt.speed?.toFixed(1) ?? '--'} m/s`
        ).addTo(map);
      });
    }

    // Current location marker
    if (currentLocation) {
      L.marker([currentLocation.latitude, currentLocation.longitude])
        .bindPopup(
          `<strong>Current Location</strong><br/>` +
          `${currentLocation.latitude.toFixed(6)}, ${currentLocation.longitude.toFixed(6)}<br/>` +
          `Accuracy: ${currentLocation.accuracy?.toFixed(1) ?? '--'}m<br/>` +
          `Updated: ${new Date(currentLocation.timestamp).toLocaleString()}`
        )
        .addTo(map);

      // Accuracy circle
      if (currentLocation.accuracy) {
        L.circle([currentLocation.latitude, currentLocation.longitude], {
          radius: currentLocation.accuracy,
          color: '#4fc3f7',
          fillColor: '#4fc3f7',
          fillOpacity: 0.1,
          weight: 1,
        }).addTo(map);
      }
    }

    // Fit bounds to all points
    const allPoints: [number, number][] = [];
    if (currentLocation) allPoints.push([currentLocation.latitude, currentLocation.longitude]);
    if (history) history.forEach(pt => allPoints.push([pt.latitude, pt.longitude]));
    if (allPoints.length > 1) {
      map.fitBounds(L.latLngBounds(allPoints), { padding: [30, 30] });
    }
  }, [currentLocation, history, geofences]);

  // Cleanup on unmount
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
