import SectionWrapper from '../components/SectionWrapper.tsx'
import PipelineStep from '../components/PipelineStep.tsx'

const steps = [
  {
    icon: '\u{1F4E1}',
    title: 'Detect Motion',
    description: 'RCWL-0516 radar detects motion up to 7m away. HC-SR04 ultrasonic confirms proximity within 50cm.',
  },
  {
    icon: '\u{1F4F7}',
    title: 'Capture Image',
    description: 'OV2640 camera captures a 320\u00D7240 JPEG frame and passes it to the on-board ML pipeline.',
  },
  {
    icon: '\u{1F9E0}',
    title: 'Edge AI Classification',
    description: 'TFLite Micro runs a quantized MobileNet model. If confidence > 0.7, it\u2019s classified as a dog.',
  },
  {
    icon: '\u{2601}\u{FE0F}',
    title: 'Cloud Identification',
    description: 'Image is sent via HTTPS to the .NET API. Perceptual hashing identifies which dog it is.',
  },
  {
    icon: '\u{2705}',
    title: 'Access Decision',
    description: 'API checks if the identified dog is allowed. Direction (entering/exiting) is inferred from camera side.',
  },
  {
    icon: '\u{1F6AA}',
    title: 'Open Door',
    description: 'Linear actuator opens the flap. IR break beam monitors safe passage. Door auto-closes after timeout.',
  },
]

export default function HowItWorks() {
  return (
    <SectionWrapper
      id="how-it-works"
      title="How It Works"
      subtitle="From motion detection to door open in under 2 seconds"
      alt
    >
      <div className="pipeline">
        {steps.map((s, i) => (
          <PipelineStep key={s.title} step={i + 1} icon={s.icon} title={s.title} description={s.description} />
        ))}
      </div>
    </SectionWrapper>
  )
}
