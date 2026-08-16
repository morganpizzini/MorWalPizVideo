import { Nav, Offcanvas } from 'react-bootstrap';
import { NavLink } from 'react-router';
import { adminMenuGroups, canAccessMenuItem } from './adminMenu';
import { useChannelContext } from '../contexts/ChannelContext';
import { useAppStore } from '../state/appStore';

interface AdminSidebarProps {
  show: boolean;
  onHide: () => void;
}

export default function AdminSidebar({ show, onHide }: AdminSidebarProps) {
  const permissions = useAppStore(state => state.effectivePermissions);
  const { channels, selectedChannelId, selectChannel } = useChannelContext();
  const content = (
    <Nav as="nav" aria-label="Admin navigation" className="flex-column gap-1 px-3 pb-3">
      <div className="channel-selector mb-3">
        <label className="sidebar-section-label d-block" htmlFor="channel-selector">Channel</label>
        <select
          id="channel-selector"
          className="form-select form-select-sm"
          aria-label="Select channel"
          value={selectedChannelId ?? ''}
          onChange={event => selectChannel(event.target.value)}
          disabled={channels.length === 0}
        >
          {channels.length === 0 ? <option value="">No accessible channels</option> : null}
          {channels.map(channel => (
            <option key={channel.channelId} value={channel.channelId}>{channel.channelName}</option>
          ))}
        </select>
      </div>
      {adminMenuGroups.map(group => {
        const items = group.items.filter(item => canAccessMenuItem(item, permissions));
        if (items.length === 0) return null;
        return (
          <div key={group.label} className="mb-3">
            <div className="sidebar-section-label">{group.label}</div>
            {items.map(item => {
              const Icon = item.icon;
              return (
                <Nav.Link
                  as={NavLink}
                  to={item.path}
                  end={item.path === '/'}
                  key={item.path}
                  onClick={onHide}
                >
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
    <Offcanvas
      show={show}
      onHide={onHide}
      responsive="lg"
      className="admin-sidebar admin-sidebar-offcanvas"
    >
      <Offcanvas.Header closeButton>
        <Offcanvas.Title>MorWalPiz Admin</Offcanvas.Title>
      </Offcanvas.Header>
      <Offcanvas.Body className="p-0">{content}</Offcanvas.Body>
    </Offcanvas>
  );
}
