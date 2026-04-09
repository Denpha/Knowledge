import { createFileRoute } from '@tanstack/react-router'
import AuthLayout from '../layout/AuthLayout'
import SignInForm from '../components/auth/SignInForm'

export const Route = createFileRoute('/login')({
  component: LoginPage,
})

function LoginPage() {
  return (
    <AuthLayout>
      <SignInForm />
    </AuthLayout>
  )
}
