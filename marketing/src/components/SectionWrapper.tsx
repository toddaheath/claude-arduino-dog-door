import type { ReactNode } from 'react'

interface SectionWrapperProps {
  id: string
  title: string
  subtitle?: string
  alt?: boolean
  children: ReactNode
}

export default function SectionWrapper({ id, title, subtitle, alt, children }: SectionWrapperProps) {
  return (
    <section id={id} className={`section${alt ? ' section--alt' : ''}`}>
      <div className="section__header">
        <h2 className="section__title">{title}</h2>
        {subtitle && <p className="section__subtitle">{subtitle}</p>}
      </div>
      {children}
    </section>
  )
}
