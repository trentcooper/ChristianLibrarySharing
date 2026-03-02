import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import ProtectedRoute from './components/common/ProtectedRoute';
import Layout from './components/common/Layout';
import LoginForm from './components/auth/LoginForm';
import DashboardHome from './components/dashboard/DashboardHome';
import UserList from './components/users/UserList';
import BookApprovalQueue from './components/books/BookApprovalQueue';

function App() {
    return (
        <AuthProvider>
            <BrowserRouter>
                <Routes>
                    <Route path="/login" element={<LoginForm />} />

                    <Route
                        path="/"
                        element={
                            <ProtectedRoute>
                                <Layout>
                                    <DashboardHome />
                                </Layout>
                            </ProtectedRoute>
                        }
                    />

                    <Route
                        path="/users"
                        element={
                            <ProtectedRoute>
                                <Layout>
                                    <UserList />
                                </Layout>
                            </ProtectedRoute>
                        }
                    />

                    <Route
                        path="/books"
                        element={
                            <ProtectedRoute>
                                <Layout>
                                    <BookApprovalQueue />
                                </Layout>
                            </ProtectedRoute>
                        }
                    />

                    <Route path="*" element={<Navigate to="/" replace />} />
                </Routes>
            </BrowserRouter>
        </AuthProvider>
    );
}

export default App;