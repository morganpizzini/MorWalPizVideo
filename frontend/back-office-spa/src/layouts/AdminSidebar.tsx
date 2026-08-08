import { Nav, Offcanvas } from 'react-bootstrap';
import { NavLink } from 'react-router';
import { authService } from '../services/authService';
import { adminMenuGroups, canAccessMenuItem } from './adminMenu';

interface AdminSidebarProps {
  show: boolean;
  onHide: () => void;
}

export default function AdminSidebar({ show, onHide }: AdminSidebarProps) {
  const permissions = authService.getPermissions();
  const content = (
    <Nav className="flex-column gap-1 px-3 pb-3">
      {adminMenuGroups.map(group => {
        const items = group.items.filter(item => canAccessMenuItem(item, permissions));
        if (items.length === 0) return null;
        return (
          <div key={group.label} className="mb-3">
            <div className="sidebar-section-label">{group.label}</div>
            {items.map(item => {
              const Icon = item.icon;
              return (
                <Nav.Link as={NavLink} to={item.path} end={item.path === '/'} key={item.path} onClick={onHide}>
                  <Icon size={17} aria-hidden="true" />
                  <span>{item.label}</span>
                </Nav.Link>
              );
            })}
          </div>
        );
      })}
    </Nav>
  );

  return (
    <>
      <aside className="admin-sidebar d-none d-lg-flex flex-column">{content}</aside>
      <Offcanvas show={show} onHide={onHide} responsive="lg" className="admin-sidebar-offcanvas">
        <Offcanvas.Header closeButton>
          <Offcanvas.Title>MorWalPiz Admin</Offcanvas.Title>
        </Offcanvas.Header>
        <Offcanvas.Body className="p-0">{content}</Offcanvas.Body>
      </Offcanvas>
    </>
  );
}