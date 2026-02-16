interface TechBadgeProps {
  label: string
  tier: 'edge' | 'backend' | 'frontend'
}

export default function TechBadge({ label, tier }: TechBadgeProps) {
  return <span className={`tech-badge tech-badge--${tier}`}>{label}</span>
}
