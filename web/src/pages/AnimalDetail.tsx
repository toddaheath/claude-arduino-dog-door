import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getAnimal, updateAnimal, deleteAnimal, getAnimalPhotos, deletePhoto, getPhotoUrl } from '../api/client';
import { useApi } from '../hooks/useApi';
import PhotoUpload from '../components/PhotoUpload';

export default function AnimalDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const animalId = Number(id);

  const { data: animal, loading, error, reload: reloadAnimal } = useApi(() => getAnimal(animalId), [animalId]);
  const { data: photos, reload: reloadPhotos } = useApi(() => getAnimalPhotos(animalId), [animalId]);

  const [editing, setEditing] = useState(false);
  const [name, setName] = useState('');
  const [breed, setBreed] = useState('');
  const [isAllowed, setIsAllowed] = useState(true);

  const startEdit = () => {
    if (!animal) return;
    setName(animal.name);
    setBreed(animal.breed || '');
    setIsAllowed(animal.isAllowed);
    setEditing(true);
  };

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault();
    await updateAnimal(animalId, {
      name: name.trim(),
      breed: breed.trim() || undefined,
      isAllowed,
    });
    setEditing(false);
    reloadAnimal();
  };

  const handleDelete = async () => {
    if (!confirm(`Delete ${animal?.name}?`)) return;
    await deleteAnimal(animalId);
    navigate('/');
  };

  const handleDeletePhoto = async (photoId: number) => {
    if (!confirm('Delete this photo?')) return;
    await deletePhoto(photoId);
    reloadPhotos();
    reloadAnimal();
  };

  if (loading) return <p>Loading...</p>;
  if (error) return <p style={{ color: 'red' }}>Error: {error}</p>;
  if (!animal) return <p>Animal not found.</p>;

  return (
    <div>
      <button onClick={() => navigate('/')} style={{ marginBottom: 16 }}>Back</button>

      {editing ? (
        <form onSubmit={handleUpdate} style={{ marginBottom: 24 }}>
          <h2>Edit Animal</h2>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12, maxWidth: 400 }}>
            <input
              placeholder="Name"
              value={name}
              onChange={e => setName(e.target.value)}
              required
              style={{ padding: 8, borderRadius: 4, border: '1px solid #444', background: '#1a1a2e', color: '#fff' }}
            />
            <input
              placeholder="Breed"
              value={breed}
              onChange={e => setBreed(e.target.value)}
              style={{ padding: 8, borderRadius: 4, border: '1px solid #444', background: '#1a1a2e', color: '#fff' }}
            />
            <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <input type="checkbox" checked={isAllowed} onChange={e => setIsAllowed(e.target.checked)} />
              Allowed through door
            </label>
            <div style={{ display: 'flex', gap: 8 }}>
              <button type="submit">Save</button>
              <button type="button" onClick={() => setEditing(false)}>Cancel</button>
            </div>
          </div>
        </form>
      ) : (
        <div style={{ marginBottom: 24 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
            <h2 style={{ margin: 0 }}>{animal.name}</h2>
            <span style={{
              padding: '2px 8px',
              borderRadius: 12,
              fontSize: 12,
              background: animal.isAllowed ? '#2e7d32' : '#c62828',
              color: '#fff',
            }}>
              {animal.isAllowed ? 'Allowed' : 'Denied'}
            </span>
          </div>
          {animal.breed && <p style={{ color: '#999' }}>{animal.breed}</p>}
          <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
            <button onClick={startEdit}>Edit</button>
            <button onClick={handleDelete} style={{ color: '#ef5350', borderColor: '#ef5350' }}>Delete</button>
          </div>
        </div>
      )}

      <h3>Photos ({photos?.length || 0})</h3>
      <PhotoUpload animalId={animalId} onUploaded={() => { reloadPhotos(); reloadAnimal(); }} />

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 12, marginTop: 16 }}>
        {photos?.map(photo => (
          <div key={photo.id} style={{ position: 'relative', borderRadius: 8, overflow: 'hidden', border: '1px solid #333' }}>
            <img
              src={getPhotoUrl(photo.id)}
              alt={photo.fileName || 'Animal photo'}
              style={{ width: '100%', height: 200, objectFit: 'cover' }}
            />
            <div style={{ padding: 8, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <small style={{ color: '#999' }}>{photo.fileName}</small>
              <button
                onClick={() => handleDeletePhoto(photo.id)}
                style={{ fontSize: 11, color: '#ef5350', background: 'none', border: 'none', cursor: 'pointer' }}
              >
                Delete
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
