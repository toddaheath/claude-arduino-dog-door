import SectionWrapper from '../components/SectionWrapper.tsx'
import FeatureCard from '../components/FeatureCard.tsx'

const features = [
  {
    icon: '\u{1F9E0}',
    title: 'On-Device AI',
    description:
      'TensorFlow Lite Micro runs a quantized MobileNet on the ESP32-CAM. Dog-or-not classification happens at the edge in milliseconds — no cloud round-trip needed for detection.',
  },
  {
    icon: '\u{1F4F7}',
    title: 'Dog Recognition',
    description:
      'Perceptual hashing (pHash) compares captured images against stored profiles so the system knows which dog is at the door — not just that a dog is there.',
  },
  {
    icon: '\u{2194}\u{FE0F}',
    title: 'Dual-Sided Detection',
    description:
      'Cameras on both sides of the door detect whether your dog is entering or exiting. The API infers transit direction automatically from the trigger side.',
  },
  {
    icon: '\u{1F6E1}\u{FE0F}',
    title: 'Multi-Sensor Safety',
    description:
      'IR break beam prevents the door from closing while an animal passes through. Reed switches confirm position. Radar and ultrasonic provide layered detection.',
  },
  {
    icon: '\u{1F5A5}\u{FE0F}',
    title: 'Cloud Dashboard',
    description:
      'A React + TypeScript SPA lets you manage animal profiles, review access logs with photos, and configure door behavior — all through a clean admin interface.',
  },
  {
    icon: '\u{1F4B0}',
    title: 'Open Source & Affordable',
    description:
      'The entire system — firmware, API, dashboard, and Helm charts — is open source. Total hardware BOM is approximately $84 using off-the-shelf components.',
  },
]

export default function Features() {
  return (
    <SectionWrapper id="features" title="Features" subtitle="Everything you need for a smart, safe dog door">
      <div className="features__grid">
        {features.map((f) => (
          <FeatureCard key={f.title} icon={f.icon} title={f.title} description={f.description} />
        ))}
      </div>
    </SectionWrapper>
  )
}
