interface PipelineStepProps {
  step: number
  icon: string
  title: string
  description: string
}

export default function PipelineStep({ step, icon, title, description }: PipelineStepProps) {
  return (
    <div className="pipeline-step">
      <div className="pipeline-step__number">{step}</div>
      <div className="pipeline-step__icon">{icon}</div>
      <h3 className="pipeline-step__title">{title}</h3>
      <p className="pipeline-step__desc">{description}</p>
    </div>
  )
}
