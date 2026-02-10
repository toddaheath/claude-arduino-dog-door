import { useState, useRef } from 'react';
import { uploadPhoto } from '../api/client';

interface Props {
  animalId: number;
  onUploaded: () => void;
}

export default function PhotoUpload({ animalId, onUploaded }: Props) {
  const [dragging, setDragging] = useState(false);
  const [uploading, setUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFiles = async (files: FileList | null) => {
    if (!files || files.length === 0) return;
    setUploading(true);
    try {
      for (const file of Array.from(files)) {
        await uploadPhoto(animalId, file);
      }
      onUploaded();
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setUploading(false);
    }
  };

  return (
    <div
      onDragOver={e => { e.preventDefault(); setDragging(true); }}
      onDragLeave={() => setDragging(false)}
      onDrop={e => { e.preventDefault(); setDragging(false); handleFiles(e.dataTransfer.files); }}
      onClick={() => fileInputRef.current?.click()}
      style={{
        border: `2px dashed ${dragging ? '#4fc3f7' : '#666'}`,
        borderRadius: 8,
        padding: 32,
        textAlign: 'center',
        cursor: 'pointer',
        background: dragging ? '#1a1a2e' : 'transparent',
        transition: 'all 0.2s',
      }}
    >
      <input
        ref={fileInputRef}
        type="file"
        accept=".jpg,.jpeg,.png"
        multiple
        onChange={e => handleFiles(e.target.files)}
        style={{ display: 'none' }}
      />
      {uploading ? (
        <p>Uploading...</p>
      ) : (
        <p>Drop photos here or click to browse<br /><small>.jpg, .jpeg, .png (max 10MB)</small></p>
      )}
    </div>
  );
}
