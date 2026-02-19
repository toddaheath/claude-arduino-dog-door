import { Link } from 'react-router-dom';

interface BreadcrumbItem {
  label: string;
  to?: string;
}

interface BreadcrumbProps {
  items: BreadcrumbItem[];
}

export default function Breadcrumb({ items }: BreadcrumbProps) {
  return (
    <nav className="breadcrumb" aria-label="Breadcrumb">
      {items.map((item, i) => (
        <span key={i} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          {i > 0 && <span className="breadcrumb__sep">/</span>}
          {item.to ? (
            <Link to={item.to} className="breadcrumb__item">
              {item.label}
            </Link>
          ) : (
            <span className="breadcrumb__item breadcrumb__item--current">
              {item.label}
            </span>
          )}
        </span>
      ))}
    </nav>
  );
}
