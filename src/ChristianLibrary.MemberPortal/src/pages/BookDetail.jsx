import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../services/api';

export default function BookDetailPage() {
    const { id } = useParams();
    const navigate = useNavigate();
    const [book, setBook] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [requesting, setRequesting] = useState(false);
    const [requestSuccess, setRequestSuccess] = useState(false);

    useEffect(() => {
        const fetchBook = async () => {
            try {
                const response = await api.get(`/books/${id}`);
                setBook(response.data);
            } catch (err) {
                setError('Book not found.');
            } finally {
                setLoading(false);
            }
        };

        fetchBook();
    }, [id]);

    const handleBorrowRequest = async () => {
        setRequesting(true);
        try {
            await api.post('/borrowrequests', { bookId: parseInt(id) });
            setRequestSuccess(true);
        } catch (err) {
            setError(err.response?.data?.message || 'Failed to submit borrow request.');
        } finally {
            setRequesting(false);
        }
    };

    if (loading) return <p style={{ padding: '2rem' }}>Loading...</p>;
    if (error) return <p style={{ padding: '2rem', color: '#c62828' }}>{error}</p>;

    return (
        <div style={{ maxWidth: '800px', margin: '0 auto', padding: '2rem' }}>
            <button
                onClick={() => navigate('/')}
                style={{
                    background: 'none',
                    border: 'none',
                    color: '#1976d2',
                    cursor: 'pointer',
                    fontSize: '1rem',
                    marginBottom: '1.5rem',
                    padding: 0
                }}
            >
                ← Back to Catalog
            </button>

            <div style={{
                background: 'white',
                borderRadius: '8px',
                boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
                padding: '2rem'
            }}>
                <h1 style={{ margin: '0 0 0.5rem 0' }}>{book.title}</h1>
                <p style={{ color: '#666', fontSize: '1.1rem', margin: '0 0 1rem 0' }}>by {book.author}</p>

                <p style={{
                    display: 'inline-block',
                    padding: '0.25rem 0.75rem',
                    borderRadius: '999px',
                    backgroundColor: book.isAvailable ? '#e8f5e9' : '#ffebee',
                    color: book.isAvailable ? '#388e3c' : '#c62828',
                    fontSize: '0.875rem',
                    marginBottom: '1.5rem'
                }}>
                    {book.isAvailable ? 'Available' : 'Unavailable'}
                </p>

                {book.description && (
                    <p style={{ lineHeight: '1.6', marginBottom: '1.5rem' }}>{book.description}</p>
                )}

                {book.isbn && (
                    <p style={{ color: '#666', fontSize: '0.875rem', marginBottom: '1.5rem' }}>
                        ISBN: {book.isbn}
                    </p>
                )}

                {requestSuccess ? (
                    <div style={{
                        background: '#e8f5e9',
                        color: '#388e3c',
                        padding: '1rem',
                        borderRadius: '4px'
                    }}>
                        ✓ Borrow request submitted successfully!
                    </div>
                ) : (
                    <button
                        onClick={handleBorrowRequest}
                        disabled={!book.isAvailable || requesting}
                        style={{
                            padding: '0.75rem 2rem',
                            backgroundColor: book.isAvailable ? '#1976d2' : '#ccc',
                            color: 'white',
                            border: 'none',
                            borderRadius: '4px',
                            fontSize: '1rem',
                            cursor: book.isAvailable ? 'pointer' : 'not-allowed'
                        }}
                    >
                        {requesting ? 'Submitting...' : 'Request to Borrow'}
                    </button>
                )}
            </div>
        </div>
    );
}
