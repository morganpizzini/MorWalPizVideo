import React from 'react';

interface GenericErrorListProps {
  errors?: string[];
}

const GenericErrorList: React.FC<GenericErrorListProps> = ({ errors }) => {
  if (!errors || errors.length === 0) {
    return null;
  }

  return (
    <>
      <p className="text-danger mt-1">Generic errors found:</p>
      <ul className="text-danger">
        {errors.map((error, index) => (
          <li className="text-danger mt-1" key={index}>
            {error}
          </li>
        ))}
      </ul>
    </>
  );
};

export default GenericErrorList;
