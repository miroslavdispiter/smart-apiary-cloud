import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { login as loginApi } from "../api/auth";
import { Input, Button, Alert } from "../components/common/ui";

export const LoginPage = () => {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      const res = await loginApi(email, password);
      login(res.data);
      const role = res.data.role;
      if (role === "Admin") navigate("/admin/users");
      else if (role === "Beekeeper") navigate("/beekeeper/apiaries");
      else if (role === "Farmer") navigate("/farmer/plots");
      else navigate("/login");
    } catch (err: any) {
      setError(err.response?.data?.message || "Invalid email or password.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-amber-50 via-orange-50 to-yellow-50 flex items-center justify-center px-4">
      {/* Background decoration */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-96 h-96 bg-amber-200 rounded-full opacity-20 blur-3xl" />
        <div className="absolute -bottom-40 -left-40 w-96 h-96 bg-orange-200 rounded-full opacity-20 blur-3xl" />
      </div>

      <div className="relative w-full max-w-md">
        {/* Card */}
        <div className="bg-white rounded-3xl shadow-2xl p-8 border border-amber-100">
          {/* Logo */}
          <div className="text-center mb-8">
            <div className="w-16 h-16 bg-gradient-to-br from-amber-400 to-orange-500 rounded-2xl flex items-center justify-center mx-auto mb-4 shadow-lg">
              <span className="text-3xl">🐝</span>
            </div>
            <h1 className="text-2xl font-bold text-gray-800">Smart Apiary</h1>
            <p className="text-gray-500 text-sm mt-1">Beekeeping management platform</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <Input
              label="Email address"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="your@email.com"
              icon={<span>✉</span>}
            />

            <Input
              label="Password"
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              icon={<span>🔒</span>}
            />

            {error && <Alert message={error} variant="error" />}

            <Button
              type="submit"
              loading={loading}
              className="w-full justify-center py-2.5"
              size="lg"
            >
              Sign in
            </Button>

            <div className="text-center">
              <Link
                to="/forgot-password"
                className="text-sm text-amber-600 hover:text-amber-800 hover:underline transition"
              >
                Forgot your password?
              </Link>
            </div>
          </form>
        </div>

        <p className="text-center text-xs text-gray-400 mt-6">
          © {new Date().getFullYear()} Smart Apiary — FTN Novi Sad
        </p>
      </div>
    </div>
  );
};
