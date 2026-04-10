import React, { useState, useEffect } from 'react';
import { Form, useActionData, useNavigation } from 'react-router';

interface LoginError {
  message?: string;
  retryAfter?: number;
  remainingAttempts?: number;
}

export default function LoginComponent() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [retryCountdown, setRetryCountdown] = useState(0);
  const actionData = useActionData() as LoginError | undefined;
  const navigation = useNavigation();
  const isSubmitting = navigation.state === 'submitting';

  // Handle retry countdown
  useEffect(() => {
    if (actionData?.retryAfter && actionData.retryAfter > 0) {
      setRetryCountdown(Math.ceil(actionData.retryAfter));
      
      const interval = setInterval(() => {
        setRetryCountdown(prev => {
          if (prev <= 1) {
            clearInterval(interval);
            return 0;
          }
          return prev - 1;
        });
      }, 1000);

      return () => clearInterval(interval);
    }
  }, [actionData?.retryAfter]);

  const isBlocked = retryCountdown > 0;

  return (
    <div className="d-flex align-items-center justify-content-center" style={{ minHeight: '100vh', backgroundColor: '#f8f9fa' }}>
      <div className="card shadow" style={{ width: '100%', maxWidth: '400px' }}>
        <div className="card-body p-4">
          <h2 className="text-center mb-3 h3">
            Sign in to BackOffice
          </h2>
          <p className="text-center text-muted mb-4">
            Enter your credentials to access the administration panel
          </p>
        
        <Form method="post">
          <div className="mb-3">
            <label htmlFor="username" className="form-label">
              Username or Email
            </label>
            <input
              id="username"
              name="username"
              type="text"
              required
              className="form-control"
              placeholder="Enter your username or email"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              disabled={isSubmitting}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="password" className="form-label">
              Password
            </label>
            <input
              id="password"
              name="password"
              type="password"
              required
              className="form-control"
              placeholder="Enter your password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={isSubmitting}
            />
          </div>

          {/* Rate limit warning */}
          {isBlocked && (
            <div className="alert alert-warning" role="alert">
              <h5 className="alert-heading mb-2">Account Temporarily Locked</h5>
              <p className="mb-0">
                Please wait {retryCountdown} second{retryCountdown !== 1 ? 's' : ''} before trying again.
              </p>
            </div>
          )}

          {/* Login error message */}
          {actionData?.message && !isBlocked && (
            <div className="alert alert-danger" role="alert">
              <h5 className="alert-heading mb-2">Login Failed</h5>
              <p className="mb-1">{actionData.message}</p>
              {actionData.remainingAttempts !== undefined && actionData.remainingAttempts > 0 && (
                <small className="d-block mt-1">
                  {actionData.remainingAttempts} attempt{actionData.remainingAttempts !== 1 ? 's' : ''} remaining
                </small>
              )}
            </div>
          )}

          <button
            type="submit"
            disabled={isSubmitting || isBlocked}
            className="btn btn-primary w-100"
          >
            {isSubmitting ? 'Signing in...' : isBlocked ? `Blocked (${retryCountdown}s)` : 'Sign in'}
          </button>
        </Form>
        </div>
      </div>
    </div>
  );
}
