import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../services/api';

export default function BookDetail() {
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

    if (loading) return <p className="p-8 text-gray-500">Loading...</p>;
    if (error) return <p className="p-8 text-red-600">{error}</p>;

    return (
        <div className="max-w-3xl mx-auto px-6 py-8">
            <button
                onClick={() => navigate('/')}
                className="text-blue-600 hover:text-blue-700 text-sm mb-6 flex items-center gap-1 transition-colors"
            >
                ← Back to Catalog
            </button>

            <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-8">
                <h1 className="text-3xl font-bold text-gray-900 mb-1">{book.title}</h1>
                <p className="text-gray-500 text-lg mb-4">by {book.author}</p>

                <span className={`inline-block px-3 py-1 rounded-full text-sm font-medium mb-6 ${
                    book.isAvailable
                        ? 'bg-green-100 text-green-700'
                        : 'bg-red-100 text-red-600'
                }`}>
                    {book.isAvailable ? 'Available' : 'Unavailable'}
                </span>

                {book.description && (
                    <p className="text-gray-600 leading-relaxed mb-6">{book.description}</p>
                )}

                {book.isbn && (
                    <p className="text-sm text-gray-400 mb-8">ISBN: {book.isbn}</p>
                )}

                {requestSuccess ? (
                    <div className="bg-green-50 text-green-700 px-4 py-3 rounded-lg">
                        ✓ Borrow request submitted successfully!
                    </div>
                ) : (
                    <button
                        onClick={handleBorrowRequest}
                        disabled={!book.isAvailable || requesting}
                        className={`px-8 py-3 rounded-lg text-white font-medium transition-colors ${
                            book.isAvailable
                                ? 'bg-blue-600 hover:bg-blue-700 cursor-pointer'
                                : 'bg-gray-300 cursor-not-allowed'
                        }`}
                    >
                        {requesting ? 'Submitting...' : 'Request to Borrow'}
                    </button>
                )}
            </div>
        </div>
    );
}
