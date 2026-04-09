import { useState } from 'react'
import { RichTextEditor } from './components/RichTextEditor'
import { useCurrentUser, useIsAuthenticated, useLogout } from './hooks/useAuth'
import './App.css'

function App() {
  const [count, setCount] = useState(0)
  const [editorContent, setEditorContent] = useState('<p>Try editing this text!</p>')
  const isAuthenticated = useIsAuthenticated()
  const currentUserQuery = useCurrentUser()
  const logoutMutation = useLogout()

  const currentUser = currentUserQuery.data?.data
  const userLabel = currentUser?.fullName || currentUser?.username || currentUser?.email || 'Unknown user'
  const rolesText = currentUser?.roles?.map((x) => x.name).filter(Boolean).join(', ') || 'No roles'

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <h1 className="text-2xl font-bold text-gray-900">KMS</h1>
              <span className="ml-2 text-sm text-gray-500">Knowledge Management System</span>
            </div>
            <nav className="flex items-center space-x-4">
              <a href="/" className="text-gray-700 hover:text-gray-900">Home</a>
              <a href="/articles" className="text-gray-700 hover:text-gray-900">Articles</a>
              <a href="/profile" className="text-gray-700 hover:text-gray-900">Profile</a>
              <a href="/media" className="text-gray-700 hover:text-gray-900">Media</a>
              <a href="#" className="text-gray-700 hover:text-gray-900">Categories</a>

              {isAuthenticated ? (
                <>
                  <span className="rounded-full bg-emerald-100 px-3 py-1 text-xs font-semibold text-emerald-700">
                    Logged in
                  </span>
                  <span className="max-w-xs truncate text-sm text-gray-600" title={`${userLabel} (${rolesText})`}>
                    {userLabel}
                  </span>
                  <button
                    type="button"
                    onClick={() => {
                      logoutMutation.mutate(undefined, {
                        onSettled: () => {
                          window.location.assign('/login')
                        },
                      })
                    }}
                    className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
                  >
                    Logout
                  </button>
                </>
              ) : (
                <>
                  <a href="/login" className="text-blue-600 hover:text-blue-800 font-medium">Login</a>
                  <a href="/register" className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700">Sign Up</a>
                </>
              )}
            </nav>
          </div>
        </div>
      </header>

      <div className="border-b bg-gray-100">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-2 text-xs text-gray-600 sm:px-6 lg:px-8">
          <span>
            Auth Status:{' '}
            {isAuthenticated ? 'Authenticated (token present)' : 'Not authenticated'}
          </span>
          {isAuthenticated && (
            <span>
              {currentUserQuery.isLoading
                ? 'Loading profile...'
                : currentUser
                  ? `User: ${userLabel} | Roles: ${rolesText}`
                  : 'Profile unavailable'}
            </span>
          )}
        </div>
      </div>

      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          <div className="border-4 border-dashed border-gray-200 rounded-lg p-8">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Welcome to KMS</h2>
            <p className="text-gray-600 mb-6">
              Knowledge Management System for Rajamangala University of Technology Isan, Sakon Nakhon Campus
            </p>
            
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mt-8">
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-xl font-semibold text-gray-800 mb-2">Knowledge Articles</h3>
                <p className="text-gray-600">Create, manage, and share academic knowledge</p>
              </div>
              
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-xl font-semibold text-gray-800 mb-2">AI-Powered</h3>
                <p className="text-gray-600">AI writing assistant and semantic search</p>
              </div>
              
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-xl font-semibold text-gray-800 mb-2">Media Library</h3>
                <p className="text-gray-600">Upload and manage images, documents, and media</p>
              </div>
            </div>

            <div className="mt-8 text-center">
              <button
                className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-6 rounded-lg"
                onClick={() => setCount((count) => count + 1)}
              >
                Count is {count}
              </button>
              <p className="text-sm text-gray-500 mt-2">Click to test React state</p>
            </div>

            <div className="mt-8">
              <h3 className="text-xl font-semibold text-gray-800 mb-4">Rich Text Editor Demo (TipTap)</h3>
              <RichTextEditor 
                value={editorContent}
                onChange={setEditorContent}
                placeholder="Write your article content here..."
              />
              <div className="mt-4 text-sm text-gray-600">
                <p>Editor content (HTML preview):</p>
                <div className="mt-2 p-3 bg-gray-100 rounded border text-xs font-mono overflow-auto max-h-32">
                  {editorContent}
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>

      <footer className="bg-white border-t mt-8">
        <div className="max-w-7xl mx-auto px-4 py-6">
          <p className="text-center text-gray-500">
            KMS v4 - React 19 + TypeScript + Vite Frontend
          </p>
        </div>
      </footer>
    </div>
  )
}

export default App