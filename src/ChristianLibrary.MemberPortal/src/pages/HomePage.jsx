import { useState, useEffect } from 'react';
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

    if (loading) return <p className="p-8 text-gray-500">Loading books...</p>;

    return (
        <div className="max-w-6xl mx-auto px-6 py-8">
            <h1 className="text-4xl font-bold text-blue-600 mb-2">Christian Library</h1>
            <p className="text-gray-500 mb-8">Borrow and share Christian books with your community.</p>

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                {books.map(book => (
                    <div key={book.id} className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 flex flex-col gap-2">
                        <h3 className="font-semibold text-gray-900 text-base leading-snug">{book.title}</h3>
                        <p className="text-sm text-gray-500">{book.author}</p>
                        <p className={`text-sm font-medium ${book.isAvailable ? 'text-green-600' : 'text-red-500'}`}>
                            {book.isAvailable ? 'Available' : 'Unavailable'}
                        </p>
                        <Link
                            to={`/books/${book.id}`}
                            className="mt-auto inline-block text-center bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium py-2 px-4 rounded-lg transition-colors"
                        >
                            View Details
                        </Link>
                    </div>
                ))}
            </div>
        </div>
    );
}
