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
        <div className="min-h-screen flex flex-col bg-gray-50">
            {/* Header */}
            <header className="bg-white border-b border-gray-200 sticky top-0 z-50">
                <div className="max-w-6xl mx-auto px-6 py-4 flex items-center justify-between">
                    <Link to="/" className="text-xl font-bold text-blue-600 hover:text-blue-700">
                        Christian Library
                    </Link>
                    <nav className="flex items-center gap-6">
                        <Link to="/" className="text-sm text-gray-600 hover:text-gray-900 transition-colors">
                            Browse Books
                        </Link>
                        {user ? (
                            <>
                                <span className="text-sm text-gray-500">{user.email}</span>
                                <button
                                    onClick={handleLogout}
                                    className="text-sm bg-gray-100 hover:bg-gray-200 text-gray-700 px-4 py-2 rounded-lg transition-colors"
                                >
                                    Sign Out
                                </button>
                            </>
                        ) : (
                            <>
                                <Link to="/login" className="text-sm text-gray-600 hover:text-gray-900 transition-colors">
                                    Sign In
                                </Link>
                                <Link to="/register" className="text-sm bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg transition-colors">
                                    Register
                                </Link>
                            </>
                        )}
                    </nav>
                </div>
            </header>

            {/* Main Content */}
            <main className="flex-1">
                {children}
            </main>

            {/* Footer */}
            <footer className="bg-gray-900 text-gray-400 mt-auto">
                <div className="max-w-6xl mx-auto px-6 py-10 flex flex-col md:flex-row justify-between gap-6">
                    <div>
                        <h3 className="text-white font-semibold text-lg mb-2">Christian Library</h3>
                        <p className="text-sm">Borrow and share Christian books with your community.</p>
                    </div>
                    <div className="flex gap-12">
                        <div>
                            <h4 className="text-white text-sm font-medium mb-3">Browse</h4>
                            <ul className="space-y-2 text-sm">
                                <li><Link to="/" className="hover:text-white transition-colors">All Books</Link></li>
                            </ul>
                        </div>
                        <div>
                            <h4 className="text-white text-sm font-medium mb-3">Account</h4>
                            <ul className="space-y-2 text-sm">
                                <li><Link to="/login" className="hover:text-white transition-colors">Sign In</Link></li>
                                <li><Link to="/register" className="hover:text-white transition-colors">Register</Link></li>
                            </ul>
                        </div>
                    </div>
                </div>
                <div className="border-t border-gray-800 text-center text-xs text-gray-600 py-4">
                    © {new Date().getFullYear()} Christian Library Sharing. All rights reserved.
                </div>
            </footer>
        </div>
    );
}
