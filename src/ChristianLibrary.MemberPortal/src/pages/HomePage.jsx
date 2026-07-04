import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../services/api';

export default function HomePage() {
    const [books, setBooks] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchBooks = async () => {
            try {
                const response = await api.get('/books');
                setBooks(response.data);
            } catch (error) {
                console.error('Failed to fetch books:', error);
            } finally {
                setLoading(false);
            }
        };

        fetchBooks();
    }, []);

    return (
        <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '2rem' }}>
            <h1 style={{ marginBottom: '0.5rem' }}>Christian Library</h1>
            <p style={{ color: '#666', marginBottom: '2rem' }}>
                Borrow and share Christian books with your community.
            </p>

            {loading ? (
                <p>Loading books...</p>
            ) : (
                <div style={{
                    display: 'grid',
                    gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))',
                    gap: '1.5rem'
                }}>
                    {books.map(book => (
                        <div key={book.id} style={{
                            background: 'white',
                            borderRadius: '8px',
                            boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
                            padding: '1.25rem'
                        }}>
                            <h3 style={{ margin: '0 0 0.5rem 0', fontSize: '1rem' }}>{book.title}</h3>
                            <p style={{ margin: '0 0 0.5rem 0', color: '#666', fontSize: '0.875rem' }}>{book.author}</p>
                            <p style={{ margin: '0 0 1rem 0', fontSize: '0.875rem', color: book.isAvailable ? '#388e3c' : '#c62828' }}>
                                {book.isAvailable ? 'Available' : 'Unavailable'}
                            </p>
                            <Link to={`/books/${book.id}`} style={{
                                display: 'inline-block',
                                padding: '0.5rem 1rem',
                                backgroundColor: '#1976d2',
                                color: 'white',
                                borderRadius: '4px',
                                textDecoration: 'none',
                                fontSize: '0.875rem'
                            }}>
                                View Details
                            </Link>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
