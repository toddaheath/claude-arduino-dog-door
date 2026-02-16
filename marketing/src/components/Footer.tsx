const REPO_URL = 'https://github.com/toddlheath/claude-arduino-dog-door'

export default function Footer() {
  return (
    <footer className="footer">
      <div className="footer__inner">
        <span className="footer__copy">Smart Dog Door &mdash; Open Source IoT</span>
        <div className="footer__links">
          <a className="footer__link" href={REPO_URL} target="_blank" rel="noopener noreferrer">
            GitHub
          </a>
          <a className="footer__link" href={`${REPO_URL}/blob/main/docs/architecture.md`} target="_blank" rel="noopener noreferrer">
            Docs
          </a>
          <a className="footer__link" href={`${REPO_URL}/issues`} target="_blank" rel="noopener noreferrer">
            Issues
          </a>
        </div>
      </div>
    </footer>
  )
}
