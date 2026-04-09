import { useEffect, useRef, useState } from 'react'
import { Link } from '@tanstack/react-router'
import { useIsAuthenticated, useCurrentUser } from '../hooks/useAuth'

// ── Icons ────────────────────────────────────────────────────────────────────

const ArrowRight = () => (
  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7l5 5m0 0l-5 5m5-5H6" />
  </svg>
)

const StarIcon = () => (
  <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
  </svg>
)

// ── Topbar ───────────────────────────────────────────────────────────────────

function Topbar() {
  return (
    <div className="bg-primary-800 text-white text-sm py-2 px-4 flex flex-wrap justify-between items-center gap-2">
      <div className="flex items-center gap-6">
        <span className="flex items-center gap-2">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
          </svg>
          kms@skc.rmuti.ac.th
        </span>
        <span className="hidden sm:flex items-center gap-2">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
          </svg>
          มทร.อีสาน วิทยาเขตสกลนคร
        </span>
      </div>
      <div className="flex items-center gap-2 text-primary-200 text-xs">
        ระบบจัดการองค์ความรู้ · Knowledge Management System
      </div>
    </div>
  )
}

// ── Navbar ───────────────────────────────────────────────────────────────────

function Navbar() {
  const [scrolled, setScrolled] = useState(false)
  const [menuOpen, setMenuOpen] = useState(false)
  const isAuthenticated = useIsAuthenticated()
  const userQuery = useCurrentUser()
  const user = userQuery.data?.data

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 20)
    window.addEventListener('scroll', onScroll)
    return () => window.removeEventListener('scroll', onScroll)
  }, [])

  return (
    <nav className={`sticky top-0 z-50 bg-white border-b border-gray-100 transition-all duration-300 ${scrolled ? 'shadow-md' : 'shadow-sm'}`}>
      <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
        {/* Logo */}
        <Link to="/" className="flex items-center gap-2">
          <div className="w-9 h-9 bg-primary-700 rounded-lg flex items-center justify-center">
            <svg className="w-5 h-5 text-white" fill="currentColor" viewBox="0 0 24 24">
              <path d="M12 3L1 9l11 6 9-4.91V17h2V9L12 3zM5 13.18v4L12 21l7-3.82v-4L12 17l-7-3.82z" />
            </svg>
          </div>
          <span className="font-display text-xl font-bold text-primary-800 tracking-tight">KMS</span>
        </Link>

        {/* Desktop Nav */}
        <div className="hidden md:flex items-center gap-8 text-sm font-medium text-gray-700">
          {[
            { label: 'หน้าหลัก', href: '/' },
            { label: 'บทความ', href: '/articles' },
            { label: 'สื่อ', href: '/media' },
            { label: 'โปรไฟล์', href: '/profile' },
          ].map(({ label, href }) => (
            <Link
              key={href}
              to={href}
              className="relative hover:text-primary-700 transition-colors after:content-[''] after:absolute after:-bottom-0.5 after:left-0 after:w-0 after:h-0.5 after:bg-accent after:transition-all hover:after:w-full"
            >
              {label}
            </Link>
          ))}
        </div>

        {/* Auth Buttons */}
        <div className="hidden md:flex items-center gap-3">
          {isAuthenticated ? (
            <>
              <span className="text-sm text-gray-600 font-medium">
                {user?.fullName || user?.username || 'ผู้ใช้'}
              </span>
              {(user?.roles?.some(r => r.name === 'Admin' || r.name === 'SuperAdmin')) && (
                <Link
                  to="/admin"
                  className="text-sm font-medium text-gray-700 hover:text-primary-700 transition-colors"
                >
                  แดชบอร์ด
                </Link>
              )}
            </>
          ) : (
            <>
              <Link to="/login" className="text-sm font-medium text-gray-700 hover:text-primary-700 transition-colors">
                เข้าสู่ระบบ
              </Link>
              <Link
                to="/register"
                className="bg-accent text-white text-sm font-semibold px-5 py-2 rounded-full hover:bg-orange-600 transition-colors shadow-sm"
              >
                สมัครสมาชิก
              </Link>
            </>
          )}
        </div>

        {/* Mobile menu btn */}
        <button
          className="md:hidden p-2 rounded-lg hover:bg-gray-100"
          onClick={() => setMenuOpen(v => !v)}
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>
      </div>

      {/* Mobile Menu */}
      {menuOpen && (
        <div className="md:hidden px-6 pb-4 border-t border-gray-100">
          <div className="flex flex-col gap-3 pt-4 text-sm font-medium">
            <Link to="/" className="hover:text-primary-700">หน้าหลัก</Link>
            <Link to="/articles" className="hover:text-primary-700">บทความ</Link>
            <Link to="/media" className="hover:text-primary-700">สื่อ</Link>
            <div className="flex gap-3 pt-2">
              {isAuthenticated ? (
                <Link to="/admin" className="text-gray-700 hover:text-primary-700">แดชบอร์ด</Link>
              ) : (
                <>
                  <Link to="/login" className="text-gray-700 hover:text-primary-700">เข้าสู่ระบบ</Link>
                  <Link to="/register" className="bg-accent text-white px-4 py-2 rounded-full hover:bg-orange-600 transition-colors">สมัครสมาชิก</Link>
                </>
              )}
            </div>
          </div>
        </div>
      )}
    </nav>
  )
}

// ── Hero ─────────────────────────────────────────────────────────────────────

const SLIDES = [
  {
    bg: 'linear-gradient(135deg, #0f3d1a 0%, #1a5c2a 60%, #1e7a35 100%)',
    badge: 'ระบบความรู้',
    badgeColor: 'bg-white/10',
    title: <>แบ่งปันความรู้<br /><span className="text-primary-300">เพื่อการพัฒนา</span></>,
    desc: 'ระบบจัดการองค์ความรู้ของ มทร.อีสาน วิทยาเขตสกลนคร รวบรวมและแบ่งปันความรู้ทางวิชาการ งานวิจัย และนวัตกรรมเพื่อสร้างสังคมแห่งการเรียนรู้',
    stat1Label: 'บทความ',
    stat1Value: '200+',
    stat2Label: 'ผู้ใช้งาน',
    stat2Value: '500+',
    progress: 80,
    progressLabel: 'บรรลุเป้าหมายองค์ความรู้ 80%',
    ctaPrimary: 'อ่านบทความ',
    ctaPrimaryHref: '/articles',
    ctaSecondary: 'เรียนรู้เพิ่มเติม',
    accentCircle: 'bg-primary-600',
    floatCircle: 'bg-accent',
    progressColor: 'bg-accent',
    statBg: 'bg-primary-100',
    statColor: 'text-primary-700',
    dotColor: 'bg-primary-300',
  },
  {
    bg: 'linear-gradient(135deg, #1a1040 0%, #2d1b6b 60%, #3d2090 100%)',
    badge: 'AI-Powered',
    badgeColor: 'bg-white/10',
    title: <>ค้นหาความรู้<br /><span className="text-purple-300">ด้วย AI</span></>,
    desc: 'ระบบค้นหาเชิงความหมาย (Semantic Search) และ AI Writing Assistant ช่วยให้คุณค้นหาและสร้างเนื้อหาได้อย่างชาญฉลาดและรวดเร็วยิ่งขึ้น',
    stat1Label: 'หมวดหมู่',
    stat1Value: '20+',
    stat2Label: 'ยอดเข้าชม',
    stat2Value: '10K+',
    progress: 72,
    progressLabel: 'ความแม่นยำในการค้นหา 72%',
    ctaPrimary: 'ลองใช้ AI',
    ctaPrimaryHref: '/articles',
    ctaSecondary: 'ดูการสาธิต',
    accentCircle: 'bg-purple-500',
    floatCircle: 'bg-yellow-400',
    progressColor: 'bg-yellow-400',
    statBg: 'bg-purple-100',
    statColor: 'text-purple-700',
    dotColor: 'text-yellow-400',
  },
]

function Hero() {
  const [current, setCurrent] = useState(0)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  const startTimer = () => {
    if (timerRef.current) clearInterval(timerRef.current)
    timerRef.current = setInterval(() => setCurrent(c => (c + 1) % SLIDES.length), 5500)
  }

  useEffect(() => {
    startTimer()
    return () => { if (timerRef.current) clearInterval(timerRef.current) }
  }, [])

  const goSlide = (n: number) => {
    setCurrent(n)
    startTimer()
  }

  const slide = SLIDES[current]

  return (
    <section className="relative overflow-hidden" style={{ background: slide.bg, transition: 'background 0.6s ease' }}>
      {/* Dot pattern */}
      <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle at 2px 2px, white 1px, transparent 0)', backgroundSize: '40px 40px' }} />
      {/* Decorative circles */}
      <div className={`absolute right-0 top-0 w-[600px] h-[600px] ${slide.accentCircle} opacity-20 rounded-full translate-x-1/3 -translate-y-1/4 animate-float`} />
      <div className={`absolute right-20 bottom-0 w-[300px] h-[300px] ${slide.floatCircle} opacity-10 rounded-full translate-y-1/3 animate-float`} style={{ animationDelay: '1s' }} />

      <div className="relative max-w-7xl mx-auto px-6 py-20 grid lg:grid-cols-2 gap-12 items-center min-h-[85vh]">
        <div>
          <div className={`inline-flex items-center gap-2 ${slide.badgeColor} backdrop-blur-sm text-white text-xs font-semibold px-4 py-2 rounded-full mb-6`}>
            <span className="w-2 h-2 bg-accent rounded-full" />
            {slide.badge}
          </div>
          <h1 className="font-display text-5xl lg:text-6xl font-bold text-white leading-tight mb-6 animate-fadeUp">
            {slide.title}
          </h1>
          <p className="text-white/80 text-lg leading-relaxed mb-8 max-w-md">{slide.desc}</p>

          {/* Stats box */}
          <div className="bg-white/10 backdrop-blur-sm rounded-2xl p-5 mb-8">
            <div className="flex justify-between text-white text-sm mb-3">
              <span>{slide.stat1Label}: <strong className={slide.dotColor}>{slide.stat1Value}</strong></span>
              <span>{slide.stat2Label}: <strong className="text-white">{slide.stat2Value}</strong></span>
            </div>
            <div className="bg-white/20 rounded-full h-2.5">
              <div className={`${slide.progressColor} rounded-full h-2.5 transition-all duration-1000`} style={{ width: `${slide.progress}%` }} />
            </div>
            <div className="text-white/60 text-xs mt-2">{slide.progressLabel}</div>
          </div>

          <div className="flex flex-wrap gap-4">
            <Link
              to={slide.ctaPrimaryHref}
              className="bg-accent hover:bg-orange-600 text-white font-semibold px-8 py-3.5 rounded-full transition-all shadow-lg hover:shadow-xl hover:-translate-y-0.5"
            >
              {slide.ctaPrimary}
            </Link>
            <button className="border border-white/40 text-white hover:bg-white/10 font-medium px-8 py-3.5 rounded-full transition-all">
              {slide.ctaSecondary}
            </button>
          </div>
        </div>

        {/* Right card */}
        <div className="hidden lg:flex justify-end">
          <div className="relative">
            <div className="w-80 h-96 rounded-3xl overflow-hidden border-4 border-white/20 shadow-2xl flex items-center justify-center" style={{ background: 'linear-gradient(135deg, rgba(255,255,255,0.05) 0%, rgba(255,255,255,0.15) 100%)' }}>
              <svg className="w-32 h-32 text-white/20" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 3L1 9l11 6 9-4.91V17h2V9L12 3zM5 13.18v4L12 21l7-3.82v-4L12 17l-7-3.82z" />
              </svg>
            </div>
            <div className="absolute -bottom-4 -left-4 bg-white rounded-2xl shadow-xl p-4 flex items-center gap-3">
              <div className={`w-10 h-10 ${slide.statBg} rounded-full flex items-center justify-center`}>
                <svg className={`w-5 h-5 ${slide.statColor}`} fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <div>
                <div className="text-xs text-gray-500">ผู้ใช้งาน</div>
                <div className="font-bold text-gray-800">500+</div>
              </div>
            </div>
            <div className="absolute -top-4 -right-4 bg-accent text-white rounded-2xl shadow-xl p-4 text-center">
              <div className="text-xs mb-1">บทความ</div>
              <div className="text-2xl font-bold">200+</div>
            </div>
          </div>
        </div>
      </div>

      {/* Slide dots */}
      <div className="absolute bottom-8 left-1/2 -translate-x-1/2 flex gap-2">
        {SLIDES.map((_, i) => (
          <button
            key={i}
            onClick={() => goSlide(i)}
            className={`w-2.5 h-2.5 rounded-full transition-colors cursor-pointer ${i === current ? 'bg-accent' : 'bg-gray-300'}`}
          />
        ))}
      </div>
    </section>
  )
}

// ── How Can You Use ──────────────────────────────────────────────────────────

function HowToUse() {
  const cards = [
    {
      icon: (
        <svg className="w-8 h-8 text-primary-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
        </svg>
      ),
      iconBg: 'bg-primary-50',
      bg: 'bg-white border border-gray-100',
      title: 'อ่านบทความ',
      titleColor: 'text-gray-900',
      desc: 'ค้นหาและอ่านบทความวิชาการ งานวิจัย และคลังความรู้ที่รวบรวมโดยคณาจารย์และนักวิจัยของ มทร.อีสาน',
      descColor: 'text-gray-500',
      cta: 'อ่านเลย',
      href: '/articles',
      featured: false,
    },
    {
      icon: (
        <svg className="w-8 h-8 text-primary-200" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
        </svg>
      ),
      iconBg: 'bg-primary-700',
      bg: 'bg-primary-800 shadow-xl scale-105',
      title: 'แบ่งปันความรู้',
      titleColor: 'text-white',
      desc: 'เผยแพร่บทความ งานวิจัย หรือบทเรียนของคุณเพื่อให้ชุมชนได้เรียนรู้และพัฒนาร่วมกัน',
      descColor: 'text-primary-200',
      cta: 'เริ่มเขียน',
      href: '/articles/create',
      featured: true,
    },
    {
      icon: (
        <svg className="w-8 h-8 text-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
      ),
      iconBg: 'bg-orange-50',
      bg: 'bg-white border border-gray-100',
      title: 'ค้นหาด้วย AI',
      titleColor: 'text-gray-900',
      desc: 'ใช้ระบบค้นหาเชิงความหมาย (Semantic Search) เพื่อค้นหาความรู้ที่ตรงกับความต้องการได้อย่างชาญฉลาด',
      descColor: 'text-gray-500',
      cta: 'ค้นหาเลย',
      href: '/articles',
      featured: false,
    },
  ]

  return (
    <section className="py-24 bg-gray-50">
      <div className="max-w-7xl mx-auto px-6">
        <div className="text-center mb-14">
          <span className="text-accent text-sm font-semibold uppercase tracking-widest">เริ่มต้นใช้งาน</span>
          <h2 className="font-display text-4xl font-bold text-gray-900 mt-2 mb-4">คุณจะได้รับอะไรจากระบบนี้?</h2>
          <p className="text-gray-500 max-w-xl mx-auto leading-relaxed">ค้นหา อ่าน และแบ่งปันความรู้ได้ทุกที่ทุกเวลา พร้อม AI ช่วยให้การเรียนรู้เป็นเรื่องง่าย</p>
        </div>
        <div className="grid md:grid-cols-3 gap-8">
          {cards.map((card) => (
            <div
              key={card.title}
              className={`rounded-3xl p-8 text-center transition-all duration-300 hover:-translate-y-1.5 hover:shadow-xl ${card.bg}`}
            >
              <div className={`w-16 h-16 ${card.iconBg} rounded-2xl flex items-center justify-center mx-auto mb-6`}>
                {card.icon}
              </div>
              <h3 className={`font-display text-xl font-semibold mb-3 ${card.titleColor}`}>{card.title}</h3>
              <p className={`text-sm leading-relaxed mb-6 ${card.descColor}`}>{card.desc}</p>
              <Link
                to={card.href}
                className={`inline-flex items-center gap-2 font-semibold text-sm hover:gap-3 transition-all ${card.featured ? 'text-accent' : 'text-accent'}`}
              >
                {card.cta} <ArrowRight />
              </Link>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}

// ── Stats Banner ─────────────────────────────────────────────────────────────

function StatsBanner() {
  const stats = [
    { value: '200+', label: 'บทความความรู้' },
    { value: '500+', label: 'ผู้ใช้งาน' },
    { value: '20+', label: 'หมวดหมู่วิชา' },
    { value: '10K+', label: 'ยอดเข้าชม' },
  ]
  return (
    <section className="bg-accent py-14">
      <div className="max-w-7xl mx-auto px-6 grid grid-cols-2 md:grid-cols-4 gap-8 text-white text-center">
        {stats.map(({ value, label }) => (
          <div key={label}>
            <div className="font-display text-4xl font-bold mb-1">{value}</div>
            <div className="text-orange-100 text-sm">{label}</div>
          </div>
        ))}
      </div>
    </section>
  )
}

// ── Featured Categories ──────────────────────────────────────────────────────

function FeaturedCategories() {
  const categories = [
    {
      gradient: 'linear-gradient(135deg, #166534 0%, #22c55e 100%)',
      tag: 'วิทยาศาสตร์',
      title: 'คลังความรู้ด้านวิทยาศาสตร์และเทคโนโลยีสารสนเทศ',
      articles: 45,
      goal: 65,
    },
    {
      gradient: 'linear-gradient(135deg, #7c3aed 0%, #c084fc 100%)',
      tag: 'วิศวกรรม',
      title: 'งานวิจัยและนวัตกรรมด้านวิศวกรรมศาสตร์',
      articles: 62,
      goal: 65,
    },
    {
      gradient: 'linear-gradient(135deg, #c2410c 0%, #fb923c 100%)',
      tag: 'บริหาร',
      title: 'องค์ความรู้ด้านบริหารธุรกิจและการจัดการ',
      articles: 38,
      goal: 50,
    },
  ]

  return (
    <section id="categories" className="py-24 bg-white">
      <div className="max-w-7xl mx-auto px-6">
        <div className="text-center mb-14">
          <span className="text-accent text-sm font-semibold uppercase tracking-widest">หมวดหมู่ความรู้</span>
          <h2 className="font-display text-4xl font-bold text-gray-900 mt-2 mb-4">คลังความรู้ที่น่าสนใจ</h2>
          <p className="text-gray-500 max-w-xl mx-auto leading-relaxed">รวบรวมองค์ความรู้จากหลากหลายสาขาวิชา ทั้งวิทยาศาสตร์ วิศวกรรม บริหาร และอื่นๆ อีกมากมาย</p>
        </div>
        <div className="grid md:grid-cols-3 gap-8">
          {categories.map((cat) => (
            <div key={cat.tag} className="bg-white rounded-3xl overflow-hidden border border-gray-100 shadow-sm hover:shadow-xl transition-all duration-300 group">
              <div className="relative overflow-hidden h-52" style={{ background: cat.gradient }}>
                <div className="absolute inset-0 flex items-center justify-center">
                  <svg className="w-20 h-20 text-white/20" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M12 3L1 9l11 6 9-4.91V17h2V9L12 3zM5 13.18v4L12 21l7-3.82v-4L12 17l-7-3.82z" />
                  </svg>
                </div>
                <div className="absolute inset-0 bg-gradient-to-t from-black/30 to-transparent group-hover:scale-105 transition-transform duration-500" />
                <div className="absolute top-4 left-4">
                  <span className="bg-accent text-white text-xs font-semibold px-3 py-1 rounded-full">{cat.tag}</span>
                </div>
              </div>
              <div className="p-6">
                <h3 className="font-display text-lg font-semibold text-gray-900 mb-4">{cat.title}</h3>
                <div className="mb-4">
                  <div className="flex justify-between text-sm mb-2">
                    <span className="text-gray-500">บทความ: <strong className="text-primary-700">{cat.articles}</strong></span>
                    <span className="text-gray-500">เป้าหมาย: <strong>{cat.goal}</strong></span>
                  </div>
                  <div className="bg-gray-100 rounded-full h-2">
                    <div className="bg-primary-600 rounded-full h-2 transition-all duration-1000" style={{ width: `${Math.round(cat.articles / cat.goal * 100)}%` }} />
                  </div>
                </div>
                <Link to="/articles" className="block w-full bg-primary-700 hover:bg-primary-800 text-white font-semibold py-3 rounded-xl transition-colors text-center text-sm">
                  ดูบทความ
                </Link>
              </div>
            </div>
          ))}
        </div>
        <div className="text-center mt-10">
          <Link
            to="/articles"
            className="inline-flex items-center gap-2 border-2 border-primary-700 text-primary-700 hover:bg-primary-700 hover:text-white font-semibold px-8 py-3.5 rounded-full transition-all"
          >
            ดูบทความทั้งหมด <ArrowRight />
          </Link>
        </div>
      </div>
    </section>
  )
}

// ── Latest Articles ──────────────────────────────────────────────────────────

function LatestArticles() {
  const articles = [
    {
      gradient: 'linear-gradient(135deg, #0369a1 0%, #38bdf8 100%)',
      date: 'พ.ค. 2025',
      title: 'แนวทางการพัฒนาระบบสารสนเทศเพื่อการจัดการองค์ความรู้',
      excerpt: 'การนำเทคโนโลยี AI มาประยุกต์ใช้ในการจัดการองค์ความรู้ภายในองค์กรการศึกษา',
    },
    {
      gradient: 'linear-gradient(135deg, #166534 0%, #4ade80 100%)',
      date: 'พ.ค. 2025',
      title: 'นวัตกรรมการเรียนรู้ในศตวรรษที่ 21 สำหรับมหาวิทยาลัย',
      excerpt: 'รูปแบบการเรียนการสอนที่ตอบสนองต่อความต้องการของนักศึกษาในยุคดิจิทัล',
    },
    {
      gradient: 'linear-gradient(135deg, #9d174d 0%, #f472b6 100%)',
      date: 'พ.ค. 2025',
      title: 'การประยุกต์ใช้ Machine Learning ในงานวิจัยด้านเกษตรกรรม',
      excerpt: 'การใช้ปัญญาประดิษฐ์เพื่อเพิ่มประสิทธิภาพการผลิตและลดต้นทุนในภาคเกษตร',
    },
  ]

  return (
    <section id="articles" className="py-24 bg-gray-50">
      <div className="max-w-7xl mx-auto px-6">
        <div className="text-center mb-14">
          <span className="text-accent text-sm font-semibold uppercase tracking-widest">บทความล่าสุด</span>
          <h2 className="font-display text-4xl font-bold text-gray-900 mt-2 mb-4">ความรู้ใหม่จากทีมวิจัย</h2>
          <p className="text-gray-500 max-w-xl mx-auto leading-relaxed">ติดตามบทความและงานวิจัยล่าสุดจากคณาจารย์และนักวิจัยของ มทร.อีสาน วิทยาเขตสกลนคร</p>
        </div>
        <div className="grid md:grid-cols-3 gap-8">
          {articles.map((a) => (
            <div key={a.title} className="bg-white rounded-3xl overflow-hidden shadow-sm hover:shadow-xl transition-all duration-300 group">
              <div className="relative h-48 overflow-hidden" style={{ background: a.gradient }}>
                <div className="absolute top-4 left-4 bg-white/20 backdrop-blur-sm text-white text-xs font-medium px-3 py-1 rounded-full">{a.date}</div>
                <div className="absolute inset-0 flex items-center justify-center">
                  <svg className="w-16 h-16 text-white/20" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M20 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm-7 3c1.93 0 3.5 1.57 3.5 3.5S14.93 13 13 13s-3.5-1.57-3.5-3.5S11.07 6 13 6z" />
                  </svg>
                </div>
              </div>
              <div className="p-6">
                <h3 className="font-display text-base font-semibold text-gray-900 mb-2 group-hover:text-primary-700 transition-colors">{a.title}</h3>
                <p className="text-gray-500 text-sm leading-relaxed mb-4">{a.excerpt}</p>
                <Link to="/articles" className="inline-flex items-center gap-1 text-accent text-sm font-semibold hover:gap-3 transition-all">
                  อ่านเพิ่มเติม <ArrowRight />
                </Link>
              </div>
            </div>
          ))}
        </div>
        <div className="text-center mt-10">
          <Link to="/articles" className="inline-flex items-center gap-2 border-2 border-primary-700 text-primary-700 hover:bg-primary-700 hover:text-white font-semibold px-8 py-3.5 rounded-full transition-all">
            ดูบทความทั้งหมด
          </Link>
        </div>
      </div>
    </section>
  )
}

// ── Newsletter ───────────────────────────────────────────────────────────────

function Newsletter() {
  const [email, setEmail] = useState('')
  const [sent, setSent] = useState(false)

  return (
    <section className="py-20 bg-primary-800 relative overflow-hidden">
      <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle at 2px 2px, white 1px, transparent 0)', backgroundSize: '30px 30px' }} />
      <div className="absolute right-0 bottom-0 w-72 h-72 bg-primary-600 rounded-full opacity-20 translate-x-1/3 translate-y-1/3" />
      <div className="relative max-w-2xl mx-auto px-6 text-center">
        <span className="text-primary-300 text-sm font-semibold uppercase tracking-widest">รับข่าวสาร</span>
        <h2 className="font-display text-4xl font-bold text-white mt-2 mb-4">ติดตามความรู้ใหม่ๆ ก่อนใคร</h2>
        <p className="text-primary-200 leading-relaxed mb-8">สมัครรับข่าวสารเพื่อรับการแจ้งเตือนเมื่อมีบทความใหม่ งานวิจัย และกิจกรรมทางวิชาการ</p>
        {sent ? (
          <div className="bg-white/10 rounded-2xl px-6 py-4 text-primary-100 font-medium">✅ ขอบคุณ! เราจะส่งข่าวสารถึงคุณเร็วๆ นี้</div>
        ) : (
          <div className="flex gap-3 max-w-md mx-auto">
            <input
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              placeholder="กรอกอีเมลของคุณ"
              className="flex-1 bg-white/10 border border-white/20 text-white placeholder-white/50 rounded-full px-5 py-3 text-sm focus:outline-none focus:bg-white/20 transition-all"
            />
            <button
              onClick={() => { if (email) setSent(true) }}
              className="bg-accent hover:bg-orange-600 text-white font-semibold px-7 py-3 rounded-full transition-colors whitespace-nowrap"
            >
              สมัคร
            </button>
          </div>
        )}
      </div>
    </section>
  )
}

// ── Testimonials ─────────────────────────────────────────────────────────────

function Testimonials() {
  const reviews = [
    { initials: 'อจ', bg: 'bg-primary-100', color: 'text-primary-800', name: 'ผศ.ดร.อรรถวิท', role: 'อาจารย์คณะวิทยาศาสตร์', quote: 'ระบบช่วยให้การแบ่งปันองค์ความรู้ระหว่างคณาจารย์เป็นเรื่องง่ายขึ้นมาก ข้อมูลค้นหาได้รวดเร็ว', dark: false },
    { initials: 'นศ', bg: 'bg-primary-700', color: 'text-white', name: 'นายสมชาย ใจดี', role: 'นักศึกษาปริญญาโท', quote: 'ค้นหาบทความวิจัยสำหรับทำวิทยานิพนธ์ได้ง่ายมาก ฟีเจอร์ AI ช่วยให้เจอบทความที่ต้องการได้เร็วขึ้น', dark: true },
    { initials: 'นว', bg: 'bg-orange-100', color: 'text-orange-700', name: 'นางสาวนวลนภา', role: 'นักวิจัย', quote: 'ระบบมีความสะดวกในการอัปโหลดงานวิจัยและจัดหมวดหมู่ได้ดี ทำให้การเผยแพร่ผลงานง่ายขึ้นมาก', dark: false },
    { initials: 'ผด', bg: 'bg-purple-100', color: 'text-purple-700', name: 'ผศ.ดร.ประดิษฐ์', role: 'ผู้อำนวยการสำนักวิทยบริการ', quote: 'KMS ตอบโจทย์การจัดการองค์ความรู้ขององค์กร ช่วยลดการสูญหายของความรู้สำคัญ', dark: false },
  ]

  return (
    <section className="py-24 bg-gray-50">
      <div className="max-w-7xl mx-auto px-6">
        <div className="text-center mb-14">
          <span className="text-accent text-sm font-semibold uppercase tracking-widest">เสียงจากผู้ใช้</span>
          <h2 className="font-display text-4xl font-bold text-gray-900 mt-2 mb-4">ผู้ใช้งานพูดถึงระบบ KMS</h2>
          <p className="text-gray-500 max-w-xl mx-auto leading-relaxed">ฟังประสบการณ์จริงจากคณาจารย์ นักวิจัย และนักศึกษาที่ใช้งานระบบ</p>
        </div>
        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-6">
          {reviews.map((r) => (
            <div
              key={r.name}
              className={`rounded-3xl p-6 transition-all duration-300 hover:-translate-y-1 hover:shadow-xl ${r.dark ? 'bg-primary-800 shadow-xl' : 'bg-white shadow-sm border border-gray-100'}`}
            >
              <div className={`flex mb-4 ${r.dark ? 'text-yellow-400' : 'text-accent'}`}>
                {[...Array(5)].map((_, i) => <StarIcon key={i} />)}
              </div>
              <p className={`text-sm leading-relaxed mb-6 ${r.dark ? 'text-primary-100' : 'text-gray-600'}`}>
                "{r.quote}"
              </p>
              <div className="flex items-center gap-3">
                <div className={`w-10 h-10 ${r.bg} rounded-full flex items-center justify-center ${r.color} font-bold text-sm`}>{r.initials}</div>
                <div>
                  <div className={`font-semibold text-sm ${r.dark ? 'text-white' : 'text-gray-900'}`}>{r.name}</div>
                  <div className={`text-xs ${r.dark ? 'text-primary-300' : 'text-gray-400'}`}>{r.role}</div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}

// ── CTA ──────────────────────────────────────────────────────────────────────

function CTA() {
  return (
    <section className="py-24 bg-white">
      <div className="max-w-7xl mx-auto px-6">
        <div className="bg-primary-800 rounded-[2.5rem] overflow-hidden relative">
          <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle at 2px 2px, white 1px, transparent 0)', backgroundSize: '30px 30px' }} />
          <div className="absolute right-0 top-0 w-96 h-96 bg-primary-600 opacity-20 rounded-full translate-x-1/3 -translate-y-1/3 animate-float" />
          <div className="relative px-12 py-16 text-center">
            <span className="text-primary-300 text-sm font-semibold uppercase tracking-widest">ร่วมกับเรา</span>
            <h2 className="font-display text-4xl font-bold text-white mt-2 mb-4">เริ่มต้นแบ่งปันความรู้วันนี้</h2>
            <p className="text-primary-200 max-w-xl mx-auto leading-relaxed mb-8">
              ร่วมเป็นส่วนหนึ่งของชุมชนนักวิชาการและผู้เรียนรู้ของ มทร.อีสาน วิทยาเขตสกลนคร เพื่อสร้างสังคมแห่งการแบ่งปันองค์ความรู้ที่ยั่งยืน
            </p>
            <div className="flex flex-wrap justify-center gap-4">
              <Link
                to="/register"
                className="bg-accent hover:bg-orange-600 text-white font-semibold px-10 py-4 rounded-full transition-all shadow-lg hover:-translate-y-0.5 hover:shadow-xl"
              >
                สมัครสมาชิกฟรี
              </Link>
              <Link
                to="/articles"
                className="border-2 border-white/30 text-white hover:bg-white/10 font-medium px-10 py-4 rounded-full transition-all"
              >
                ดูบทความ
              </Link>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

// ── Footer ───────────────────────────────────────────────────────────────────

function Footer() {
  return (
    <footer id="contact" className="bg-gray-900 text-gray-300 pt-16 pb-8">
      <div className="max-w-7xl mx-auto px-6">
        <div className="grid md:grid-cols-4 gap-10 mb-12">
          {/* Brand */}
          <div className="md:col-span-1">
            <div className="flex items-center gap-2 mb-4">
              <div className="w-9 h-9 bg-primary-600 rounded-lg flex items-center justify-center">
                <svg className="w-5 h-5 text-white" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 3L1 9l11 6 9-4.91V17h2V9L12 3zM5 13.18v4L12 21l7-3.82v-4L12 17l-7-3.82z" />
                </svg>
              </div>
              <span className="font-display text-xl font-bold text-white">KMS</span>
            </div>
            <p className="text-gray-400 text-sm leading-relaxed mb-5">
              ระบบจัดการองค์ความรู้ มหาวิทยาลัยเทคโนโลยีราชมงคลอีสาน วิทยาเขตสกลนคร
            </p>
          </div>

          {/* Quick Links */}
          <div>
            <h4 className="text-white font-semibold mb-4">ลิงก์ด่วน</h4>
            <ul className="space-y-2 text-sm">
              {[
                { label: 'หน้าหลัก', href: '/' },
                { label: 'บทความ', href: '/articles' },
                { label: 'สื่อ', href: '/media' },
                { label: 'เข้าสู่ระบบ', href: '/login' },
                { label: 'สมัครสมาชิก', href: '/register' },
              ].map(({ label, href }) => (
                <li key={href}>
                  <Link to={href} className="hover:text-primary-400 transition-colors">{label}</Link>
                </li>
              ))}
            </ul>
          </div>

          {/* Categories */}
          <div>
            <h4 className="text-white font-semibold mb-4">หมวดหมู่</h4>
            <ul className="space-y-2 text-sm">
              {['วิทยาศาสตร์และเทคโนโลยี', 'วิศวกรรมศาสตร์', 'บริหารธุรกิจ', 'เกษตรศาสตร์', 'ศิลปศาสตร์'].map(cat => (
                <li key={cat}>
                  <Link to="/articles" className="hover:text-primary-400 transition-colors">{cat}</Link>
                </li>
              ))}
            </ul>
          </div>

          {/* Contact Info */}
          <div>
            <h4 className="text-white font-semibold mb-4">ติดต่อเรา</h4>
            <div className="space-y-3 text-sm">
              <div className="flex items-start gap-3">
                <svg className="w-4 h-4 text-primary-400 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                <span>เลขที่ 199 ถ.พังโคน ต.พังโคน อ.พังโคน จ.สกลนคร 47160</span>
              </div>
              <div className="flex items-center gap-3">
                <svg className="w-4 h-4 text-primary-400 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
                <a href="mailto:kms@skc.rmuti.ac.th" className="hover:text-primary-400 transition-colors">kms@skc.rmuti.ac.th</a>
              </div>
              <div className="flex items-center gap-3">
                <svg className="w-4 h-4 text-primary-400 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
                </svg>
                <span>042-741-411</span>
              </div>
            </div>
          </div>
        </div>

        <div className="border-t border-gray-800 pt-8 flex flex-wrap justify-between items-center gap-4 text-sm text-gray-500">
          <span>© {new Date().getFullYear()} KMS — มทร.อีสาน วิทยาเขตสกลนคร. สงวนลิขสิทธิ์</span>
          <div className="flex gap-6">
            <a href="#" className="hover:text-gray-300 transition-colors">นโยบายความเป็นส่วนตัว</a>
            <a href="#" className="hover:text-gray-300 transition-colors">เงื่อนไขการใช้งาน</a>
          </div>
        </div>
      </div>
    </footer>
  )
}

// ── Main LandingPage ─────────────────────────────────────────────────────────

export default function LandingPage() {
  return (
    <div className="bg-white text-gray-800 overflow-x-hidden font-body">
      <Topbar />
      <Navbar />
      <Hero />
      <HowToUse />
      <StatsBanner />
      <FeaturedCategories />
      <LatestArticles />
      <Newsletter />
      <Testimonials />
      <CTA />
      <Footer />
    </div>
  )
}
