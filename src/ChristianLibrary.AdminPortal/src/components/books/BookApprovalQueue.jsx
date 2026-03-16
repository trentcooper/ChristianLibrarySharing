import {useState, useEffect} from 'react';
import {bookService} from '../../services/bookService';
import LoadingSpinner from '../common/LoadingSpinner';

export default function BookApprovalQueue() {
    const [books, setBooks] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetchPendingBooks();
    }, []);

    const fetchPendingBooks = async () => {
        try {
            const data = await bookService.getPendingBooks();
            setBooks(data);
        } catch (error) {
            console.error('Failed to fetch pending books:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleApprove = async (bookId) => {
        try {
            await bookService.approveBook(bookId);
            fetchPendingBooks(); // Refresh list
        } catch (error) {
            console.error('Failed to approve book:', error);
        }
    };

    const handleReject = async (bookId) => {
        const reason = prompt('Reason for rejection (optional):');
        try {
            await bookService.rejectBook(bookId, reason);
            fetchPendingBooks(); // Refresh list
        } catch (error) {
            console.error('Failed to reject book:', error);
        }
    };

    if (loading) return <LoadingSpinner/>;

    return (
        <div>
            <h1>Book Approval Queue</h1>

            {books.length === 0 ? (
                <div style={{
                    background: 'white',
                    padding: '2rem',
                    borderRadius: '8px',
                    textAlign: 'center',
                    color: '#666'
                }}>
                    No pending books to review
                </div>
            ) : (
                <div style={{
                    display: 'grid',
                    gap: '1rem',
                    marginTop: '2rem'
                }}>
                    {books.map(book => (
                        <BookCard
                            key={book.id}
                            book={book}
                            onApprove={handleApprove}
                            onReject={handleReject}
                        />
                    ))}
                </div>
            )}
        </div>
    );
}

function BookCard({book, onApprove, onReject}) {
    return (
        <div style={{
            background: 'white',
            padding: '1.5rem',
            borderRadius: '8px',
            boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center'
        }}>
            <div>
                <h3 style={{margin: '0 0 0.5rem 0'}}>{book.title}</h3>
                <p style={{margin: '0 0 0.25rem 0', color: '#666'}}>
                    Author: {book.author}
                </p>
                <p style={{margin: 0, color: '#999', fontSize: '0.875rem'}}>
                    Submitted: {new Date(book.submittedAt).toLocaleDateString()}
                </p>
            </div>
            <div style={{display: 'flex', gap: '0.5rem'}}>
                <button
                    onClick={() => onApprove(book.id)}
                    style={{
                        padding: '0.5rem 1.5rem',
                        background: '#4caf50',
                        color: 'white',
                        border: 'none',
                        borderRadius: '4px',
                        cursor: 'pointer'
                    }}
                >
                    Approve
                </button>
                <button
                    onClick={() => onReject(book.id)}
                    style={{
                        padding: '0.5rem 1.5rem',
                        background: '#f44336',
                        color: 'white',
                        border: 'none',
                        borderRadius: '4px',
                        cursor: 'pointer'
                    }}
                >
                    Reject
                </button>
            </div>
        </div>
    );
}