import SectionWrapper from '../components/SectionWrapper'
import PipelineStep from '../components/PipelineStep'

const steps = [
  {
    icon: '🛒',
    title: 'Order ~$84 in Parts',
    description: 'ESP32-CAM, servo motor, reed switch, door frame hardware. See the full bill of materials below.',
  },
  {
    icon: '⚡',
    title: 'Flash Firmware',
    description: 'Clone the repo and use PlatformIO to compile and flash the firmware to your ESP32-CAM in minutes.',
  },
  {
    icon: '🐳',
    title: 'Deploy the API',
    description: 'Run the backend with a single docker-compose up command, or deploy to Kubernetes with the included Helm chart.',
  },
  {
    icon: '🐾',
    title: 'Register Your Animals',
    description: 'Upload a few photos of each dog through the dashboard. The system learns their identity and starts granting access.',
  },
]

export default function QuickStart() {
  return (
    <SectionWrapper
      id="quick-start"
      title="Up and Running in a Weekend"
      subtitle="Four steps from parts to a fully operational smart dog door."
    >
      <div className="pipeline quickstart-grid">
        {steps.map((step, i) => (
          <PipelineStep
            key={i}
            step={i + 1}
            icon={step.icon}
            title={step.title}
            description={step.description}
          />
        ))}
      </div>
    </SectionWrapper>
  )
}
