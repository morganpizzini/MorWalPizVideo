import { useState } from "react";
import { useNavigate } from "react-router";
import { useWorkshopAuth } from "../../auth/AuthProvider";
export default function AccountLogin() {
  const navigate = useNavigate();
  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState("");
  const { login } = useWorkshopAuth();
  return (
    <main className="account-page">
      <div className="account-card">
        <p className="eyebrow">Private workspace</p>
        <h1>Sign in to Workshop</h1>
        <p>
          Use your existing MorWalPiz account. Workshop requests stay on the
          account cookie transport.
        </p>
        <form
          onSubmit={(event) => {
            event.preventDefault();
            void login({ identifier, password }).then((ok) => {
              if (ok) void navigate("/");
              else setMessage("Sign-in was not accepted.");
            });
          }}
        >
          <label>
            Username or email
            <input
              required
              value={identifier}
              onChange={(event) => setIdentifier(event.target.value)}
            />
          </label>
          <label>
            Password
            <input
              required
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </label>
          {message && (
            <p className="form-error" role="alert">
              {message}
            </p>
          )}
          <button className="button button-primary">Sign in</button>
        </form>
      </div>
    </main>
  );
}
