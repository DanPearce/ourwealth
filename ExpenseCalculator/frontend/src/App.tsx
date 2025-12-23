import React from 'react';
import './App.css';
import { useExpenses } from './hooks/useExpenses';

function App() {
  const { data: expenses, isLoading, error } = useExpenses();

  if (isLoading) return <div>Loading expenses...</div>;
  if (error) return <div>Error loading expenses: {String(error)}</div>;

  return (
      <div className="App">
        <header className="App-header">
          <h1>OurWealth</h1>
          <h2>Recent Expenses</h2>

          {expenses && expenses.length > 0 ? (
              <ul style={{ textAlign: 'left', maxWidth: '600px' }}>
                {expenses.map((expense) => (
                    <li key={expense.id}>
                      <strong>{expense.description}</strong> - £{expense.amount.toFixed(2)}
                      <br />
                      <small>
                        {expense.category?.name} • {new Date(expense.expenseDate).toLocaleDateString()}
                      </small>
                    </li>
                ))}
              </ul>
          ) : (
              <p>No expenses found</p>
          )}
        </header>
      </div>
  );
}

export default App;