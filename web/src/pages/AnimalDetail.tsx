import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getAnimal, updateAnimal, deleteAnimal, getAnimalPhotos, deletePhoto, getPhotoUrl } from '../api/client';
import { useApi } from '../hooks/useApi';
import PhotoUpload from '../components/PhotoUpload';
import Breadcrumb from '../components/Breadcrumb';
import EmptyState from '../components/EmptyState';
import { Skeleton } from '../components/Skeleton';
import { useToast } from '../contexts/ToastContext';

export default function AnimalDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { addToast } = useToast();
  const animalId = Number(id);

  const { data: animal, loading, error, reload: reloadAnimal } = useApi(() => getAnimal(animalId), [animalId]);
  const { data: photos, loading: photosLoading, reload: reloadPhotos } = useApi(() => getAnimalPhotos(animalId), [animalId]);

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
    try {
      await updateAnimal(animalId, {
        name: name.trim(),
        breed: breed.trim() || undefined,
        isAllowed,
      });
      setEditing(false);
      reloadAnimal();
      addToast('Animal updated', 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Update failed', 'error');
    }
  };

  const handleDelete = async () => {
    if (!confirm(`Delete ${animal?.name}?`)) return;
    try {
      await deleteAnimal(animalId);
      addToast(`${animal?.name} deleted`, 'success');
      navigate('/animals');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Delete failed', 'error');
    }
  };

  const handleDeletePhoto = async (photoId: number) => {
    if (!confirm('Delete this photo?')) return;
    try {
      await deletePhoto(photoId);
      reloadPhotos();
      reloadAnimal();
      addToast('Photo deleted', 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Failed to delete photo', 'error');
    }
  };

  if (loading) {
    return (
      <div>
        <Breadcrumb items={[{ label: 'Animals', to: '/animals' }, { label: '…' }]} />
        <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginBottom: 24 }}>
          <Skeleton width={200} height={32} />
          <Skeleton width={60} height={22} />
        </div>
        <div style={{ display: 'flex', gap: 8, marginBottom: 24 }}>
          <Skeleton width={80} height={34} />
          <Skeleton width={80} height={34} />
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 12 }}>
          {[...Array(4)].map((_, i) => (
            <div key={i} className="skeleton skeleton--card" />
          ))}
        </div>
      </div>
    );
  }

  if (error) return <div className="alert alert--error">Error: {error}</div>;
  if (!animal) return <div className="alert alert--error">Animal not found.</div>;

  return (
    <div>
      <Breadcrumb items={[{ label: 'Animals', to: '/animals' }, { label: animal.name }]} />

      {editing ? (
        <form onSubmit={handleUpdate} style={{ marginBottom: 24 }}>
          <h2>Edit Animal</h2>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12, maxWidth: 400 }}>
            <div className="form-field">
              <label className="form-label">Name</label>
              <input
                className="form-input"
                placeholder="Name"
                value={name}
                onChange={e => setName(e.target.value)}
                required
              />
            </div>
            <div className="form-field">
              <label className="form-label">Breed</label>
              <input
                className="form-input"
                placeholder="Breed (optional)"
                value={breed}
                onChange={e => setBreed(e.target.value)}
              />
            </div>
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
            <span className={`badge ${animal.isAllowed ? 'badge--allowed' : 'badge--denied'}`}>
              {animal.isAllowed ? 'Allowed' : 'Denied'}
            </span>
          </div>
          {animal.breed && <p style={{ color: 'var(--color-muted)', marginTop: 8 }}>{animal.breed}</p>}
          <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
            <button onClick={startEdit}>Edit</button>
            <button onClick={handleDelete} className="btn btn--danger">Delete</button>
          </div>
        </div>
      )}

      <h3>Photos ({photos?.length || 0})</h3>
      <PhotoUpload
        animalId={animalId}
        onUploaded={() => {
          reloadPhotos();
          reloadAnimal();
          addToast('Photo uploaded', 'success');
        }}
      />

      {photosLoading ? (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 12, marginTop: 16 }}>
          {[...Array(4)].map((_, i) => (
            <div key={i} className="skeleton skeleton--card" style={{ height: 200 }} />
          ))}
        </div>
      ) : photos && photos.length === 0 ? (
        <EmptyState icon="📷" title="No photos yet" message="Upload photos so the system can recognise this animal." />
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 12, marginTop: 16 }}>
          {photos?.map(photo => (
            <div key={photo.id} style={{ position: 'relative', borderRadius: 8, overflow: 'hidden', border: '1px solid var(--color-border)' }}>
              <img
                src={getPhotoUrl(photo.id)}
                alt={photo.fileName || 'Animal photo'}
                style={{ width: '100%', height: 200, objectFit: 'cover' }}
              />
              <div style={{ padding: 8, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <small style={{ color: 'var(--color-muted)' }}>{photo.fileName}</small>
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
      )}
    </div>
  );
}
