import api from './api';

export const bookService = {
    getPendingBooks: async () => {
        const response = await api.get('/admin/books/pending');
        return response.data;
    },

    approveBook: async (id) => {
        const response = await api.post(`/admin/books/${id}/approve`);
        return response.data;
    },

    rejectBook: async (id, reason) => {
        const response = await api.post(`/admin/books/${id}/reject`, { reason });
        return response.data;
    },

    getAllBooks: async () => {
        const response = await api.get('/admin/books');
        return response.data;
    }
};