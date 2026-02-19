import { useState } from 'react';
import { Link } from 'react-router-dom';
import { getAnimals, createAnimal, deleteAnimal, getAnimalPhotos, getPhotoUrl } from '../api/client';
import { useApi } from '../hooks/useApi';
import { SkeletonCard } from '../components/Skeleton';
import EmptyState from '../components/EmptyState';
import { useToast } from '../contexts/ToastContext';

function AnimalThumbnail({ animalId }: { animalId: number }) {
  const { data: photos, loading } = useApi(() => getAnimalPhotos(animalId), [animalId]);
  const first = photos?.[0];
  if (loading) return <div className="skeleton" style={{ width: '100%', height: 120, borderRadius: '6px 6px 0 0' }} />;
  if (!first) return null;
  return (
    <img
      src={getPhotoUrl(first.id)}
      alt=""
      loading="lazy"
      style={{ width: '100%', height: 120, objectFit: 'cover', borderRadius: '6px 6px 0 0', display: 'block' }}
    />
  );
}

export default function AnimalList() {
  const { data: animals, loading, error, reload } = useApi(getAnimals);
  const { addToast } = useToast();
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState('');
  const [breed, setBreed] = useState('');

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;
    try {
      await createAnimal({ name: name.trim(), breed: breed.trim() || undefined });
      setName('');
      setBreed('');
      setShowForm(false);
      reload();
      addToast(`${name.trim()} added`, 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Failed to create animal', 'error');
    }
  };

  const handleDelete = async (id: number, animalName: string) => {
    if (!confirm(`Delete ${animalName}?`)) return;
    try {
      await deleteAnimal(id);
      reload();
      addToast(`${animalName} deleted`, 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Failed to delete animal', 'error');
    }
  };

  return (
    <div>
      <div className="page-header">
        <h2 className="page-title">Animals</h2>
        <button onClick={() => setShowForm(!showForm)}>
          {showForm ? 'Cancel' : '+ Add Animal'}
        </button>
      </div>

      {showForm && (
        <form onSubmit={handleCreate} style={{ marginBottom: 24, display: 'flex', gap: 12, flexWrap: 'wrap' }}>
          <input
            placeholder="Name"
            value={name}
            onChange={e => setName(e.target.value)}
            required
            className="form-input"
            style={{ width: 200 }}
          />
          <input
            placeholder="Breed (optional)"
            value={breed}
            onChange={e => setBreed(e.target.value)}
            className="form-input"
            style={{ width: 220 }}
          />
          <button type="submit">Create</button>
        </form>
      )}

      {error && <div className="alert alert--error">{error}</div>}

      {loading ? (
        <div className="card-grid">
          {[...Array(6)].map((_, i) => <SkeletonCard key={i} />)}
        </div>
      ) : animals && animals.length === 0 ? (
        <EmptyState
          icon="🐾"
          title="No animals yet"
          message="Add your first animal to get started."
          action={<button onClick={() => setShowForm(true)}>+ Add Animal</button>}
        />
      ) : (
        <div className="card-grid">
          {animals?.map(animal => (
            <div key={animal.id} className="card" style={{ padding: 0, overflow: 'hidden' }}>
              {animal.photoCount > 0 && <AnimalThumbnail animalId={animal.id} />}
              <div style={{ padding: 16, display: 'flex', flexDirection: 'column', gap: 8 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <Link to={`/animals/${animal.id}`} style={{ color: 'var(--color-accent)', textDecoration: 'none', fontSize: 18, fontWeight: 600 }}>
                    {animal.name}
                  </Link>
                  <span className={`badge ${animal.isAllowed ? 'badge--allowed' : 'badge--denied'}`}>
                    {animal.isAllowed ? 'Allowed' : 'Denied'}
                  </span>
                </div>
                {animal.breed && <p style={{ margin: 0, color: 'var(--color-muted)', fontSize: 14 }}>{animal.breed}</p>}
                <p style={{ margin: 0, color: 'var(--color-muted)', fontSize: 12 }}>{animal.photoCount} photo(s)</p>
                <button
                  onClick={() => handleDelete(animal.id, animal.name)}
                  className="btn btn--danger"
                  style={{ alignSelf: 'flex-end', fontSize: 12, padding: '4px 8px' }}
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
