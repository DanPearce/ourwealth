import axios from 'axios';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000/api';

export const api = axios.create({
    baseURL: `${API_URL}/api`,
    headers: {
        'Content-Type': 'application/json',
    },
});

export interface Expense {
    id: number; 
    householdId: number;
    categoryId: number;
    description: string;
    amount: number;
    expenseDate: string;
    notes?: string;
    category: {
        id: number;
        name: string;
        color: string;
        icon: string;
    };
}

export const expensesApi = {
    getAll: () => api.get<Expense[]>('/expenses'),
    getById: (id: number) => api.get<Expense>(`/expenses/${id}`),
    getSummary: (id: number) => api.get<Expense>(`/expenses/summary`),
}