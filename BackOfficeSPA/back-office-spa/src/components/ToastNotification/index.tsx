import React, { useState } from 'react';
import { Toast, ToastContainer } from 'react-bootstrap';
import { ToastContext, Toast as ToastType } from './ToastContext';

interface ToastProviderProps {
  children: React.ReactNode;
}

export const ToastProvider: React.FC<ToastProviderProps> = ({ children }) => {
  const [toasts, setToasts] = useState<ToastType[]>([]);

  const show = (
    title: string,
    message: string,
    options: Partial<Omit<ToastType, 'id' | 'title' | 'message'>> = {}
  ) => {
    const newToast: ToastType = {
      id: Date.now(),
      title,
      message,
      autohide: options.autohide !== undefined ? options.autohide : true,
      delay: options.autohide !== false && !options.delay ? 3000 : options.delay,
      ...options,
    };
    setToasts(prevToasts => [...prevToasts, newToast]);
  };

  const removeToast = (id: number) => {
    setToasts(prevToasts => prevToasts.filter(toast => toast.id !== id));
  };

  return (
    <ToastContext.Provider value={{ show }}>
      {children}
      <ToastContainer position="top-end" className="p-3">
        {toasts.map(toast => (
          <Toast
            key={toast.id}
            bg={toast.variant}
            onClose={() => removeToast(toast.id)}
            show
            delay={toast.autohide ? toast.delay : undefined}
            autohide={toast.autohide}
          >
            <Toast.Header>
              <strong className="me-auto">{toast.title}</strong>
            </Toast.Header>
            <Toast.Body>{toast.message}</Toast.Body>
          </Toast>
        ))}
      </ToastContainer>
    </ToastContext.Provider>
  );
};
