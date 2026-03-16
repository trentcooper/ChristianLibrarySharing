import {useState, useEffect} from 'react';
import {userService} from '../../services/userService';
import LoadingSpinner from '../common/LoadingSpinner';

export default function UserList() {
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState('');

    useEffect(() => {
        fetchUsers();
    }, []);

    const fetchUsers = async () => {
        try {
            const data = await userService.getAllUsers();
            setUsers(data);
        } catch (error) {
            console.error('Failed to fetch users:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleToggleStatus = async (userId) => {
        try {
            await userService.toggleUserStatus(userId);
            fetchUsers(); // Refresh the list
        } catch (error) {
            console.error('Failed to toggle user status:', error);
        }
    };

    const filteredUsers = users.filter(user =>
        user.email?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        user.name?.toLowerCase().includes(searchTerm.toLowerCase())
    );

    if (loading) return <LoadingSpinner/>;

    return (
        <div>
            <div style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: '2rem'
            }}>
                <h1>Users</h1>
                <input
                    type="text"
                    placeholder="Search users..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    style={{
                        padding: '0.5rem',
                        border: '1px solid #ddd',
                        borderRadius: '4px',
                        width: '300px'
                    }}
                />
            </div>

            <div style={{
                background: 'white',
                borderRadius: '8px',
                boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
                overflow: 'hidden'
            }}>
                <table style={{width: '100%', borderCollapse: 'collapse'}}>
                    <thead>
                    <tr style={{background: '#f5f5f5'}}>
                        <th style={tableHeaderStyle}>Name</th>
                        <th style={tableHeaderStyle}>Email</th>
                        <th style={tableHeaderStyle}>Status</th>
                        <th style={tableHeaderStyle}>Joined</th>
                        <th style={tableHeaderStyle}>Actions</th>
                    </tr>
                    </thead>
                    <tbody>
                    {filteredUsers.map(user => (
                        <tr key={user.id} style={{borderBottom: '1px solid #eee'}}>
                            <td style={tableCellStyle}>{user.name || 'N/A'}</td>
                            <td style={tableCellStyle}>{user.email}</td>
                            <td style={tableCellStyle}>
                                    <span style={{
                                        padding: '0.25rem 0.75rem',
                                        borderRadius: '12px',
                                        fontSize: '0.875rem',
                                        background: user.isActive ? '#e8f5e9' : '#ffebee',
                                        color: user.isActive ? '#2e7d32' : '#c62828'
                                    }}>
                                        {user.isActive ? 'Active' : 'Inactive'}
                                    </span>
                            </td>
                            <td style={tableCellStyle}>
                                {new Date(user.createdAt).toLocaleDateString()}
                            </td>
                            <td style={tableCellStyle}>
                                <button
                                    onClick={() => handleToggleStatus(user.id)}
                                    style={{
                                        padding: '0.5rem 1rem',
                                        background: user.isActive ? '#f44336' : '#4caf50',
                                        color: 'white',
                                        border: 'none',
                                        borderRadius: '4px',
                                        cursor: 'pointer'
                                    }}
                                >
                                    {user.isActive ? 'Deactivate' : 'Activate'}
                                </button>
                            </td>
                        </tr>
                    ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
}

const tableHeaderStyle = {
    padding: '1rem',
    textAlign: 'left',
    fontWeight: '600',
    color: '#666'
};

const tableCellStyle = {
    padding: '1rem'
};