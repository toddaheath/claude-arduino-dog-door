const REPO_URL = 'https://github.com/toddlheath/claude-arduino-dog-door'

export default function OpenSource() {
  return (
    <section id="open-source" className="section opensource">
      <h2 className="opensource__title">Built in the Open</h2>
      <p className="opensource__desc">
        Firmware, API, admin dashboard, Helm charts, and documentation &mdash; everything is open source
        and ready to clone.
      </p>
      <div className="opensource__clone">
        git clone {REPO_URL}.git
      </div>
      <div className="opensource__links">
        <a className="opensource__link" href={REPO_URL} target="_blank" rel="noopener noreferrer">
          GitHub Repository
        </a>
        <a className="opensource__link" href={`${REPO_URL}/blob/main/docs/architecture.md`} target="_blank" rel="noopener noreferrer">
          Architecture Docs
        </a>
        <a className="opensource__link" href={`${REPO_URL}/blob/main/docs/hardware-selection.md`} target="_blank" rel="noopener noreferrer">
          Hardware Guide
        </a>
      </div>
    </section>
  )
}
