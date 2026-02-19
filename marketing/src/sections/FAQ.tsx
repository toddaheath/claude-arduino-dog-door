import { useState } from 'react'
import SectionWrapper from '../components/SectionWrapper'

const faqs = [
  {
    q: 'Does it work in the dark?',
    a: 'The ESP32-CAM has limited low-light performance. For reliable night operation we recommend adding an infrared LED module alongside the camera. The door can also be configured to lock automatically during night hours via Night Mode.',
  },
  {
    q: 'What happens in heavy rain?',
    a: 'The electronics should be housed in a weatherproof enclosure. The door mechanism itself is mechanical and operates independently of weather. We recommend a small roofed recess so the cameras stay dry.',
  },
  {
    q: 'Can it tell apart multiple dogs?',
    a: 'Yes. Each animal gets its own photo gallery, and the API uses perceptual hashing (pHash) to identify individuals. Upload at least 3–5 photos per dog from different angles for best accuracy.',
  },
  {
    q: 'What happens if an unknown animal approaches?',
    a: 'The system logs an "UnknownAnimal" event and keeps the door closed. You will see the event in the dashboard access log and can review it. You can add photos of that animal to teach the system who it is.',
  },
  {
    q: 'What if the WiFi goes down?',
    a: 'The ESP32-CAM requires a WiFi connection to the API for dog identification. Without connectivity, the door defaults to closed (safe-fail). You can configure the firmware to allow passage on timeout if you prefer.',
  },
  {
    q: 'Is my camera footage stored or shared?',
    a: 'Only the cropped photo used for identification is stored on your server for the purpose of access logging. No live video stream is recorded or sent anywhere. If you self-host, your data never leaves your infrastructure.',
  },
  {
    q: 'Is there a subscription or ongoing cost?',
    a: 'No. The one-time hardware cost is approximately $84. The software is fully open source and free. Running the API on a small VPS or home server costs whatever you pay for electricity or hosting — typically pennies per month.',
  },
  {
    q: 'How long does the build take?',
    a: 'Most builders complete the hardware assembly in an afternoon and have the software running the same day. The most time-consuming step is training the recognition system, which improves over the first week of use.',
  },
]

function FaqItem({ q, a }: { q: string; a: string }) {
  const [open, setOpen] = useState(false)
  return (
    <div className={`faq-item${open ? ' faq-item--open' : ''}`}>
      <button className="faq-question" onClick={() => setOpen(v => !v)} aria-expanded={open}>
        <span>{q}</span>
        <span className="faq-chevron">{open ? '−' : '+'}</span>
      </button>
      {open && <div className="faq-answer">{a}</div>}
    </div>
  )
}

export default function FAQ() {
  return (
    <SectionWrapper
      id="faq"
      title="Frequently Asked Questions"
      subtitle="Everything you need to know before you start building."
    >
      <div className="faq-list">
        {faqs.map(item => (
          <FaqItem key={item.q} q={item.q} a={item.a} />
        ))}
      </div>
    </SectionWrapper>
  )
}
