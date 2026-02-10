import { useState } from 'react';
import { Link } from 'react-router-dom';
import { getAnimals, createAnimal, deleteAnimal } from '../api/client';
import { useApi } from '../hooks/useApi';

export default function AnimalList() {
  const { data: animals, loading, error, reload } = useApi(getAnimals);
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState('');
  const [breed, setBreed] = useState('');

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;
    await createAnimal({ name: name.trim(), breed: breed.trim() || undefined });
    setName('');
    setBreed('');
    setShowForm(false);
    reload();
  };

  const handleDelete = async (id: number, animalName: string) => {
    if (!confirm(`Delete ${animalName}?`)) return;
    await deleteAnimal(id);
    reload();
  };

  if (loading) return <p>Loading...</p>;
  if (error) return <p style={{ color: 'red' }}>Error: {error}</p>;

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h2>Animals</h2>
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
            style={{ padding: 8, borderRadius: 4, border: '1px solid #444', background: '#1a1a2e', color: '#fff' }}
          />
          <input
            placeholder="Breed (optional)"
            value={breed}
            onChange={e => setBreed(e.target.value)}
            style={{ padding: 8, borderRadius: 4, border: '1px solid #444', background: '#1a1a2e', color: '#fff' }}
          />
          <button type="submit">Create</button>
        </form>
      )}

      {animals && animals.length === 0 && <p>No animals yet. Add one to get started.</p>}

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: 16 }}>
        {animals?.map(animal => (
          <div
            key={animal.id}
            style={{
              border: '1px solid #333',
              borderRadius: 8,
              padding: 16,
              display: 'flex',
              flexDirection: 'column',
              gap: 8,
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Link to={`/animals/${animal.id}`} style={{ color: '#4fc3f7', textDecoration: 'none', fontSize: 18, fontWeight: 600 }}>
                {animal.name}
              </Link>
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
            {animal.breed && <p style={{ margin: 0, color: '#999', fontSize: 14 }}>{animal.breed}</p>}
            <p style={{ margin: 0, color: '#999', fontSize: 12 }}>{animal.photoCount} photo(s)</p>
            <button
              onClick={() => handleDelete(animal.id, animal.name)}
              style={{ alignSelf: 'flex-end', fontSize: 12, color: '#ef5350', background: 'none', border: '1px solid #ef5350', borderRadius: 4, padding: '4px 8px', cursor: 'pointer' }}
            >
              Delete
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
