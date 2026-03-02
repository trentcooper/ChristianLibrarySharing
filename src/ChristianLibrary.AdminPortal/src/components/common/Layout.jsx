import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';

export default function Layout({ children }) {
    const { user, logout } = useAuth();
    const navigate = useNavigate();

    const handleLogout = () => {
        logout();
        navigate('/login');
    };

    return (
        <div style={{ minHeight: '100vh' }}>
            <nav style={{
                background: '#1976d2',
                color: 'white',
                padding: '1rem',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center'
            }}>
                <div style={{ display: 'flex', gap: '2rem', alignItems: 'center' }}>
                    <h1 style={{ margin: 0, fontSize: '1.5rem' }}>Christian Library Admin</h1>
                    <div style={{ display: 'flex', gap: '1rem' }}>
                        <Link to="/" style={{ color: 'white', textDecoration: 'none' }}>
                            Dashboard
                        </Link>
                        <Link to="/users" style={{ color: 'white', textDecoration: 'none' }}>
                            Users
                        </Link>
                        <Link to="/books" style={{ color: 'white', textDecoration: 'none' }}>
                            Books
                        </Link>
                    </div>
                </div>
                <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
                    <span>{user?.email}</span>
                    <button onClick={handleLogout} style={{
                        background: 'white',
                        color: '#1976d2',
                        border: 'none',
                        padding: '0.5rem 1rem',
                        borderRadius: '4px',
                        cursor: 'pointer'
                    }}>
                        Logout
                    </button>
                </div>
            </nav>
            <main style={{ padding: '2rem' }}>
                {children}
            </main>
        </div>
    );
}