import { useState, useEffect, useRef } from 'react'
import { Link } from '@tanstack/react-router'
import SliderLib from 'react-slick'
import 'slick-carousel/slick/slick.css'
import 'slick-carousel/slick/slick-theme.css'
import { Icon } from '@iconify/react'
import { useIsAuthenticated, useCurrentUser, useLogout } from '../hooks/useAuth'

// react-slick ships CJS — unwrap .default when Vite resolves the module as an object
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const Slider = ((SliderLib as any).default ?? SliderLib) as typeof SliderLib

// ─── Data ────────────────────────────────────────────────────────────────────

const NAV_LINKS = [
  { label: 'หน้าแรก', href: '#Home' },
  { label: 'หมวดหมู่', href: '#categories-section' },
  { label: 'บทความ', href: '#articles-section' },
  { label: 'ผู้เชี่ยวชาญ', href: '#mentors-section' },
  { label: 'รีวิว', href: '#testimonial-section' },
]

const CATEGORIES = [
  { name: 'Web Development', icon: 'tabler:world-www', color: 'bg-blue-100 text-blue-700', count: 24, category: 'webdevelopment' },
  { name: 'Mobile Development', icon: 'tabler:device-mobile', color: 'bg-purple-100 text-purple-700', count: 18, category: 'mobiledevelopment' },
  { name: 'Data Science', icon: 'tabler:chart-bar', color: 'bg-green-100 text-green-700', count: 15, category: 'datascience' },
  { name: 'Cloud Computing', icon: 'tabler:cloud', color: 'bg-orange-100 text-orange-700', count: 12, category: 'cloudcomputing' },
]

const ARTICLES = [
  {
    title: 'การพัฒนาเว็บแอปพลิเคชันด้วย React',
    category: 'Web Development',
    imageSrc: '/images/courses/coursesOne.svg',
    price: 'ฟรี',
    profession: 'เรียนรู้การสร้าง UI ด้วย React และ TypeScript',
    tag: 'webdevelopment',
  },
  {
    title: 'Node.js Backend Development',
    category: 'Web Development',
    imageSrc: '/images/courses/coursesTwo.svg',
    price: 'ฟรี',
    profession: 'Backend ด้วย Node.js และ Express',
    tag: 'webdevelopment',
  },
  {
    title: 'PostgreSQL Database Design',
    category: 'Data Science',
    imageSrc: '/images/courses/coursesThree.svg',
    price: 'ฟรี',
    profession: 'ออกแบบฐานข้อมูลเชิงสัมพันธ์',
    tag: 'datascience',
  },
  {
    title: 'React Native Mobile App',
    category: 'Mobile Development',
    imageSrc: '/images/courses/coursesFour.svg',
    price: 'ฟรี',
    profession: 'พัฒนา Mobile App ด้วย React Native',
    tag: 'mobiledevelopment',
  },
]

const MENTORS = [
  { name: 'อาจารย์ด้าน AI', imageSrc: '/images/mentor/boy1.svg', fullName: 'ผศ.ดร. สมชาย ใจดี' },
  { name: 'Web Development Expert', imageSrc: '/images/mentor/boy2.svg', fullName: 'อ.วิชัย พัฒนศักดิ์' },
  { name: 'Data Science Instructor', imageSrc: '/images/mentor/boy3.svg', fullName: 'ผศ. สุภา วิทยาการ' },
  { name: 'Cloud Architecture', imageSrc: '/images/mentor/boy4.svg', fullName: 'อ.ธีรพงษ์ คลาวด์' },
  { name: 'Mobile Development', imageSrc: '/images/mentor/boy5.svg', fullName: 'อ.พรชัย โมบาย' },
  { name: 'UX/UI Design', imageSrc: '/images/mentor/girl1.svg', fullName: 'อ.สุดา ดีไซน์' },
]

const TESTIMONIALS = [
  {
    profession: 'นักศึกษาปริญญาตรี ปี 4',
    name: 'สมศรี ใจงาม',
    imgSrc: '/images/testimonial/user-1.jpg',
    detail: 'KMS ช่วยให้หาข้อมูลสำหรับทำวิทยานิพนธ์ได้ง่ายมาก มีเนื้อหาครบถ้วนและอัปเดตล่าสุด',
  },
  {
    profession: 'อาจารย์ภาควิชาคอมพิวเตอร์',
    name: 'ดร. วิชัย ศรีสวัสดิ์',
    imgSrc: '/images/testimonial/user-2.jpg',
    detail: 'ระบบจัดการความรู้นี้ช่วยให้อาจารย์แบ่งปันเอกสารและงานวิจัยได้อย่างสะดวก',
  },
  {
    profession: 'นักศึกษาบัณฑิตศึกษา',
    name: 'จิราพร ทองดี',
    imgSrc: '/images/testimonial/user-3.jpg',
    detail: 'ชอบฟีเจอร์ค้นหาด้วย AI มากเลยครับ ช่วยประหยัดเวลาในการหาข้อมูลที่เกี่ยวข้องได้มาก',
  },
  {
    profession: 'เจ้าหน้าที่วิชาการ',
    name: 'ประสิทธิ์ มั่นคง',
    imgSrc: '/images/testimonial/user-1.jpg',
    detail: 'ระบบใช้งานง่าย รวดเร็ว และมีประสิทธิภาพในการจัดเก็บและเรียกค้นข้อมูล',
  },
]

const PARTNER_LOGOS = [
  '/images/slickCompany/microsoft.svg',
  '/images/slickCompany/google.svg',
  '/images/slickCompany/airbnb.svg',
  '/images/slickCompany/hubspot.svg',
  '/images/slickCompany/walmart.svg',
  '/images/slickCompany/fedex.svg',
]

// ─── Sub-components ───────────────────────────────────────────────────────────

function Logo() {
  return (
    <Link to="/" className="flex items-center gap-2">
      <div className="bg-primary rounded-lg p-2">
        <Icon icon="tabler:brain" className="text-white text-2xl" />
      </div>
      <span className="font-bold text-xl text-primary">KMS</span>
    </Link>
  )
}

function Header() {
  const [sticky, setSticky] = useState(false)
  const [navOpen, setNavOpen] = useState(false)
  const isAuthenticated = useIsAuthenticated()
  const { data: userRes } = useCurrentUser()
  const logoutMutation = useLogout()
  const user = userRes?.data
  const isAdmin = user?.roles?.some(r => r.name === 'Admin' || r.name === 'SuperAdmin')

  useEffect(() => {
    const onScroll = () => setSticky(window.scrollY >= 10)
    window.addEventListener('scroll', onScroll)
    return () => window.removeEventListener('scroll', onScroll)
  }, [])

  return (
    <header className={`fixed top-0 z-40 w-full transition-all duration-300 ${sticky ? 'shadow-lg bg-white py-4' : 'shadow-none py-4'}`}>
      <div className="container mx-auto max-w-7xl px-4 flex items-center justify-between">
        <Logo />
        <nav className="hidden lg:flex items-center gap-8 ml-14">
          {NAV_LINKS.map((item, i) => (
            <a key={i} href={item.href} className="text-gray-700 hover:text-primary font-medium transition-colors">{item.label}</a>
          ))}
          {isAdmin && (
            <Link to="/admin" className="text-primary font-semibold">แดชบอร์ด</Link>
          )}
        </nav>
        <div className="flex items-center gap-4">
          {isAuthenticated ? (
            <>
              <span className="hidden lg:block text-sm text-gray-600">{user?.fullName || user?.username}</span>
              <button
                onClick={() => logoutMutation.mutate(undefined, { onSettled: () => window.location.assign('/login') })}
                className="hidden lg:block bg-transparent text-primary border hover:bg-primary border-primary hover:text-white duration-300 px-6 py-2 rounded-lg cursor-pointer"
              >
                ออกจากระบบ
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="hidden lg:block bg-transparent text-primary border hover:bg-primary border-primary hover:text-white duration-300 px-6 py-2 rounded-lg">
                เข้าสู่ระบบ
              </Link>
              <Link to="/register" className="hidden lg:block bg-primary text-white hover:bg-transparent hover:text-primary border border-primary px-6 py-2 rounded-lg duration-300">
                สมัครสมาชิก
              </Link>
            </>
          )}
          <button
            onClick={() => setNavOpen(!navOpen)}
            className="block lg:hidden p-2 rounded-lg"
            aria-label="เมนู"
          >
            <span className="block w-6 h-0.5 bg-black" />
            <span className="block w-6 h-0.5 bg-black mt-1.5" />
            <span className="block w-6 h-0.5 bg-black mt-1.5" />
          </button>
        </div>
      </div>

      {/* Mobile overlay */}
      {navOpen && <div className="fixed inset-0 bg-black/50 z-40" onClick={() => setNavOpen(false)} />}
      <div className={`lg:hidden fixed top-0 right-0 h-full w-72 bg-white shadow-lg transform transition-transform duration-300 ${navOpen ? 'translate-x-0' : 'translate-x-full'} z-50`}>
        <div className="flex items-center justify-between p-4">
          <Logo />
          <button onClick={() => setNavOpen(false)} className="bg-black/20 rounded-full p-1">
            <Icon icon="material-symbols:close-rounded" width={24} height={24} />
          </button>
        </div>
        <nav className="flex flex-col p-4 gap-2">
          {NAV_LINKS.map((item, i) => (
            <a key={i} href={item.href} onClick={() => setNavOpen(false)} className="py-2 text-gray-700 hover:text-primary font-medium">{item.label}</a>
          ))}
          <div className="mt-4 flex flex-col gap-3">
            {isAuthenticated ? (
              <button onClick={() => logoutMutation.mutate(undefined, { onSettled: () => window.location.assign('/login') })} className="bg-primary text-white px-4 py-2 rounded-lg border border-primary hover:text-primary hover:bg-transparent transition duration-300">
                ออกจากระบบ
              </button>
            ) : (
              <>
                <Link to="/login" onClick={() => setNavOpen(false)} className="bg-primary text-white px-4 py-2 rounded-lg border border-primary hover:text-primary hover:bg-transparent transition duration-300 text-center">เข้าสู่ระบบ</Link>
                <Link to="/register" onClick={() => setNavOpen(false)} className="bg-primary text-white px-4 py-2 rounded-lg border border-primary hover:text-primary hover:bg-transparent transition duration-300 text-center">สมัครสมาชิก</Link>
              </>
            )}
          </div>
        </nav>
      </div>
    </header>
  )
}

function Hero() {
  const [selectedCat, setSelectedCat] = useState('Web Development')

  return (
    <section id="Home" className="bg-banner-image bg-cover bg-center pt-28 pb-20">
      <div className="relative px-6 lg:px-8">
        <div className="container mx-auto max-w-7xl">
          <div className="flex flex-col gap-4 text-center">
            <h1 className="leading-tight font-bold tracking-tight max-w-4xl mx-auto text-black md:text-6xl sm:text-5xl text-4xl">
              พัฒนาทักษะความรู้ <br /> ด้วยระบบ KMS มทร.อีสาน
            </h1>
            <p className="text-lg leading-8 text-black">
              แบ่งปันและค้นหาความรู้จากบทความของอาจารย์และนักศึกษา
            </p>
            <div className="backdrop-blur-md bg-white/30 border border-white/30 rounded-lg shadow-lg p-6 w-fit mx-auto">
              <div className="flex items-center justify-center gap-8">
                <div className="hidden sm:flex -space-x-2">
                  {[1, 2, 3, 4, 5].map(i => (
                    <div key={i} className="inline-block h-12 w-12 rounded-full ring-2 ring-white bg-primary/20 flex items-center justify-center text-primary font-bold overflow-hidden">
                      <Icon icon="tabler:user" className="text-2xl text-primary/60" />
                    </div>
                  ))}
                </div>
                <div>
                  <div className="flex justify-center items-center gap-1">
                    <h3 className="text-2xl font-semibold">4.8</h3>
                    <img src="/images/banner/Stars.svg" alt="stars" className="w-24" />
                  </div>
                  <h3 className="text-sm text-gray-600">ความพึงพอใจจากผู้ใช้งาน 500+ คน</h3>
                </div>
              </div>
            </div>
          </div>

          {/* Search bar */}
          <div className="mx-auto max-w-4xl mt-12 p-6 bg-white rounded-lg shadow-lg">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-8">
              <div className="col-span-3">
                <p className="text-sm text-gray-500 mb-1">ต้องการเรียนรู้เรื่องอะไร?</p>
                <select
                  value={selectedCat}
                  onChange={e => setSelectedCat(e.target.value)}
                  className="w-full text-lg font-semibold py-2 border-0 focus:outline-none cursor-pointer"
                >
                  {CATEGORIES.map(c => <option key={c.category} value={c.name}>{c.name}</option>)}
                </select>
              </div>
              <div className="col-span-3">
                <p className="text-sm text-gray-500 mb-1">ค้นหาบทความ</p>
                <div className="flex items-center gap-2">
                  <Icon icon="tabler:search" className="text-gray-400 text-xl" />
                  <input
                    type="text"
                    placeholder="พิมพ์คำค้นหา..."
                    className="w-full text-lg font-semibold py-2 border-0 focus:outline-none"
                  />
                </div>
              </div>
              <div className="col-span-3 sm:col-span-2 mt-2">
                <Link to="/articles">
                  <button className="bg-primary w-full hover:bg-transparent hover:text-primary duration-300 border border-primary text-white font-bold py-4 px-3 rounded-sm cursor-pointer">
                    ค้นหา
                  </button>
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

function Partners() {
  const settings = {
    dots: false, infinite: true, slidesToShow: 4, slidesToScroll: 1,
    arrows: false, autoplay: true, speed: 2000, autoplaySpeed: 2000, cssEase: 'linear',
    responsive: [
      { breakpoint: 1024, settings: { slidesToShow: 4 } },
      { breakpoint: 700, settings: { slidesToShow: 2 } },
      { breakpoint: 500, settings: { slidesToShow: 1 } },
    ],
  }
  return (
    <section className="py-14">
      <div className="container mx-auto max-w-7xl px-4">
        <h2 className="text-lg mb-10 text-black/40 text-center">เครือข่ายพันธมิตรและเทคโนโลยีที่ใช้</h2>
        <Slider {...settings}>
          {PARTNER_LOGOS.map((src, i) => (
            <div key={i}>
              <img src={src} alt={`partner-${i}`} className="h-10 w-auto mx-auto object-contain opacity-60 hover:opacity-100 transition-opacity" />
            </div>
          ))}
        </Slider>
      </div>
    </section>
  )
}

type CategoryKey = 'webdevelopment' | 'mobiledevelopment' | 'datascience' | 'cloudcomputing' | 'all'

function Articles() {
  const [selected, setSelected] = useState<CategoryKey>('webdevelopment')

  const tabs: { key: CategoryKey; label: string }[] = [
    { key: 'webdevelopment', label: 'Web Dev' },
    { key: 'mobiledevelopment', label: 'Mobile Dev' },
    { key: 'datascience', label: 'Data Science' },
    { key: 'cloudcomputing', label: 'Cloud' },
  ]

  const filtered = ARTICLES.filter(a => a.tag === selected)

  return (
    <section id="articles-section" className="py-14">
      <div className="container mx-auto max-w-7xl px-4">
        <div className="flex flex-col sm:flex-row gap-4 justify-between sm:items-center mb-8">
          <h2 className="font-bold tracking-tight text-black sm:text-5xl text-4xl">
            บทความความรู้ <br /> ในคลังของเรา
          </h2>
          <Link to="/articles">
            <button className="bg-transparent cursor-pointer hover:bg-primary text-primary font-semibold hover:text-white py-3 px-4 border border-primary hover:border-transparent rounded-sm duration-300">
              ดูบทความทั้งหมด
            </button>
          </Link>
        </div>

        {/* Category tabs */}
        <div className="flex flex-wrap gap-3 mb-8" id="categories-section">
          {tabs.map(t => (
            <button
              key={t.key}
              onClick={() => setSelected(t.key)}
              className={`flex items-center gap-2 px-5 py-2 rounded-sm border font-medium transition-colors cursor-pointer ${selected === t.key ? 'bg-primary text-white border-primary' : 'border-primary text-primary hover:bg-primary hover:text-white'}`}
            >
              {t.label}
            </button>
          ))}
        </div>

        {/* Article cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
          {filtered.map((item, i) => (
            <div key={i} className="shadow-lg rounded-xl group flex flex-col">
              <div className="overflow-hidden rounded-t-xl bg-gray-100">
                <img
                  src={item.imageSrc}
                  alt={item.title}
                  className="h-48 w-full object-cover group-hover:scale-110 transition duration-300"
                />
              </div>
              <div className="p-4 flex flex-col justify-between gap-3 flex-1">
                <div className="flex flex-col gap-3">
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-normal text-gray-500">{item.category}</p>
                    <span className="text-sm font-semibold text-success-land border-2 border-success-land rounded-md px-2 py-0.5">{item.price}</span>
                  </div>
                  <Link to="/articles">
                    <p className="text-base font-semibold group-hover:text-primary transition-colors cursor-pointer">{item.profession}</p>
                  </Link>
                </div>
                <div className="flex items-center justify-between text-sm text-gray-500 pt-2 border-t border-gray-100">
                  <div className="flex items-center gap-1">
                    <Icon icon="tabler:clock" className="text-base" />
                    <span>5 นาที</span>
                  </div>
                  <div className="flex items-center gap-1">
                    <Icon icon="tabler:eye" className="text-base" />
                    <span>1.2k</span>
                  </div>
                  <div className="flex items-center gap-1">
                    <Icon icon="tabler:heart" className="text-base" />
                    <span>48</span>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}

function Mentors() {
  return (
    <section id="mentors-section" className="py-14">
      <div className="container mx-auto max-w-7xl px-4">
        <div className="flex flex-col sm:flex-row gap-4 justify-between sm:items-center mb-8">
          <h2 className="font-bold tracking-tight text-black sm:text-5xl text-4xl">
            ผู้เชี่ยวชาญและ <br /> อาจารย์ผู้สอน
          </h2>
          <button className="bg-transparent cursor-pointer hover:bg-primary text-primary font-semibold hover:text-white py-3 px-4 border border-primary hover:border-transparent rounded-sm duration-300">
            ดูทั้งหมด
          </button>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {MENTORS.map((item, i) => (
            <div key={i} className="group relative shadow-lg rounded-xl overflow-hidden">
              <div className="h-64 w-full overflow-hidden bg-gray-200">
                <img src={item.imageSrc} alt={item.name} className="h-full w-full object-cover object-center" />
              </div>
              <div className="my-4 flex justify-center">
                <div>
                  <div className="border border-white rounded-lg -mt-8 bg-white p-2 shadow-md flex items-center justify-center">
                    <span className="text-sm text-gray-700 text-center">{item.name}</span>
                  </div>
                  <p className="mt-3 text-xl font-semibold text-black/80 text-center">{item.fullName}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}

function Testimonials() {
  const settings = {
    dots: true, infinite: true, slidesToShow: 3, slidesToScroll: 1,
    arrows: false, autoplay: false, cssEase: 'linear',
    responsive: [
      { breakpoint: 1200, settings: { slidesToShow: 2 } },
      { breakpoint: 800, settings: { slidesToShow: 1 } },
    ],
  }
  return (
    <section id="testimonial-section" className="bg-cream py-14">
      <div className="container mx-auto max-w-7xl px-4">
        <div className="flex flex-col sm:flex-row gap-5 justify-between sm:items-center mb-6">
          <h2 className="font-bold tracking-tight text-black sm:text-5xl text-4xl">
            เสียงจากผู้ใช้งาน <br /> ที่มีความสุข
          </h2>
          <button className="bg-transparent cursor-pointer hover:bg-primary text-primary font-semibold hover:text-white py-3 px-4 border border-primary hover:border-transparent rounded-sm duration-300">
            เขียนรีวิว
          </button>
        </div>
        <p className="text-lg font-medium mb-8 text-gray-600">
          เรียนรู้จากบทความของผู้เชี่ยวชาญ <br /> และนักศึกษาในมหาวิทยาลัย
        </p>
        <Slider {...settings}>
          {TESTIMONIALS.map((item, i) => (
            <div key={i}>
              <div className="bg-white m-4 pt-8 px-8 pb-8 text-center rounded-lg shadow-sm">
                <div className="flex justify-center items-center mb-4">
                  <img src={item.imgSrc} alt={item.name} className="h-16 w-16 rounded-full ring-2 ring-primary/30 object-cover" />
                </div>
                <p className="text-sm text-gray-500 pb-1">{item.profession}</p>
                <p className="text-xl font-semibold pb-3">{item.name}</p>
                <div className="flex justify-center mb-4">
                  {[...Array(5)].map((_, i) => (
                    <Icon key={i} icon="tabler:star-filled" className="text-yellow-400 text-lg" />
                  ))}
                </div>
                <p className="text-base font-medium leading-7 text-gray-600">{item.detail}</p>
              </div>
            </div>
          ))}
        </Slider>
      </div>
    </section>
  )
}

function Newsletter() {
  const [email, setEmail] = useState('')
  const [sent, setSent] = useState(false)

  return (
    <section id="join-section" className="-mb-64 py-14">
      <div className="relative z-10">
        <div className="mx-auto max-w-7xl py-16 md:py-24 px-8 lg:px-24 bg-si-orange rounded-lg" style={{ backgroundImage: "url('/images/newsletter/hands.svg')", backgroundSize: 'contain', backgroundRepeat: 'no-repeat', backgroundPosition: 'right bottom' }}>
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
            <div>
              <h3 className="text-4xl font-bold mb-3">รับข่าวสารจาก KMS</h3>
              <p className="text-lg font-medium mb-7 text-gray-700">
                สมัครรับข่าวสารบทความใหม่ กิจกรรมและอัปเดตจากมหาวิทยาลัย
              </p>
              {sent ? (
                <p className="text-primary font-semibold">✓ สมัครรับข่าวสารสำเร็จแล้ว!</p>
              ) : (
                <div className="flex gap-2">
                  <input
                    type="email"
                    value={email}
                    onChange={e => setEmail(e.target.value)}
                    className="py-4 w-full text-base px-4 bg-white rounded-lg focus:outline-none focus:border-primary focus:outline-1 border"
                    placeholder="อีเมลของคุณ"
                  />
                  <button
                    onClick={() => { if (email) setSent(true) }}
                    className="bg-primary cursor-pointer hover:bg-transparent border border-primary hover:text-primary text-white font-medium py-2 px-4 rounded-sm transition-colors whitespace-nowrap"
                  >
                    สมัคร
                  </button>
                </div>
              )}
            </div>
            <div className="hidden sm:block">
              <div className="float-right -mt-32">
                <img src="/images/newsletter/Free.svg" alt="newsletter" className="w-auto max-h-48" />
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

function Footer() {
  const FOOTER_LINKS = [
    {
      section: 'ลิงก์หลัก',
      links: [
        { label: 'หน้าแรก', href: '#Home' },
        { label: 'หมวดหมู่', href: '#categories-section' },
        { label: 'บทความ', href: '#articles-section' },
        { label: 'ผู้เชี่ยวชาญ', href: '#mentors-section' },
        { label: 'ติดต่อเรา', href: '#join-section' },
      ],
    },
    {
      section: 'ช่วยเหลือ',
      links: [
        { label: 'ศูนย์ช่วยเหลือ', href: '/' },
        { label: 'เงื่อนไขการใช้งาน', href: '/' },
        { label: 'นโยบายความเป็นส่วนตัว', href: '/' },
        { label: 'เกี่ยวกับเรา', href: '/' },
      ],
    },
  ]

  return (
    <div className="bg-primary" id="footer-section">
      <div className="container mx-auto max-w-7xl pt-72 pb-10 px-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-12 gap-16 xl:gap-8">
          <div className="col-span-4 flex flex-col gap-5">
            <div className="flex items-center gap-2">
              <div className="bg-white rounded-lg p-2">
                <Icon icon="tabler:brain" className="text-primary text-2xl" />
              </div>
              <span className="font-bold text-xl text-white">KMS</span>
            </div>
            <p className="text-white text-lg font-medium leading-7">
              ระบบจัดการความรู้ มทร.อีสาน<br />วิทยาเขตสกลนคร
            </p>
            <div className="flex gap-4">
              {['tabler:brand-facebook', 'tabler:brand-instagram', 'tabler:brand-youtube-filled', 'tabler:mail'].map((icon, i) => (
                <a key={i} href="#!" className="bg-white/20 rounded-full p-2 text-white hover:bg-white hover:text-primary duration-300">
                  <Icon icon={icon} className="text-2xl" />
                </a>
              ))}
            </div>
          </div>

          <div className="col-span-4">
            <div className="flex gap-16">
              {FOOTER_LINKS.map((section, i) => (
                <div key={i}>
                  <p className="text-white text-xl font-semibold mb-6">{section.section}</p>
                  <ul>
                    {section.links.map((link, j) => (
                      <li key={j} className="mb-3">
                        <a href={link.href} className="text-white/60 hover:text-white text-sm font-normal">{link.label}</a>
                      </li>
                    ))}
                  </ul>
                </div>
              ))}
            </div>
          </div>

          <div className="col-span-4">
            <h3 className="text-white text-xl font-semibold mb-6">ติดต่อเรา</h3>
            <div className="flex flex-col gap-3 text-white/70 text-sm">
              <div className="flex items-start gap-2">
                <Icon icon="tabler:map-pin" className="text-xl mt-0.5 flex-shrink-0" />
                <span>199 ม.3 ถ.พังโคน-วาริชภูมิ ต.พังโคน อ.พังโคน จ.สกลนคร 47160</span>
              </div>
              <div className="flex items-center gap-2">
                <Icon icon="tabler:phone" className="text-xl" />
                <span>042-771-503</span>
              </div>
              <div className="flex items-center gap-2">
                <Icon icon="tabler:mail" className="text-xl" />
                <span>kms@rmuti.ac.th</span>
              </div>
            </div>
            <div className="mt-6 relative flex">
              <input
                type="email"
                className="py-4 text-sm w-full text-white bg-white/15 rounded-md pl-4 focus:outline-none"
                placeholder="อีเมลของคุณ"
              />
              <div className="absolute inset-y-0 right-0 flex items-center pr-2">
                <button className="p-1 focus:outline-none">
                  <Icon icon="tabler:send" className="text-white text-2xl" />
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
      <div className="py-4 border-t border-white/10">
        <p className="text-center text-white/50 text-sm">
          © 2025 KMS มทร.อีสาน วิทยาเขตสกลนคร — สงวนลิขสิทธิ์
        </p>
      </div>
    </div>
  )
}

// ─── Main LandingPage ────────────────────────────────────────────────────────

export default function LandingPage() {
  const scrollRef = useRef<HTMLDivElement>(null)
  return (
    <div ref={scrollRef} className="landing-page" style={{ fontFamily: "'Kanit', sans-serif" }}>
      <Header />
      <main>
        <Hero />
        <Partners />
        <Articles />
        <Mentors />
        <Testimonials />
        <Newsletter />
      </main>
      <Footer />
    </div>
  )
}
