import { createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'

export const Route = createFileRoute('/articles')({
  component: ArticlesPage,
})

interface Article {
  id: string
  title: string
  titleEn: string
  summary: string
  status: 'draft' | 'under_review' | 'published' | 'archived'
  visibility: 'public' | 'internal' | 'restricted'
  createdAt: string
  author: string
}

function ArticlesPage() {
  const [articles] = useState<Article[]>([
    {
      id: '1',
      title: 'การวิจัยเกี่ยวกับปัญญาประดิษฐ์ในประเทศไทย',
      titleEn: 'AI Research in Thailand',
      summary: 'บทความสรุปสถานการณ์การวิจัย AI ในประเทศไทยและแนวโน้มในอนาคต',
      status: 'published',
      visibility: 'public',
      createdAt: '2026-04-01',
      author: 'ดร.สมชาย ใจดี',
    },
    {
      id: '2',
      title: 'ระบบจัดการความรู้สำหรับมหาวิทยาลัย',
      titleEn: 'Knowledge Management System for Universities',
      summary: 'การออกแบบและพัฒนาระบบจัดการความรู้ที่เหมาะสมกับสถาบันอุดมศึกษา',
      status: 'published',
      visibility: 'internal',
      createdAt: '2026-03-28',
      author: 'ผศ.ดร.สุภาพร แก้วใส',
    },
    {
      id: '3',
      title: 'เทคนิคการเขียนโปรแกรมด้วย Python',
      titleEn: 'Python Programming Techniques',
      summary: 'เทคนิคและแนวทางปฏิบัติที่ดีในการเขียนโปรแกรมด้วยภาษา Python',
      status: 'draft',
      visibility: 'restricted',
      createdAt: '2026-04-05',
      author: 'คุณวิศวะ เก่งดี',
    },
    {
      id: '4',
      title: 'การประยุกต์ใช้ Machine Learning ในภาคเกษตร',
      titleEn: 'Machine Learning Applications in Agriculture',
      summary: 'การนำเทคโนโลยี ML มาใช้ในการเกษตรเพื่อเพิ่มผลผลิตและลดต้นทุน',
      status: 'under_review',
      visibility: 'internal',
      createdAt: '2026-04-03',
      author: 'รศ.ดร.เกษตร ปลูกดี',
    },
  ])

  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('all')

  const filteredArticles = articles.filter(article => {
    const matchesSearch = article.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         article.titleEn.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         article.summary.toLowerCase().includes(searchTerm.toLowerCase())
    const matchesStatus = statusFilter === 'all' || article.status === statusFilter
    
    return matchesSearch && matchesStatus
  })

  const getStatusColor = (status: Article['status']) => {
    switch (status) {
      case 'draft': return 'bg-gray-100 text-gray-800'
      case 'under_review': return 'bg-yellow-100 text-yellow-800'
      case 'published': return 'bg-green-100 text-green-800'
      case 'archived': return 'bg-red-100 text-red-800'
      default: return 'bg-gray-100 text-gray-800'
    }
  }

  const getStatusText = (status: Article['status']) => {
    switch (status) {
      case 'draft': return 'ร่าง'
      case 'under_review': return 'รอตรวจสอบ'
      case 'published': return 'เผยแพร่แล้ว'
      case 'archived': return 'เก็บถาวร'
      default: return status
    }
  }

  const getVisibilityText = (visibility: Article['visibility']) => {
    switch (visibility) {
      case 'public': return 'สาธารณะ'
      case 'internal': return 'ภายในองค์กร'
      case 'restricted': return 'จำกัดสิทธิ์'
      default: return visibility
    }
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <h1 className="text-2xl font-bold text-gray-900">KMS</h1>
              <span className="ml-2 text-sm text-gray-500">Knowledge Articles</span>
            </div>
            <nav className="flex items-center space-x-4">
              <a href="/" className="text-gray-700 hover:text-gray-900">Home</a>
              <a href="/articles" className="text-blue-600 font-medium">Articles</a>
              <a href="/profile" className="text-gray-700 hover:text-gray-900">Profile</a>
              <a href="/media" className="text-gray-700 hover:text-gray-900">Media</a>
              <a href="/login" className="text-gray-700 hover:text-gray-900">Login</a>
            </nav>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          <div className="flex justify-between items-center mb-6">
            <h2 className="text-2xl font-bold text-gray-900">Knowledge Articles</h2>
            <a
              href="/articles/create"
              className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
            >
              Create New Article
            </a>
          </div>

          <div className="bg-white rounded-lg shadow mb-6">
            <div className="p-4 border-b">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="search" className="block text-sm font-medium text-gray-700 mb-1">
                    Search Articles
                  </label>
                  <input
                    type="text"
                    id="search"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                    placeholder="Search by title or content..."
                  />
                </div>
                <div>
                  <label htmlFor="status" className="block text-sm font-medium text-gray-700 mb-1">
                    Filter by Status
                  </label>
                  <select
                    id="status"
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                  >
                    <option value="all">All Statuses</option>
                    <option value="draft">Draft</option>
                    <option value="under_review">Under Review</option>
                    <option value="published">Published</option>
                    <option value="archived">Archived</option>
                  </select>
                </div>
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Title
                    </th>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Status
                    </th>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Visibility
                    </th>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Author
                    </th>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Created
                    </th>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {filteredArticles.map((article) => (
                    <tr key={article.id} className="hover:bg-gray-50">
                      <td className="px-6 py-4">
                        <div className="text-sm font-medium text-gray-900">{article.title}</div>
                        <div className="text-sm text-gray-500">{article.titleEn}</div>
                        <div className="text-xs text-gray-400 mt-1">{article.summary}</div>
                      </td>
                      <td className="px-6 py-4">
                        <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusColor(article.status)}`}>
                          {getStatusText(article.status)}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500">
                        {getVisibilityText(article.visibility)}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500">
                        {article.author}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500">
                        {article.createdAt}
                      </td>
                      <td className="px-6 py-4 text-sm font-medium">
                        <a href={`/articles/${article.id}`} className="text-blue-600 hover:text-blue-900 mr-3">
                          View
                        </a>
                        <a href={`/articles/${article.id}/edit`} className="text-green-600 hover:text-green-900 mr-3">
                          Edit
                        </a>
                        <button className="text-red-600 hover:text-red-900">
                          Delete
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {filteredArticles.length === 0 && (
              <div className="text-center py-12">
                <div className="text-gray-400 mb-2">No articles found</div>
                <div className="text-sm text-gray-500">Try adjusting your search or filters</div>
              </div>
            )}

            <div className="px-6 py-4 border-t">
              <div className="flex justify-between items-center">
                <div className="text-sm text-gray-500">
                  Showing {filteredArticles.length} of {articles.length} articles
                </div>
                <div className="flex space-x-2">
                  <button className="px-3 py-1 border rounded-md text-sm hover:bg-gray-50">
                    Previous
                  </button>
                  <button className="px-3 py-1 border rounded-md text-sm bg-blue-50 text-blue-600 border-blue-200">
                    1
                  </button>
                  <button className="px-3 py-1 border rounded-md text-sm hover:bg-gray-50">
                    2
                  </button>
                  <button className="px-3 py-1 border rounded-md text-sm hover:bg-gray-50">
                    Next
                  </button>
                </div>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Article Statistics</h3>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="text-center p-4 bg-blue-50 rounded-lg">
                <div className="text-2xl font-bold text-blue-600">{articles.length}</div>
                <div className="text-sm text-gray-600">Total Articles</div>
              </div>
              <div className="text-center p-4 bg-green-50 rounded-lg">
                <div className="text-2xl font-bold text-green-600">
                  {articles.filter(a => a.status === 'published').length}
                </div>
                <div className="text-sm text-gray-600">Published</div>
              </div>
              <div className="text-center p-4 bg-yellow-50 rounded-lg">
                <div className="text-2xl font-bold text-yellow-600">
                  {articles.filter(a => a.status === 'under_review').length}
                </div>
                <div className="text-sm text-gray-600">Under Review</div>
              </div>
              <div className="text-center p-4 bg-gray-50 rounded-lg">
                <div className="text-2xl font-bold text-gray-600">
                  {articles.filter(a => a.status === 'draft').length}
                </div>
                <div className="text-sm text-gray-600">Drafts</div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  )
}