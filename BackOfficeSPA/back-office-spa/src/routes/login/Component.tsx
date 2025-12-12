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
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Sign in to BackOffice
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Enter your credentials to access the administration panel
          </p>
        </div>
        <Form className="mt-8 space-y-6" method="post">
          <div className="rounded-md shadow-sm -space-y-px">
            <div>
              <label htmlFor="username" className="sr-only">
                Username
              </label>
              <input
                id="username"
                name="username"
                type="text"
                required
                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-t-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                placeholder="Username or Email"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                disabled={isSubmitting}
              />
            </div>
            <div>
              <label htmlFor="password" className="sr-only">
                Password
              </label>
              <input
                id="password"
                name="password"
                type="password"
                required
                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-b-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                placeholder="Password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                disabled={isSubmitting}
              />
            </div>
          </div>

          {/* Rate limit warning */}
          {isBlocked && (
            <div className="rounded-md bg-yellow-50 p-4">
              <div className="flex">
                <div className="ml-3">
                  <h3 className="text-sm font-medium text-yellow-800">
                    Account Temporarily Locked
                  </h3>
                  <div className="mt-2 text-sm text-yellow-700">
                    Please wait {retryCountdown} seconds before trying again.
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Login error message */}
          {actionData?.message && !isBlocked && (
            <div className="rounded-md bg-red-50 p-4">
              <div className="flex">
                <div className="ml-3">
                  <h3 className="text-sm font-medium text-red-800">
                    Login Failed
                  </h3>
                  <div className="mt-2 text-sm text-red-700">
                    {actionData.message}
                    {actionData.remainingAttempts !== undefined && actionData.remainingAttempts > 0 && (
                      <div className="mt-1 text-xs text-red-600">
                        {actionData.remainingAttempts} attempt{actionData.remainingAttempts !== 1 ? 's' : ''} remaining
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>
          )}

          <div>
            <button
              type="submit"
              disabled={isSubmitting || isBlocked}
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isSubmitting ? 'Signing in...' : isBlocked ? `Blocked (${retryCountdown}s)` : 'Sign in'}
            </button>
          </div>
        </Form>
      </div>
    </div>
  );
}
