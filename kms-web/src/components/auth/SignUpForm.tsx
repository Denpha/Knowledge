import { useState } from "react";
import { Link, useNavigate } from "@tanstack/react-router";
import { ChevronLeftIcon, EyeCloseIcon, EyeIcon } from "../../icons";
import Label from "../form/Label";
import Input from "../form/input/InputField";
import Checkbox from "../form/input/Checkbox";
import Button from "../ui/button/Button";
import { useRegister } from "../../hooks/useAuth";

const FACULTIES = [
  "Engineering",
  "Science",
  "Business Administration",
  "Liberal Arts",
  "Education",
  "Medicine",
  "Nursing",
  "Public Health",
];

const POSITIONS = [
  "Professor",
  "Associate Professor",
  "Assistant Professor",
  "Lecturer",
  "Researcher",
  "Student",
  "Staff",
];

export default function SignUpForm() {
  const [showPassword, setShowPassword] = useState(false);
  const [termsAccepted, setTermsAccepted] = useState(false);
  const [errorMsg, setErrorMsg] = useState("");
  const navigate = useNavigate();
  const registerMutation = useRegister();

  const [formData, setFormData] = useState({
    email: "",
    password: "",
    confirmPassword: "",
    fullNameTh: "",
    fullNameEn: "",
    employeeCode: "",
    faculty: "",
    department: "",
    position: "",
  });

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg("");

    if (formData.password !== formData.confirmPassword) {
      setErrorMsg("Passwords do not match");
      return;
    }
    if (!termsAccepted) {
      setErrorMsg("You must accept the Terms and Conditions");
      return;
    }

    registerMutation.mutate(
      {
        email: formData.email,
        password: formData.password,
        confirmPassword: formData.confirmPassword,
        fullNameTh: formData.fullNameTh,
        fullNameEn: formData.fullNameEn || undefined,
        employeeCode: formData.employeeCode || undefined,
        faculty: formData.faculty || undefined,
        department: formData.department || undefined,
        position: formData.position || undefined,
      },
      {
        onSuccess: (data) => {
          if (data.success) {
            navigate({ to: "/" });
          } else {
            setErrorMsg(data.message || "Registration failed");
          }
        },
        onError: () => {
          setErrorMsg("Registration failed. Please try again.");
        },
      }
    );
  };

  return (
    <div className="flex flex-col flex-1 w-full overflow-y-auto lg:w-1/2 no-scrollbar">
      <div className="w-full max-w-md mx-auto mb-5 sm:pt-10">
        <Link
          to="/login"
          className="inline-flex items-center text-sm text-gray-500 transition-colors hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300"
        >
          <ChevronLeftIcon className="size-5" />
          Back to sign in
        </Link>
      </div>
      <div className="flex flex-col justify-center flex-1 w-full max-w-md mx-auto">
        <div>
          <div className="mb-5 sm:mb-8">
            <h1 className="mb-2 font-semibold text-gray-800 text-title-sm dark:text-white/90 sm:text-title-md">
              Create Account
            </h1>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              Fill in the details below to join KMS.
            </p>
          </div>
          <form onSubmit={handleSubmit}>
            <div className="space-y-5">
              {errorMsg && (
                <div className="p-3 text-sm text-error-600 bg-error-50 border border-error-200 rounded-lg dark:bg-error-500/10 dark:text-error-400 dark:border-error-500/20">
                  {errorMsg}
                </div>
              )}
              {/* Name fields */}
              <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
                <div className="sm:col-span-1">
                  <Label htmlFor="fullNameTh">
                    Full Name (Thai) <span className="text-error-500">*</span>
                  </Label>
                  <Input
                    id="fullNameTh"
                    name="fullNameTh"
                    type="text"
                    placeholder="ชื่อ นามสกุล"
                    value={formData.fullNameTh}
                    onChange={handleChange}
                  />
                </div>
                <div className="sm:col-span-1">
                  <Label htmlFor="fullNameEn">Full Name (English)</Label>
                  <Input
                    id="fullNameEn"
                    name="fullNameEn"
                    type="text"
                    placeholder="Firstname Lastname"
                    value={formData.fullNameEn}
                    onChange={handleChange}
                  />
                </div>
              </div>
              {/* Email + Employee Code */}
              <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
                <div className="sm:col-span-1">
                  <Label htmlFor="email">
                    Email <span className="text-error-500">*</span>
                  </Label>
                  <Input
                    id="email"
                    name="email"
                    type="email"
                    placeholder="you@example.com"
                    value={formData.email}
                    onChange={handleChange}
                  />
                </div>
                <div className="sm:col-span-1">
                  <Label htmlFor="employeeCode">Employee / Student Code</Label>
                  <Input
                    id="employeeCode"
                    name="employeeCode"
                    type="text"
                    placeholder="EX123456"
                    value={formData.employeeCode}
                    onChange={handleChange}
                  />
                </div>
              </div>
              {/* Faculty + Department */}
              <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
                <div className="sm:col-span-1">
                  <Label htmlFor="faculty">Faculty</Label>
                  <select
                    id="faculty"
                    name="faculty"
                    value={formData.faculty}
                    onChange={handleChange}
                    className="h-11 w-full rounded-lg border border-gray-300 bg-transparent px-4 py-2.5 text-sm text-gray-800 shadow-theme-xs focus:outline-hidden focus:ring-3 focus:border-brand-300 focus:ring-brand-500/20 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
                  >
                    <option value="">Select Faculty</option>
                    {FACULTIES.map((f) => (
                      <option key={f} value={f}>
                        {f}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="sm:col-span-1">
                  <Label htmlFor="department">Department</Label>
                  <Input
                    id="department"
                    name="department"
                    type="text"
                    placeholder="Computer Science"
                    value={formData.department}
                    onChange={handleChange}
                  />
                </div>
              </div>
              {/* Position */}
              <div>
                <Label htmlFor="position">Position</Label>
                <select
                  id="position"
                  name="position"
                  value={formData.position}
                  onChange={handleChange}
                  className="h-11 w-full rounded-lg border border-gray-300 bg-transparent px-4 py-2.5 text-sm text-gray-800 shadow-theme-xs focus:outline-hidden focus:ring-3 focus:border-brand-300 focus:ring-brand-500/20 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
                >
                  <option value="">Select Position</option>
                  {POSITIONS.map((p) => (
                    <option key={p} value={p}>
                      {p}
                    </option>
                  ))}
                </select>
              </div>
              {/* Password */}
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
                    value={formData.password}
                    onChange={handleChange}
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
              {/* Confirm Password */}
              <div>
                <Label htmlFor="confirmPassword">
                  Confirm Password <span className="text-error-500">*</span>
                </Label>
                <Input
                  id="confirmPassword"
                  name="confirmPassword"
                  type="password"
                  placeholder="Re-enter your password"
                  value={formData.confirmPassword}
                  onChange={handleChange}
                />
              </div>
              {/* Terms */}
              <div className="flex items-center gap-3">
                <Checkbox
                  checked={termsAccepted}
                  onChange={setTermsAccepted}
                />
                <p className="inline-block font-normal text-gray-500 dark:text-gray-400 text-sm">
                  By creating an account you agree to the{" "}
                  <span className="text-gray-800 dark:text-white/90">
                    Terms and Conditions
                  </span>{" "}
                  and our{" "}
                  <span className="text-gray-800 dark:text-white/90">
                    Privacy Policy
                  </span>
                </p>
              </div>
              {/* Submit */}
              <div>
                <Button
                  className="w-full"
                  size="sm"
                  disabled={registerMutation.isPending}
                >
                  {registerMutation.isPending ? "Creating account…" : "Sign Up"}
                </Button>
              </div>
            </div>
          </form>
          <div className="mt-5">
            <p className="text-sm font-normal text-center text-gray-700 dark:text-gray-400 sm:text-start">
              Already have an account?{" "}
              <Link
                to="/login"
                className="text-brand-500 hover:text-brand-600 dark:text-brand-400"
              >
                Sign In
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
