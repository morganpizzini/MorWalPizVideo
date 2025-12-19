import React from 'react';

interface FieldErrorProps {
  error?: string;
}

const FieldError: React.FC<FieldErrorProps> = ({ error }) => {
  if (!error) {
    return null;
  }

  return <p className="text-danger mt-1">{error}</p>;
};

export default FieldError;
