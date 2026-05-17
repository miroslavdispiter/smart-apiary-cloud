import { useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";

interface NavbarProps {
    title?: string;
}

export const Navbar = ({ title }: NavbarProps) => {
    const { user, logout, isAdmin, isBeekeeper, isFarmer } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();

    const handleLogout = () => {
        logout();
        navigate("/login");
    };

    const navLinks = isAdmin
        ? [{ path: "/admin/users", label: "Users" }]
        : isBeekeeper
            ? [{ path: "/beekeeper/apiaries", label: "My Apiaries" }]
            : isFarmer
                ? [
                    { path: "/farmer/plots", label: "My Plots" },
                    { path: "/farmer/spraying/history", label: "Spraying History" },
                ]
                : [];

    const homeRoute = isAdmin
        ? "/admin/users"
        : isBeekeeper
            ? "/beekeeper/apiaries"
            : isFarmer
                ? "/farmer/plots"
                : "/login";

    return (
        <nav className="bg-gradient-to-r from-amber-700 to-amber-600 text-white shadow-md">
            <div className="max-w-7xl mx-auto px-6 py-3 flex items-center justify-between">
                {/* Logo + title */}
                <div className="flex items-center gap-3">
                    <button
                        onClick={() => navigate(homeRoute)}
                        className="flex items-center gap-2 hover:opacity-80 transition"
                    >
                        <span className="text-2xl">🐝</span>
                        <span className="font-bold text-lg tracking-tight">Smart Apiary</span>
                    </button>
                    {title && (
                        <>
                            <span className="text-amber-300">/</span>
                            <span className="text-amber-100 text-sm">{title}</span>
                        </>
                    )}
                </div>

                {/* Nav links + user */}
                <div className="flex items-center gap-6">
                    {navLinks.map((link) => (
                        <button
                            key={link.path}
                            onClick={() => navigate(link.path)}
                            className={`text-sm font-medium transition hover:text-amber-200 ${location.pathname === link.path ? "text-white underline" : "text-amber-100"
                                }`}
                        >
                            {link.label}
                        </button>
                    ))}

                    <div className="flex items-center gap-3 border-l border-amber-500 pl-6">
                        <div className="text-right hidden sm:block">
                            <p className="text-xs text-amber-200">{user?.email}</p>
                            <p className="text-xs text-amber-300 font-medium">{user?.role}</p>
                        </div>
                        <button
                            onClick={handleLogout}
                            className="bg-amber-800 hover:bg-amber-900 px-4 py-1.5 rounded-lg text-sm transition font-medium"
                        >
                            Logout
                        </button>
                    </div>
                </div>
            </div>
        </nav>
    );
};
