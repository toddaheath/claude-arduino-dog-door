const REPO_URL = 'https://github.com/toddlheath/claude-arduino-dog-door'

export default function Hero() {
  return (
    <section className="hero" id="hero">
      <h1 className="hero__title">Your Dog Door, Now Intelligent</h1>
      <p className="hero__subtitle">
        Edge AI on a $10 ESP32-CAM detects and identifies your dog in milliseconds.
        Cloud dashboard. Open source. ~$84 total build cost.
      </p>
      <div className="hero__ctas">
        <a className="hero__cta hero__cta--primary" href={REPO_URL} target="_blank" rel="noopener noreferrer">
          View on GitHub
        </a>
        <a className="hero__cta hero__cta--secondary" href="#how-it-works">
          See How It Works
        </a>
      </div>
    </section>
  )
}
