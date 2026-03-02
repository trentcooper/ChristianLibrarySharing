import api from './api';

export const authService = {
    login: async (email, password) => {
        const response = await api.post('/auth/login', { email, password });
        const authResponse = response.data;

        // Check if login was successful
        if (!authResponse.success) {
            throw new Error(authResponse.message || 'Login failed');
        }

        // Store the token
        if (authResponse.token) {
            localStorage.setItem('authToken', authResponse.token);
        }

        // Return user object in the format React app expects
        return {
            id: authResponse.userId,
            email: authResponse.email,
            name: authResponse.email?.split('@')[0] || 'User', // Use email prefix as name
            roles: authResponse.roles || []
        };
    },

    logout: () => {
        localStorage.removeItem('authToken');
    },

    getCurrentUser: async () => {
        try {
            const response = await api.get('/auth/me');
            return response.data;
        } catch (error) {
            return null;
        }
    },

    isAuthenticated: () => {
        return !!localStorage.getItem('authToken');
    }
};