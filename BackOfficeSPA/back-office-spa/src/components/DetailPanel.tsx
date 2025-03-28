import React, { ReactNode } from 'react';

interface DetailPanelProps {
  title: string;
  children: ReactNode;
}

const DetailPanel: React.FC<DetailPanelProps> = ({ title, children }) => {
  return (
    <div className="mt-4 p-3 border rounded">
      <h5 className="mb-3">{title}</h5>
      {children}
    </div>
  );
};

export default DetailPanel;
