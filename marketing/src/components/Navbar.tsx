import { useEffect, useState } from 'react'

const links = [
  { label: 'Features', href: '#features' },
  { label: 'How It Works', href: '#how-it-works' },
  { label: 'Hardware', href: '#hardware' },
  { label: 'Tech Stack', href: '#tech-stack' },
  { label: 'GitHub', href: 'https://github.com/toddlheath/claude-arduino-dog-door' },
]

export default function Navbar() {
  const [scrolled, setScrolled] = useState(false)
  const [menuOpen, setMenuOpen] = useState(false)

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 40)
    window.addEventListener('scroll', onScroll, { passive: true })
    return () => window.removeEventListener('scroll', onScroll)
  }, [])

  return (
    <nav className={`navbar${scrolled ? ' navbar--scrolled' : ''}`}>
      <a className="navbar__brand" href="#" onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}>
        <svg viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect width="32" height="32" rx="6" fill="#1e293b" />
          <path d="M8 22V10l8-4 8 4v12l-8 4-8-4z" fill="#38bdf8" opacity="0.3" />
          <path d="M16 6l8 4v12l-8 4" stroke="#38bdf8" strokeWidth="1.5" fill="none" />
          <path d="M16 6l-8 4v12l8 4" stroke="#818cf8" strokeWidth="1.5" fill="none" />
          <circle cx="16" cy="15" r="3" fill="#38bdf8" />
        </svg>
        Smart Dog Door
      </a>

      <button
        className={`navbar__hamburger${menuOpen ? ' navbar__hamburger--open' : ''}`}
        onClick={() => setMenuOpen(!menuOpen)}
        aria-label="Toggle menu"
      >
        <span />
        <span />
        <span />
      </button>

      <div className={`navbar__links${menuOpen ? ' navbar__links--open' : ''}`}>
        {links.map((link) => (
          <a
            key={link.href}
            className="navbar__link"
            href={link.href}
            onClick={() => setMenuOpen(false)}
            {...(link.href.startsWith('http') ? { target: '_blank', rel: 'noopener noreferrer' } : {})}
          >
            {link.label}
          </a>
        ))}
      </div>
    </nav>
  )
}
