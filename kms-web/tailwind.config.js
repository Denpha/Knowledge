/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      fontFamily: {
        display: ['Playfair Display', 'serif'],
        body: ['DM Sans', 'sans-serif'],
      },
      colors: {
        primary: '#1a6b3a',
        cream: '#f5faf7',
        success: '#6b9f36',
        orange: '#f9cd92',
        midnight_text: '#1a1a2e',
        emailbg: 'rgba(255,255,255,0.15)',
      },
      backgroundImage: {
        'banner-image': "url('/images/banner/background.png')",
        'newsletter': "url('/images/newsletter/hands.svg')",
      },
      boxShadow: {
        'mentor-shadow': '0px 4px 20px rgba(110, 127, 185, 0.1)',
      },
      animation: {
        float: 'float 4s ease-in-out infinite',
        fadeUp: 'fadeUp 0.7s ease forwards',
      },
      keyframes: {
        float: {
          '0%, 100%': { transform: 'translateY(0)' },
          '50%': { transform: 'translateY(-12px)' },
        },
        fadeUp: {
          from: { opacity: '0', transform: 'translateY(30px)' },
          to: { opacity: '1', transform: 'translateY(0)' },
        },
      },
    },
  },
  plugins: [],
}