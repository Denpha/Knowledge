import { useState } from "react";
import { Link, useNavigate } from "@tanstack/react-router";
import { ChevronLeftIcon, EyeCloseIcon, EyeIcon } from "../../icons";
import Label from "../form/Label";
import Input from "../form/input/InputField";
import Checkbox from "../form/input/Checkbox";
import Button from "../ui/button/Button";
import { useLogin } from "../../hooks/useAuth";
import { api } from "../../services/api";

export default function SignInForm() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [errorMsg, setErrorMsg] = useState("");

  // 2FA state
  const [twoFaPending, setTwoFaPending] = useState(false);
  const [tempToken, setTempToken] = useState("");
  const [otpCode, setOtpCode] = useState("");
  const [otpLoading, setOtpLoading] = useState(false);

  const navigate = useNavigate();
  const loginMutation = useLogin();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg("");
    loginMutation.mutate(
      { email, password },
      {
        onSuccess: (data) => {
          if (!data.success) {
            setErrorMsg(data.message || "Login failed");
            return;
          }
          const d = data.data as any;
          if (d?.requiresTwoFactor) {
            setTempToken(d.tempToken);
            setTwoFaPending(true);
          } else {
            navigate({ to: "/" });
          }
        },
        onError: () => {
          setErrorMsg("Login failed. Please try again.");
        },
      }
    );
  };

  const handleOtpSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg("");
    setOtpLoading(true);
    try {
      const res = await api.verifyTwoFactor(tempToken, otpCode);
      if (res.success && res.data && 'token' in res.data) {
        api['setToken'](res.data.token, res.data.expiresAt, res.data.refreshToken);
        navigate({ to: "/" });
      } else {
        setErrorMsg(res.message || "Invalid 2FA code");
      }
    } catch {
      setErrorMsg("Verification failed. Please try again.");
    } finally {
      setOtpLoading(false);
    }
  };

  // ── 2FA OTP Screen ─────────────────────────────────────────────────────────
  if (twoFaPending) {
    return (
      <div className="flex flex-col flex-1">
        <div className="flex flex-col justify-center flex-1 w-full max-w-md mx-auto">
          <div className="mb-6 text-center">
            <div className="mb-3 inline-flex h-14 w-14 items-center justify-center rounded-full bg-indigo-100 dark:bg-indigo-900 text-3xl">
              🔐
            </div>
            <h1 className="font-semibold text-gray-800 text-title-sm dark:text-white/90 sm:text-title-md">
              Two-Factor Authentication
            </h1>
            <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">
              กรุณากรอกรหัส 6 หลักจาก Authenticator App
            </p>
          </div>
          <form onSubmit={handleOtpSubmit} className="space-y-5">
            {errorMsg && (
              <div className="p-3 text-sm text-error-600 bg-error-50 border border-error-200 rounded-lg dark:bg-error-500/10 dark:text-error-400 dark:border-error-500/20">
                {errorMsg}
              </div>
            )}
            <div>
              <Label htmlFor="otp">รหัส OTP <span className="text-error-500">*</span></Label>
              <input
                id="otp"
                type="text"
                inputMode="numeric"
                pattern="[0-9]{6}"
                maxLength={6}
                placeholder="000000"
                value={otpCode}
                onChange={(e) => setOtpCode(e.target.value.replace(/\D/g, ""))}
                className="w-full rounded-lg border border-gray-300 bg-white px-4 py-3 text-center text-2xl font-mono tracking-[0.5em] text-gray-900 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
                autoFocus
              />
            </div>
            <Button className="w-full" size="sm" disabled={otpLoading || otpCode.length !== 6}>
              {otpLoading ? "กำลังตรวจสอบ…" : "ยืนยัน"}
            </Button>
            <button
              type="button"
              onClick={() => { setTwoFaPending(false); setErrorMsg(""); setOtpCode(""); }}
              className="w-full text-sm text-gray-500 hover:text-gray-700 dark:text-gray-400"
            >
              ← กลับหน้า Login
            </button>
          </form>
        </div>
      </div>
    );
  }

  // ── Normal Login Screen ────────────────────────────────────────────────────
  return (
    <div className="flex flex-col flex-1">
      <div className="w-full max-w-md pt-10 mx-auto">
        <Link
          to="/"
          className="inline-flex items-center text-sm text-gray-500 transition-colors hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300"
        >
          <ChevronLeftIcon className="size-5" />
          Back to dashboard
        </Link>
      </div>
      <div className="flex flex-col justify-center flex-1 w-full max-w-md mx-auto">
        <div>
          <div className="mb-5 sm:mb-8">
            <h1 className="mb-2 font-semibold text-gray-800 text-title-sm dark:text-white/90 sm:text-title-md">
              Sign In
            </h1>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              Enter your email and password to sign in!
            </p>
          </div>
          <form onSubmit={handleSubmit}>
            <div className="space-y-6">
              {errorMsg && (
                <div className="p-3 text-sm text-error-600 bg-error-50 border border-error-200 rounded-lg dark:bg-error-500/10 dark:text-error-400 dark:border-error-500/20">
                  {errorMsg}
                </div>
              )}
              <div>
                <Label htmlFor="email">
                  Email <span className="text-error-500">*</span>
                </Label>
                <Input
                  id="email"
                  name="email"
                  type="email"
                  placeholder="info@example.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
              </div>
              <div>
                <Label htmlFor="password">
                  Password <span className="text-error-500">*</span>
                </Label>
                <div className="relative">
                  <Input
                    id="password"
                    name="password"
                    type={showPassword ? "text" : "password"}
                    placeholder="Enter your password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                  />
                  <span
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute z-30 -translate-y-1/2 cursor-pointer right-4 top-1/2"
                  >
                    {showPassword ? (
                      <EyeIcon className="fill-gray-500 dark:fill-gray-400 size-5" />
                    ) : (
                      <EyeCloseIcon className="fill-gray-500 dark:fill-gray-400 size-5" />
                    )}
                  </span>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <Checkbox checked={rememberMe} onChange={setRememberMe} />
                  <span className="block font-normal text-gray-700 text-theme-sm dark:text-gray-400">
                    Keep me logged in
                  </span>
                </div>
                <a
                  href="#"
                  className="text-sm text-brand-500 hover:text-brand-600 dark:text-brand-400"
                >
                  Forgot password?
                </a>
              </div>
              <div>
                <Button
                  className="w-full"
                  size="sm"
                  disabled={loginMutation.isPending}
                >
                  {loginMutation.isPending ? "Signing in…" : "Sign in"}
                </Button>
              </div>
            </div>
          </form>
          <div className="mt-5">
            <p className="text-sm font-normal text-center text-gray-700 dark:text-gray-400 sm:text-start">
              Don&apos;t have an account?{" "}
              <Link
                to="/register"
                className="text-brand-500 hover:text-brand-600 dark:text-brand-400"
              >
                Sign Up
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
