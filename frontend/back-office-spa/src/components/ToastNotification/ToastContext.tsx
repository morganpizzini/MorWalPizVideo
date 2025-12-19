import { createContext, useContext } from 'react';

export interface Toast {
  id: number;
  title: string;
  message: string;
  variant?: 'primary' | 'secondary' | 'success' | 'danger' | 'warning' | 'info' | 'light' | 'dark';
  autohide?: boolean;
  delay?: number;
}

export interface ToastContextProps {
  show: (
    title: string,
    message: string,
    options?: Partial<Omit<Toast, 'id' | 'title' | 'message'>>
  ) => void;
}

export const ToastContext = createContext<ToastContextProps | undefined>(undefined);

export const useToast = (): ToastContextProps => {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error('useToast must be used within a ToastProvider');
  }
  return context;
};
