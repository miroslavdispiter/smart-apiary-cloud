import { useEffect } from "react";
import {
  MapContainer,
  TileLayer,
  Marker,
  useMapEvents,
  useMap,
} from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

export type LatLng = { lat: number; lng: number };

interface NearbyCulture {
  plotId: string;
  plotName: string;
  latitude: number;
  longitude: number;
  cultureType: number;
}

interface MapViewProps {
  position: LatLng | null;
  setPosition?: (pos: LatLng) => void;
  nearbyCultures?: NearbyCulture[];
  height?: string;
  readOnly?: boolean;
}

function LocationPicker({
  setPosition,
}: {
  setPosition?: (pos: LatLng) => void;
}) {
  useMapEvents({
    click: (e) => {
      if (setPosition) {
        setPosition({
          lat: e.latlng.lat,
          lng: e.latlng.lng,
        });
      }
    },
  });
  return null;
}

function MapUpdater({ position }: { position: LatLng | null }) {
  const map = useMap();

  useEffect(() => {
    if (position) {
      map.setView([position.lat, position.lng], 14);
    }
  }, [position, map]);

  return null;
}

const cultureIcons: Record<number, string> = {
  1: "🌻",
  2: "🌼",
  3: "💜",
  4: "🍀",
  5: "🌿",
  6: "🌽",
  7: "🌾",
};

export default function MapView({
  position,
  setPosition,
  nearbyCultures = [],
  height = "260px",
  readOnly = false,
}: MapViewProps) {
  return (
    <div className="rounded-xl overflow-hidden border border-gray-200 shadow-sm">
      <MapContainer
        center={
          position ? [position.lat, position.lng] : [44.8, 20.5]
        }
        zoom={position ? 13 : 7}
        style={{ height, width: "100%" }}
      >
        <TileLayer
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          attribution="© OpenStreetMap"
        />

        {/* ✅ Apiary / Plot marker */}
        {position && (
          <Marker position={[position.lat, position.lng]} />
        )}

        {/* ✅ Nearby culture markers */}
        {nearbyCultures.map((c) => (
          <Marker
            key={c.plotId}
            position={[c.latitude, c.longitude]}
            icon={L.divIcon({
              html: `<div style="font-size:22px">${cultureIcons[c.cultureType] ?? "🌱"}</div>`,
              className: "",
            })}
          />
        ))}

        {/* ✅ Editable mode */}
        {!readOnly && (
          <LocationPicker setPosition={setPosition} />
        )}

        <MapUpdater position={position} />
      </MapContainer>

      {!readOnly && (
        <p className="text-xs text-gray-400 text-center py-1.5 bg-gray-50 border-t border-gray-100">
          Click on the map to set location
        </p>
      )}
    </div>
  );
}