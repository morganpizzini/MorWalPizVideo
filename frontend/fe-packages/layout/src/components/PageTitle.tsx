import type { ReactNode } from "react";

export interface PageTitleProps {
  title: string;
  subtitle?: ReactNode;
  className?: string;
}

export function PageTitle({ title, subtitle, className }: PageTitleProps) {
  return (
    <header className={`mwp-page-title ${className ?? ""}`}>
      <h1 className="mwp-page-title__heading">{title}</h1>
      {subtitle && <p className="mwp-page-title__subtitle">{subtitle}</p>}
    </header>
  );
}

export default PageTitle;
