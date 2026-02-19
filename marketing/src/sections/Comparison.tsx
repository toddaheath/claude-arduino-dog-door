import SectionWrapper from '../components/SectionWrapper'

interface Product {
  name: string
  price: string
  subscription: string
  selfHosted: string
  ai: string
  openSource: string
  multiPet: string
  highlight?: boolean
}

const products: Product[] = [
  { name: 'Smart Dog Door', price: '~$84 parts', subscription: 'None', selfHosted: '✓', ai: 'On-device + cloud', openSource: '✓', multiPet: '✓', highlight: true },
  { name: 'Wayzn', price: '$299', subscription: '$9/mo', selfHosted: '✗', ai: 'App-only', openSource: '✗', multiPet: '✓' },
  { name: 'PetSafe SmartDoor', price: '$149', subscription: '$4/mo', selfHosted: '✗', ai: '✗', openSource: '✗', multiPet: 'Limited' },
  { name: 'SureFlap', price: '$199', subscription: 'None', selfHosted: '✗', ai: 'RFID only', openSource: '✗', multiPet: '✓' },
]

const columns: { key: keyof Product; label: string }[] = [
  { key: 'price', label: 'Price' },
  { key: 'subscription', label: 'Subscription' },
  { key: 'selfHosted', label: 'Self-Hosted' },
  { key: 'ai', label: 'AI' },
  { key: 'openSource', label: 'Open Source' },
  { key: 'multiPet', label: 'Multi-Pet' },
]

export default function Comparison() {
  return (
    <SectionWrapper
      id="comparison"
      title="How We Compare"
      subtitle="No subscriptions, no lock-in, no cloud dependency — just hardware you own."
    >
      <div style={{ overflowX: 'auto' }}>
        <table className="comparison-table">
          <thead>
            <tr>
              <th>Product</th>
              {columns.map(c => <th key={c.key}>{c.label}</th>)}
            </tr>
          </thead>
          <tbody>
            {products.map(p => (
              <tr key={p.name} className={p.highlight ? 'comparison-table__row--highlight' : ''}>
                <td className="comparison-table__name">
                  {p.highlight && <span className="comparison-table__badge">This Project</span>}
                  {p.name}
                </td>
                {columns.map(c => (
                  <td
                    key={c.key}
                    className={
                      p.highlight
                        ? 'comparison-table__cell--highlight'
                        : (c.key === 'openSource' || c.key === 'selfHosted') && p[c.key] === '✗'
                          ? 'comparison-table__cell--bad'
                          : ''
                    }
                  >
                    {p[c.key] as string}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </SectionWrapper>
  )
}
