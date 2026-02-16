import Navbar from './components/Navbar.tsx'
import Hero from './sections/Hero.tsx'
import Features from './sections/Features.tsx'
import HowItWorks from './sections/HowItWorks.tsx'
import Hardware from './sections/Hardware.tsx'
import TechStack from './sections/TechStack.tsx'
import OpenSource from './sections/OpenSource.tsx'
import Footer from './components/Footer.tsx'

export default function App() {
  return (
    <>
      <Navbar />
      <main>
        <Hero />
        <Features />
        <HowItWorks />
        <Hardware />
        <TechStack />
        <OpenSource />
      </main>
      <Footer />
    </>
  )
}
