import { createFileRoute } from '@tanstack/react-router'
import AuthLayout from '../layout/AuthLayout'
import SignUpForm from '../components/auth/SignUpForm'

export const Route = createFileRoute('/register')({
  component: RegisterPage,
})

function RegisterPage() {
  return (
    <AuthLayout>
      <SignUpForm />
    </AuthLayout>
  )
}
