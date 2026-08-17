import React from 'react';
import { Navbar, Container, Button, Dropdown } from 'react-bootstrap';
import { Menu, Bell, UserRound } from 'lucide-react';
import { Link, useNavigate } from 'react-router';
import { authService } from '../services/authService';
import { useAppStore } from '../state/appStore';
import { useChannelContext } from '../contexts/ChannelContext';

interface HeaderProps {
  onToggleSidebar?: () => void;
}

const Header: React.FC<HeaderProps> = ({ onToggleSidebar = () => undefined }) => {
  const navigate = useNavigate();
  const user = useAppStore(state => state.user);
  const { selectedChannelId } = useChannelContext();

  const handleLogout = async () => {
    await authService.logout();
    navigate('/login');
  };

  return (
    <Navbar className="admin-header">
      <Container fluid>
        <div className="d-flex align-items-center gap-3">
          <Button variant="link" className="header-icon-button d-lg-none" onClick={onToggleSidebar} aria-label="Open navigation">
            <Menu size={21} />
          </Button>
          <Navbar.Brand as={Link} to="/">MorWalPiz Admin</Navbar.Brand>
        </div>
        <div className="d-flex align-items-center gap-2 ms-auto">
          <Dropdown align="end">
            <Dropdown.Toggle variant="link" className="header-icon-button" aria-label="Notifications">
              <Bell size={19} aria-hidden="true" />
            </Dropdown.Toggle>
            <Dropdown.Menu>
              <Dropdown.Header>Notifications</Dropdown.Header>
              <Dropdown.ItemText className="text-muted">No new notifications</Dropdown.ItemText>
            </Dropdown.Menu>
          </Dropdown>
          <Dropdown align="end">
            <Dropdown.Toggle variant="link" className="header-icon-button" id="user-dropdown" aria-label="User menu">
              <UserRound size={19} aria-hidden="true" />
            </Dropdown.Toggle>
            <Dropdown.Menu>
              <Dropdown.Header>
                <small className="text-muted">Logged in as:</small><br />
                <strong>{user?.username ?? 'Authenticated user'}</strong>
              </Dropdown.Header>
              <Dropdown.Divider />
              <Dropdown.Item as={Link} to="/profile" role="menuitem">
                Profile
              </Dropdown.Item>
              {selectedChannelId ? (
                <Dropdown.Item as={Link} to="/my-channel" role="menuitem">
                  My channel
                </Dropdown.Item>
              ) : null}
              <Dropdown.Divider />
              <Dropdown.Item onClick={handleLogout} role="menuitem">
                Logout
              </Dropdown.Item>
            </Dropdown.Menu>
          </Dropdown>
        </div>
      </Container>
    </Navbar>
  );
};

export default Header;
