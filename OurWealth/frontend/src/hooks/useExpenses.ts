import { useQuery } from '@tanstack/react-query';
import {Expense, expensesApi} from "../services/api";

export const useExpenses = () => {
    return useQuery({
        queryKey: ['expenses'],
        queryFn: async () => {
            const response = await expensesApi.getAll();
            return response.data;
        },
    })
}