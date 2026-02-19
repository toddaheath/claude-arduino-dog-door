import Navbar from './components/Navbar.tsx'
import Hero from './sections/Hero.tsx'
import Features from './sections/Features.tsx'
import Screenshots from './sections/Screenshots.tsx'
import HowItWorks from './sections/HowItWorks.tsx'
import QuickStart from './sections/QuickStart.tsx'
import Comparison from './sections/Comparison.tsx'
import Privacy from './sections/Privacy.tsx'
import Hardware from './sections/Hardware.tsx'
import TechStack from './sections/TechStack.tsx'
import FAQ from './sections/FAQ.tsx'
import OpenSource from './sections/OpenSource.tsx'
import Footer from './components/Footer.tsx'

export default function App() {
  return (
    <>
      <Navbar />
      <main>
        <Hero />
        <Features />
        <Screenshots />
        <HowItWorks />
        <QuickStart />
        <Comparison />
        <Privacy />
        <Hardware />
        <TechStack />
        <FAQ />
        <OpenSource />
      </main>
      <Footer />
    </>
  )
}
