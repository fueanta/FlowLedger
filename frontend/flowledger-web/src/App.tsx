import './App.css'

function App() {
  return (
    <main className="landing-shell">
      <section className="landing-panel" aria-labelledby="page-title">
        <p className="eyebrow">ERP workflow module</p>
        <h1 id="page-title">FlowLedger</h1>
        <p className="summary">
          Billing request approvals, invoice generation, audit history, and
          dashboard reporting for an internal finance workflow.
        </p>
        <div className="status-grid" aria-label="Phase 1 status">
          <div>
            <span>Backend</span>
            <strong>/health ready</strong>
          </div>
          <div>
            <span>Frontend</span>
            <strong>Vite ready</strong>
          </div>
          <div>
            <span>Next phase</span>
            <strong>Domain model</strong>
          </div>
        </div>
      </section>
    </main>
  )
}

export default App
