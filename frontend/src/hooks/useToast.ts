'use client';

import * as React from 'react';

export type ToastVariant = 'default' | 'destructive';

export interface Toast {
  id: string;
  title: string;
  description?: string;
  variant?: ToastVariant;
}

type ToastAction =
  | { type: 'ADD'; toast: Toast }
  | { type: 'REMOVE'; id: string };

const reducer = (state: Toast[], action: ToastAction): Toast[] => {
  switch (action.type) {
    case 'ADD':
      return [...state, action.toast];
    case 'REMOVE':
      return state.filter((t) => t.id !== action.id);
  }
};

// Global singleton so toast() works outside React components
let dispatch: React.Dispatch<ToastAction> | null = null;

export function useToastStore() {
  const [toasts, localDispatch] = React.useReducer(reducer, []);

  React.useEffect(() => {
    dispatch = localDispatch;
    return () => { dispatch = null; };
  }, [localDispatch]);

  const dismiss = (id: string) => localDispatch({ type: 'REMOVE', id });

  return { toasts, dismiss };
}

export function toast(options: Omit<Toast, 'id'>) {
  const id = Math.random().toString(36).slice(2);
  dispatch?.({ type: 'ADD', toast: { id, ...options } });
  // Auto-dismiss after 4 s
  setTimeout(() => dispatch?.({ type: 'REMOVE', id }), 4000);
}
