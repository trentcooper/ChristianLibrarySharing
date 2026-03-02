import { useState, useEffect } from 'react';
import api from '../../services/api';
import LoadingSpinner from '../common/LoadingSpinner';

export default function DashboardHome() {
    const [stats, setStats] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchStats = async () => {
            try {
                const response = await api.get('/admin/dashboard/stats');
                setStats(response.data);
            } catch (error) {
                console.error('Failed to fetch stats:', error);
            } finally {
                setLoading(false);
            }
        };

        fetchStats();
    }, []);

    if (loading) return <LoadingSpinner />;

    return (
        <div>
            <h1>Dashboard</h1>

            <div style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
                gap: '1rem',
                marginTop: '2rem'
            }}>
                <StatCard
                    title="Total Users"
                    value={stats?.totalUsers || 0}
                    color="#1976d2"
                />
                <StatCard
                    title="Active Loans"
                    value={stats?.activeLoans || 0}
                    color="#388e3c"
                />
                <StatCard
                    title="Pending Approvals"
                    value={stats?.pendingApprovals || 0}
                    color="#f57c00"
                />
                <StatCard
                    title="Total Books"
                    value={stats?.totalBooks || 0}
                    color="#7b1fa2"
                />
            </div>
        </div>
    );
}

function StatCard({ title, value, color }) {
    return (
        <div style={{
            background: 'white',
            padding: '1.5rem',
            borderRadius: '8px',
            boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
            borderLeft: `4px solid ${color}`
        }}>
            <h3 style={{ margin: '0 0 0.5rem 0', color: '#666', fontSize: '0.9rem' }}>
                {title}
            </h3>
            <p style={{ margin: 0, fontSize: '2rem', fontWeight: 'bold', color }}>
                {value}
            </p>
        </div>
    );
}