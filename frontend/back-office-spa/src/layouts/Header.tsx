import React, { useState, useEffect } from 'react';
import { Navbar, Nav, Container, Button, Dropdown } from 'react-bootstrap';
import { Menu, Bell } from 'lucide-react';
import { Link, useNavigate, useLocation } from 'react-router';
import { authService } from '../services/authService';

interface HeaderProps {
  onToggleSidebar?: () => void;
}

const Header: React.FC<HeaderProps> = ({ onToggleSidebar = () => undefined }) => {
  const navigate = useNavigate();
  const location = useLocation();
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [currentUser, setCurrentUser] = useState<any>(null);

  useEffect(() => {
    // Check authentication status
    const checkAuth = () => {
      const authenticated = authService.isAuthenticated();
      const user = authService.getUser();
      setIsAuthenticated(authenticated);
      setCurrentUser(user);

      // Redirect to login if not authenticated and not already on login page
      if (!authenticated && location.pathname !== '/login') {
        navigate('/login');
      }
    };

    checkAuth();
  }, [location.pathname, navigate]);

  const handleLogout = () => {
    authService.logout();
    setIsAuthenticated(false);
    setCurrentUser(null);
    navigate('/login');
  };

  const handleLogin = () => {
    navigate('/login');
  };

  return (
    <Navbar className="admin-header" expand="false">
      <Container fluid>
        <div className="d-flex align-items-center gap-3">
          <Button variant="link" className="header-icon-button d-lg-none" onClick={onToggleSidebar} aria-label="Open navigation">
            <Menu size={21} />
          </Button>
          <Navbar.Brand as={Link} to="/">MorWalPiz Admin</Navbar.Brand>
        </div>
        <Navbar.Toggle aria-controls="basic-navbar-nav" />
        <Navbar.Collapse id="basic-navbar-nav">
          <Nav className="ms-auto">
            {isAuthenticated ? (
              <div className="d-flex align-items-center gap-2">
                <Button variant="link" className="header-icon-button" aria-label="Notifications">
                  <Bell size={18} />
                </Button>
                <Dropdown align="end">
                  <Dropdown.Toggle variant="light" id="user-dropdown">
                    {currentUser?.username || currentUser?.email || 'User'}
                  </Dropdown.Toggle>
                  <Dropdown.Menu>
                    <Dropdown.Header>
                      <small className="text-muted">Logged in as:</small><br />
                      <strong>{currentUser?.username || currentUser?.email || 'User'}</strong>
                    </Dropdown.Header>
                    <Dropdown.Divider />
                    <Dropdown.Item as={Link} to="/profile" role="menuitem">
                      Profile
                    </Dropdown.Item>
                    <Dropdown.Divider />
                    <Dropdown.Item onClick={handleLogout} role="menuitem">
                      Logout
                    </Dropdown.Item>
                  </Dropdown.Menu>
                </Dropdown>
              </div>
            ) : (
              <Button
                variant="outline-primary"
                size="sm"
                onClick={handleLogin}
              >
                Login
              </Button>
            )}
          </Nav>
        </Navbar.Collapse>
      </Container>
    </Navbar>
  );
};

export default Header;
