import SectionWrapper from '../components/SectionWrapper'

function BrowserFrame({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="screenshot-frame">
      <div className="screenshot-frame__chrome">
        <div className="screenshot-frame__dots">
          <span className="screenshot-frame__dot screenshot-frame__dot--red" />
          <span className="screenshot-frame__dot screenshot-frame__dot--yellow" />
          <span className="screenshot-frame__dot screenshot-frame__dot--green" />
        </div>
        <div className="screenshot-frame__url">{title}</div>
      </div>
      <div className="screenshot-frame__content">
        {children}
      </div>
    </div>
  )
}

function MockAnimalCard({ name, breed, allowed }: { name: string; breed: string; allowed: boolean }) {
  return (
    <div style={{ border: '1px solid #334155', borderRadius: 8, overflow: 'hidden', background: '#1e293b' }}>
      <div style={{ height: 80, background: 'linear-gradient(135deg, #1e3a5f, #2d1b69)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 28 }}>
        🐕
      </div>
      <div style={{ padding: '10px 12px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div style={{ fontWeight: 600, fontSize: 14 }}>{name}</div>
          <div style={{ fontSize: 12, color: '#64748b' }}>{breed}</div>
        </div>
        <span style={{ padding: '2px 8px', borderRadius: 12, fontSize: 11, background: allowed ? '#2e7d32' : '#c62828', color: '#fff', fontWeight: 600 }}>
          {allowed ? 'Allowed' : 'Denied'}
        </span>
      </div>
    </div>
  )
}

function MockLogRow({ time, event, animal, color }: { time: string; event: string; animal: string; color: string }) {
  return (
    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', padding: '8px 12px', borderBottom: '1px solid #1e293b', fontSize: 12 }}>
      <span style={{ color: '#64748b' }}>{time}</span>
      <span style={{ padding: '1px 6px', borderRadius: 8, background: color, color: '#fff', fontSize: 11, display: 'inline-block', width: 'fit-content' }}>{event}</span>
      <span style={{ color: '#94a3b8' }}>{animal}</span>
    </div>
  )
}

export default function Screenshots() {
  return (
    <SectionWrapper id="screenshots" title="See It In Action" subtitle="A clean, responsive dashboard built for quick daily check-ins." alt>
      <div className="screenshots-grid">
        <BrowserFrame title="Animals">
          <div style={{ padding: 16 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
              <span style={{ fontWeight: 700, fontSize: 16 }}>Animals</span>
              <span style={{ padding: '4px 12px', borderRadius: 6, background: '#38bdf8', color: '#0f172a', fontSize: 12, fontWeight: 600 }}>+ Add Animal</span>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
              <MockAnimalCard name="Buddy" breed="Golden Retriever" allowed={true} />
              <MockAnimalCard name="Luna" breed="Border Collie" allowed={true} />
              <MockAnimalCard name="Max" breed="German Shepherd" allowed={false} />
            </div>
          </div>
        </BrowserFrame>

        <BrowserFrame title="Access Log">
          <div style={{ padding: 16 }}>
            <div style={{ fontWeight: 700, fontSize: 16, marginBottom: 12 }}>Access Log</div>
            <div style={{ display: 'flex', gap: 8, marginBottom: 10 }}>
              <span style={{ padding: '4px 10px', borderRadius: 6, background: '#1a237e', color: '#90caf9', fontSize: 11, fontWeight: 600 }}>Entries: 3</span>
              <span style={{ padding: '4px 10px', borderRadius: 6, background: '#bf360c', color: '#ffcc80', fontSize: 11, fontWeight: 600 }}>Exits: 2</span>
            </div>
            <div style={{ border: '1px solid #334155', borderRadius: 6, overflow: 'hidden' }}>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', padding: '6px 12px', background: '#0f172a', fontSize: 11, color: '#64748b', fontWeight: 700 }}>
                <span>TIME</span><span>EVENT</span><span>ANIMAL</span>
              </div>
              <MockLogRow time="14:32" event="AccessGranted" animal="Buddy" color="#2e7d32" />
              <MockLogRow time="14:15" event="ExitGranted" animal="Luna" color="#1565c0" />
              <MockLogRow time="13:58" event="AccessDenied" animal="Unknown" color="#c62828" />
              <MockLogRow time="13:40" event="AccessGranted" animal="Buddy" color="#2e7d32" />
            </div>
          </div>
        </BrowserFrame>

        <BrowserFrame title="Settings">
          <div style={{ padding: 16 }}>
            <div style={{ fontWeight: 700, fontSize: 16, marginBottom: 12 }}>Door Settings</div>
            {[
              { label: 'Door Enabled', checked: true },
              { label: 'Auto Close', checked: true },
              { label: 'Night Mode', checked: false },
            ].map(s => (
              <div key={s.label} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 0', borderBottom: '1px solid #1e293b', fontSize: 13 }}>
                <span style={{ color: '#e2e8f0' }}>{s.label}</span>
                <div style={{ width: 36, height: 20, borderRadius: 10, background: s.checked ? '#38bdf8' : '#334155', position: 'relative' }}>
                  <div style={{ width: 16, height: 16, borderRadius: 8, background: '#fff', position: 'absolute', top: 2, left: s.checked ? 18 : 2 }} />
                </div>
              </div>
            ))}
            <div style={{ marginTop: 12, fontSize: 13 }}>
              <div style={{ color: '#94a3b8', marginBottom: 4 }}>Min Confidence</div>
              <div style={{ height: 6, background: '#1e293b', borderRadius: 3, overflow: 'hidden' }}>
                <div style={{ height: '100%', width: '70%', background: 'linear-gradient(90deg, #38bdf8, #818cf8)', borderRadius: 3 }} />
              </div>
              <div style={{ textAlign: 'right', fontSize: 11, color: '#64748b', marginTop: 2 }}>70%</div>
            </div>
          </div>
        </BrowserFrame>
      </div>
    </SectionWrapper>
  )
}
