import api from './api';

export const userService = {
    getAllUsers: async () => {
        const response = await api.get('/admin/users');
        return response.data;
    },

    getUserById: async (id) => {
        const response = await api.get(`/admin/users/${id}`);
        return response.data;
    },

    toggleUserStatus: async (id) => {
        const response = await api.post(`/admin/users/${id}/toggle-status`);
        return response.data;
    }
};