import SectionWrapper from '../components/SectionWrapper'

const pillars = [
  {
    icon: '🔬',
    title: 'On-Device Detection',
    desc: 'Motion detection and dog/not-dog classification run entirely on the ESP32-CAM. No frames are sent to any cloud service for basic detection — your camera feed stays private.',
  },
  {
    icon: '🏠',
    title: 'Self-Hosted API',
    desc: 'The recognition engine and all your data run on your own server or home lab. You choose where it lives — a Raspberry Pi, a VPS, or your Kubernetes cluster.',
  },
  {
    icon: '🔓',
    title: 'No Subscriptions or Lock-In',
    desc: "There's no vendor to go out of business, no app that stops working, and no monthly fee. The source code is yours to read, modify, and keep running forever.",
  },
]

export default function Privacy() {
  return (
    <SectionWrapper
      id="privacy"
      title="Privacy by Design"
      subtitle="Your dog's comings and goings are nobody else's business."
      alt
    >
      <div className="privacy-grid">
        {pillars.map(p => (
          <div key={p.title} className="privacy-card">
            <div className="privacy-card__icon">{p.icon}</div>
            <h3 className="privacy-card__title">{p.title}</h3>
            <p className="privacy-card__desc">{p.desc}</p>
          </div>
        ))}
      </div>
    </SectionWrapper>
  )
}
