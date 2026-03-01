import { useState } from 'react';
import { useToast } from '../contexts/ToastContext';

const BLE_SERVICE_UUID = '5a6d0001-8e7f-4b3c-9d2a-1c6e3f8b7d4e';
const BLE_CHAR_CONFIG_UUID = '5a6d0007-8e7f-4b3c-9d2a-1c6e3f8b7d4e';

interface CollarProvisionProps {
  collarId: string;
  sharedSecret: string;
  onComplete: () => void;
}

type ProvisionStep = 'connect' | 'wifi' | 'identity' | 'done';

export default function CollarProvision({ collarId, sharedSecret, onComplete }: CollarProvisionProps) {
  const { addToast } = useToast();
  const [step, setStep] = useState<ProvisionStep>('connect');
  const [device, setDevice] = useState<BluetoothDevice | null>(null);
  const [configChar, setConfigChar] = useState<BluetoothRemoteGATTCharacteristic | null>(null);
  const [connecting, setConnecting] = useState(false);
  const [provisioning, setProvisioning] = useState(false);

  const [ssid, setSsid] = useState('');
  const [password, setPassword] = useState('');
  const [apiKey, setApiKey] = useState('');

  const [collarStatus, setCollarStatus] = useState<{
    collarId: string;
    wifiConfigured: boolean;
    fwVersion: string;
  } | null>(null);

  const isBluetoothSupported = typeof navigator !== 'undefined' && 'bluetooth' in navigator;

  const handleConnect = async () => {
    if (!isBluetoothSupported) {
      addToast('Web Bluetooth is not supported in this browser', 'error');
      return;
    }

    setConnecting(true);
    try {
      const btDevice = await navigator.bluetooth.requestDevice({
        filters: [{ services: [BLE_SERVICE_UUID] }],
      });

      const server = await btDevice.gatt!.connect();
      const service = await server.getPrimaryService(BLE_SERVICE_UUID);
      const characteristic = await service.getCharacteristic(BLE_CHAR_CONFIG_UUID);

      setDevice(btDevice);
      setConfigChar(characteristic);

      // Read current provisioning status
      const value = await characteristic.readValue();
      const statusJson = new TextDecoder().decode(value);
      try {
        const status = JSON.parse(statusJson);
        setCollarStatus(status);
      } catch { /* ignore parse errors */ }

      setStep('wifi');
      addToast(`Connected to ${btDevice.name || 'Collar'}`, 'success');
    } catch (err) {
      if (err instanceof DOMException && err.name === 'NotFoundError') {
        // User cancelled the picker
      } else {
        addToast(err instanceof Error ? err.message : 'Failed to connect', 'error');
      }
    } finally {
      setConnecting(false);
    }
  };

  const writeConfig = async (data: Record<string, string>) => {
    if (!configChar) return;
    const json = JSON.stringify(data);
    const encoder = new TextEncoder();
    await configChar.writeValue(encoder.encode(json));
  };

  const handleProvisionWifi = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!ssid.trim()) return;

    setProvisioning(true);
    try {
      await writeConfig({
        ssid: ssid.trim(),
        pass: password,
        apiKey: apiKey.trim(),
      });
      addToast('WiFi credentials sent to collar', 'success');
      setStep('identity');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Failed to write WiFi config', 'error');
    } finally {
      setProvisioning(false);
    }
  };

  const handleProvisionIdentity = async () => {
    setProvisioning(true);
    try {
      await writeConfig({
        collarId,
        secret: sharedSecret,
      });
      addToast('Collar identity configured', 'success');
      setStep('done');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Failed to write identity', 'error');
    } finally {
      setProvisioning(false);
    }
  };

  const handleDisconnect = () => {
    if (device?.gatt?.connected) {
      device.gatt.disconnect();
    }
    setDevice(null);
    setConfigChar(null);
    onComplete();
  };

  const stepIndicator = (s: ProvisionStep, label: string, num: number) => {
    const active = step === s;
    const done = ['connect', 'wifi', 'identity', 'done'].indexOf(step) >
                 ['connect', 'wifi', 'identity', 'done'].indexOf(s);
    return (
      <div style={{
        display: 'flex', alignItems: 'center', gap: 8,
        color: active ? 'var(--color-accent)' : done ? 'var(--success)' : 'var(--text-muted)',
        fontWeight: active ? 600 : 400,
        fontSize: '0.85rem',
      }}>
        <span style={{
          width: 24, height: 24, borderRadius: '50%',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontSize: '0.75rem',
          background: done ? 'var(--success)' : active ? 'var(--color-accent)' : 'var(--bg-muted)',
          color: done || active ? '#fff' : 'var(--text-muted)',
        }}>
          {done ? '\u2713' : num}
        </span>
        {label}
      </div>
    );
  };

  return (
    <div style={{ border: '2px solid var(--color-accent)', borderRadius: 8, padding: '1rem', marginBottom: '1rem' }}>
      <h3 style={{ margin: '0 0 0.75rem' }}>Collar Provisioning</h3>

      <div style={{ display: 'flex', gap: 24, marginBottom: '1rem' }}>
        {stepIndicator('connect', 'Connect', 1)}
        {stepIndicator('wifi', 'WiFi Setup', 2)}
        {stepIndicator('identity', 'Identity', 3)}
        {stepIndicator('done', 'Complete', 4)}
      </div>

      {!isBluetoothSupported && (
        <div className="alert alert-error" style={{ marginBottom: '0.5rem' }}>
          Web Bluetooth is not supported. Use Chrome or Edge on a desktop/Android device.
        </div>
      )}

      {step === 'connect' && (
        <div>
          <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', margin: '0 0 0.75rem' }}>
            Power on the collar device. It will advertise as &ldquo;SDDCollar-...&rdquo; via Bluetooth.
          </p>
          <button
            className="btn btn-primary"
            onClick={handleConnect}
            disabled={connecting || !isBluetoothSupported}
          >
            {connecting ? 'Connecting...' : 'Connect to Collar'}
          </button>
        </div>
      )}

      {step === 'wifi' && (
        <div>
          {collarStatus && (
            <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginBottom: '0.5rem' }}>
              Connected to: {device?.name || 'Collar'} | FW: v{collarStatus.fwVersion} |
              WiFi: {collarStatus.wifiConfigured ? 'Configured' : 'Not configured'}
            </div>
          )}
          <form onSubmit={handleProvisionWifi}>
            <div className="form-group">
              <label>WiFi SSID</label>
              <input
                type="text"
                value={ssid}
                onChange={e => setSsid(e.target.value)}
                placeholder="Your home WiFi network"
                required
              />
            </div>
            <div className="form-group">
              <label>WiFi Password</label>
              <input
                type="password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="WiFi password"
              />
            </div>
            <div className="form-group">
              <label>API Key (optional)</label>
              <input
                type="text"
                value={apiKey}
                onChange={e => setApiKey(e.target.value)}
                placeholder="For authenticated uploads"
              />
            </div>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={provisioning}
            >
              {provisioning ? 'Sending...' : 'Configure WiFi'}
            </button>
          </form>
        </div>
      )}

      {step === 'identity' && (
        <div>
          <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', margin: '0 0 0.5rem' }}>
            Send the collar identity and shared secret for NFC authentication.
          </p>
          <div style={{ fontFamily: 'monospace', fontSize: '0.8rem', background: 'var(--bg-muted)', padding: '0.5rem', borderRadius: 4, marginBottom: '0.75rem' }}>
            <div>Collar ID: {collarId}</div>
            <div>Secret: {sharedSecret.slice(0, 8)}...{sharedSecret.slice(-4)}</div>
          </div>
          <button
            className="btn btn-primary"
            onClick={handleProvisionIdentity}
            disabled={provisioning}
          >
            {provisioning ? 'Sending...' : 'Write Identity to Collar'}
          </button>
        </div>
      )}

      {step === 'done' && (
        <div>
          <p style={{ color: 'var(--success)', fontWeight: 600, margin: '0 0 0.5rem' }}>
            Provisioning complete! The collar is now configured.
          </p>
          <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', margin: '0 0 0.75rem' }}>
            The collar will connect to WiFi and begin uploading GPS data automatically.
          </p>
          <button className="btn" onClick={handleDisconnect}>
            Disconnect &amp; Close
          </button>
        </div>
      )}
    </div>
  );
}
