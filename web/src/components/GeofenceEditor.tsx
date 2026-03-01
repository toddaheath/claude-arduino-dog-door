import { useEffect, useRef, useCallback } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import 'leaflet-draw';
import 'leaflet-draw/dist/leaflet.draw.css';

interface GeofenceEditorProps {
  fenceType: 'polygon' | 'circle' | 'corridor';
  initialBoundary?: string;
  onChange: (boundaryJson: string) => void;
  height?: number;
}

// Fix Leaflet default marker icon paths (broken by bundlers)
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

export default function GeofenceEditor({ fenceType, initialBoundary, onChange, height = 400 }: GeofenceEditorProps) {
  const mapRef = useRef<HTMLDivElement>(null);
  const leafletMap = useRef<L.Map | null>(null);
  const drawnItems = useRef<L.FeatureGroup>(new L.FeatureGroup());
  const drawControl = useRef<L.Control.Draw | null>(null);

  const emitBoundary = useCallback((layer: L.Layer) => {
    if (layer instanceof L.Circle) {
      const center = layer.getLatLng();
      onChange(JSON.stringify({
        type: 'Circle',
        center: [center.lat, center.lng],
        radius: Math.round(layer.getRadius()),
      }));
    } else if (layer instanceof L.Polygon) {
      const latlngs = (layer.getLatLngs()[0] as L.LatLng[]);
      onChange(JSON.stringify({
        type: 'Polygon',
        coordinates: latlngs.map(ll => [ll.lat, ll.lng]),
      }));
    } else if (layer instanceof L.Polyline) {
      const latlngs = layer.getLatLngs() as L.LatLng[];
      onChange(JSON.stringify({
        type: 'Corridor',
        coordinates: latlngs.map(ll => [ll.lat, ll.lng]),
      }));
    }
  }, [onChange]);

  useEffect(() => {
    if (!mapRef.current) return;

    if (!leafletMap.current) {
      leafletMap.current = L.map(mapRef.current).setView([33.45, -112.07], 16);

      // Satellite tile layer
      L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
        attribution: 'Tiles &copy; Esri',
        maxZoom: 20,
      }).addTo(leafletMap.current);

      // Street name overlay for readability
      L.tileLayer('https://stamen-tiles.a.ssl.fastly.net/toner-labels/{z}/{x}/{y}.png', {
        maxZoom: 20,
        opacity: 0.7,
      }).addTo(leafletMap.current);

      drawnItems.current.addTo(leafletMap.current);

      // Try to get user's location for a useful default view
      if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
          (pos) => {
            leafletMap.current?.setView([pos.coords.latitude, pos.coords.longitude], 18);
          },
          () => { /* ignore errors, keep default view */ }
        );
      }
    }

    return () => {
      // Cleanup only on unmount
    };
  }, []);

  // Load initial boundary data
  useEffect(() => {
    if (!leafletMap.current || !initialBoundary) return;

    try {
      const geo = JSON.parse(initialBoundary);
      if (!geo.type || geo.type === '{}') return;

      drawnItems.current.clearLayers();

      if (geo.type === 'Circle' && geo.center && geo.radius) {
        const circle = L.circle(L.latLng(geo.center[0], geo.center[1]), { radius: geo.radius });
        drawnItems.current.addLayer(circle);
        leafletMap.current.setView(L.latLng(geo.center[0], geo.center[1]), 17);
      } else if (geo.type === 'Polygon' && geo.coordinates) {
        const latlngs = geo.coordinates.map(([lat, lng]: number[]) => L.latLng(lat, lng));
        const polygon = L.polygon(latlngs);
        drawnItems.current.addLayer(polygon);
        leafletMap.current.fitBounds(L.latLngBounds(latlngs), { padding: [40, 40] });
      } else if (geo.type === 'Corridor' && geo.coordinates) {
        const latlngs = geo.coordinates.map(([lat, lng]: number[]) => L.latLng(lat, lng));
        const polyline = L.polyline(latlngs);
        drawnItems.current.addLayer(polyline);
        leafletMap.current.fitBounds(L.latLngBounds(latlngs), { padding: [40, 40] });
      }
    } catch {
      // Invalid JSON, ignore
    }
  }, [initialBoundary]);

  // Update draw controls when fence type changes
  useEffect(() => {
    const map = leafletMap.current;
    if (!map) return;

    // Remove existing draw control
    if (drawControl.current) {
      map.removeControl(drawControl.current);
    }

    // Configure draw options based on fence type
    const drawOptions: L.Control.DrawConstructorOptions = {
      position: 'topright',
      draw: {
        polygon: fenceType === 'polygon' ? {
          shapeOptions: { color: '#4caf50', fillOpacity: 0.2 },
        } : false,
        circle: fenceType === 'circle' ? {
          shapeOptions: { color: '#ef5350', fillOpacity: 0.2 },
        } : false,
        polyline: fenceType === 'corridor' ? {
          shapeOptions: { color: '#ff9800' },
        } : false,
        rectangle: false,
        circlemarker: false,
        marker: false,
      },
      edit: {
        featureGroup: drawnItems.current,
        remove: true,
      },
    };

    drawControl.current = new L.Control.Draw(drawOptions);
    map.addControl(drawControl.current);

    // Handle draw created
    const onCreated = (e: L.LeafletEvent) => {
      const event = e as L.DrawEvents.Created;
      // Only one shape at a time — clear previous
      drawnItems.current.clearLayers();
      drawnItems.current.addLayer(event.layer);
      emitBoundary(event.layer);
    };

    const onEdited = (e: L.LeafletEvent) => {
      const event = e as L.DrawEvents.Edited;
      event.layers.eachLayer((layer) => {
        emitBoundary(layer);
      });
    };

    const onDeleted = () => {
      onChange('{}');
    };

    map.on(L.Draw.Event.CREATED, onCreated);
    map.on(L.Draw.Event.EDITED, onEdited);
    map.on(L.Draw.Event.DELETED, onDeleted);

    return () => {
      map.off(L.Draw.Event.CREATED, onCreated);
      map.off(L.Draw.Event.EDITED, onEdited);
      map.off(L.Draw.Event.DELETED, onDeleted);
    };
  }, [fenceType, emitBoundary, onChange]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (leafletMap.current) {
        leafletMap.current.remove();
        leafletMap.current = null;
      }
    };
  }, []);

  return (
    <div>
      <div ref={mapRef} style={{ height, borderRadius: 8, overflow: 'hidden' }} />
      <p style={{ color: 'var(--text-muted)', fontSize: '0.8rem', marginTop: '0.5rem' }}>
        {fenceType === 'polygon' && 'Click the polygon tool (top-right) and click points to draw a boundary. Double-click to finish.'}
        {fenceType === 'circle' && 'Click the circle tool (top-right) and drag from center to draw. Drag edges to resize.'}
        {fenceType === 'corridor' && 'Click the line tool (top-right) and click points to draw a path. Double-click to finish.'}
      </p>
    </div>
  );
}
