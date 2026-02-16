import SectionWrapper from '../components/SectionWrapper.tsx'
import TechBadge from '../components/TechBadge.tsx'

const tiers = [
  {
    title: 'Edge',
    titleClass: 'techstack__tier-title--edge' as const,
    tier: 'edge' as const,
    techs: ['ESP32-CAM', 'TFLite Micro', 'PlatformIO', 'C++', 'FreeRTOS'],
  },
  {
    title: 'Backend',
    titleClass: 'techstack__tier-title--backend' as const,
    tier: 'backend' as const,
    techs: ['.NET 8', 'ASP.NET Core', 'EF Core 9', 'PostgreSQL', 'Docker', 'Helm/K8s'],
  },
  {
    title: 'Frontend',
    titleClass: 'techstack__tier-title--frontend' as const,
    tier: 'frontend' as const,
    techs: ['React 19', 'TypeScript', 'Vite', 'CSS Custom Properties'],
  },
]

export default function TechStack() {
  return (
    <SectionWrapper
      id="tech-stack"
      title="Tech Stack"
      subtitle="From microcontroller to cloud to browser"
      alt
    >
      <div className="techstack__tiers">
        {tiers.map((t) => (
          <div key={t.title} className="techstack__tier">
            <div className={`techstack__tier-title ${t.titleClass}`}>{t.title}</div>
            <div className="techstack__badges">
              {t.techs.map((tech) => (
                <TechBadge key={tech} label={tech} tier={t.tier} />
              ))}
            </div>
          </div>
        ))}
      </div>
    </SectionWrapper>
  )
}
