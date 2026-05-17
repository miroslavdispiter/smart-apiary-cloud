import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { LoginPage } from "./pages/LoginPage";
import { ActivatePage } from "./pages/ActivatePage";
import { ForgotPasswordPage } from "./pages/ForgotPasswordPage";
import { ResetPasswordPage } from "./pages/ResetPasswordPage";
import { UsersPage } from "./pages/admin/UsersPage";
import ApiariesPage from "./pages/beekeeper/ApiariesPage";
import ApiaryDetailsPage from "./pages/beekeeper/ApiaryDetailsPage";
import TelemetryPage from "./pages/beekeeper/TelemetryPage";
import HiveDiaryPage from "./pages/beekeeper/HiveDiaryPage";
// Farmer (Clan 5)
import PlotsPage from "./pages/farmer/PlotsPage";
import PlotDetailsPage from "./pages/farmer/PlotDetailsPage";
import SprayingHistoryPage from "./pages/farmer/SprayingHistoryPage";

function App() {
    return (
        <AuthProvider>
            <BrowserRouter>
                <Routes>
                    {/* Public */}
                    <Route path="/login" element={<LoginPage />} />
                    <Route path="/activate" element={<ActivatePage />} />
                    <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                    <Route path="/reset-password" element={<ResetPasswordPage />} />

                    {/* Admin */}
                    <Route
                        path="/admin/users"
                        element={
                            <ProtectedRoute requiredRole="Admin">
                                <UsersPage />
                            </ProtectedRoute>
                        }
                    />

                    {/* Beekeeper */}
                    <Route
                        path="/beekeeper/apiaries"
                        element={
                            <ProtectedRoute requiredRole="Beekeeper">
                                <ApiariesPage />
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/beekeeper/apiaries/:id"
                        element={
                            <ProtectedRoute requiredRole="Beekeeper">
                                <ApiaryDetailsPage />
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/beekeeper/hives/:hiveId/diary"
                        element={
                            <ProtectedRoute requiredRole="Beekeeper">
                                <HiveDiaryPage />
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/beekeeper/apiaries/:apiaryId/telemetry"
                        element={
                            <ProtectedRoute requiredRole="Beekeeper">
                                <TelemetryPage />
                            </ProtectedRoute>
                        }
                    />

                    {/* Farmer (Clan 5) */}
                    <Route
                        path="/farmer/plots"
                        element={
                            <ProtectedRoute requiredRole="Farmer">
                                <PlotsPage />
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/farmer/plots/:id"
                        element={
                            <ProtectedRoute requiredRole="Farmer">
                                <PlotDetailsPage />
                            </ProtectedRoute>
                        }
                    />
                    <Route
                        path="/farmer/spraying/history"
                        element={
                            <ProtectedRoute requiredRole="Farmer">
                                <SprayingHistoryPage />
                            </ProtectedRoute>
                        }
                    />

                    {/* Fallback */}
                    <Route path="*" element={<Navigate to="/login" replace />} />
                </Routes>
            </BrowserRouter>
        </AuthProvider>
    );
}

export default App;
