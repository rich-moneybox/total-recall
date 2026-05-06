const { useState, useCallback } = React;

// --- API Client ---
const API_BASE = "/api/payments/family";

async function initiatePayment(request) {
  const response = await fetch(API_BASE, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      isaAccountId: request.isaAccountId,
      familyMemberName: request.familyMemberName,
      familyMemberEmail: request.familyMemberEmail,
      familyMemberSortCode: request.familyMemberSortCode,
      familyMemberAccountNumber: request.familyMemberAccountNumber,
      amount: request.amount,
      reference: request.reference,
    }),
  });

  const data = await response.json().catch(() => null);

  if (data) return data;

  // If response has no JSON body, build a result from the status
  return {
    paymentId: "00000000-0000-0000-0000-000000000000",
    isaAccountId: request.isaAccountId,
    amount: request.amount,
    status: response.ok ? "Accepted" : "Failed",
    createdAt: new Date().toISOString(),
  };
}

function delay(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

// --- Components ---

function Header({ view, onNavigate }) {
  return (
    <header>
      <div className="flag-banner">
        LaunchDarkly: <span>FamilyPayments</span> — Rollout at 5% ·
        Observation window active
      </div>
      <div className="header">
        <div className="header-logo">
          <svg viewBox="0 0 32 32" fill="none">
            <rect width="32" height="32" rx="8" fill="#00b4aa" />
            <text
              x="16"
              y="22"
              textAnchor="middle"
              fill="white"
              fontSize="18"
              fontWeight="bold"
              fontFamily="sans-serif"
            >
              M
            </text>
          </svg>
          <div>
            <div className="header-title">Moneybox</div>
            <div className="header-subtitle">Family Payments ISA</div>
          </div>
        </div>
        <nav className="header-nav">
          <button
            className={`nav-btn ${view === "isa-holder" ? "active" : ""}`}
            onClick={() => onNavigate("isa-holder")}
          >
            ISA Holder
          </button>
          <button
            className={`nav-btn ${view === "family-member" ? "active" : ""}`}
            onClick={() => onNavigate("family-member")}
          >
            Family Member
          </button>
        </nav>
      </div>
    </header>
  );
}

function AllowanceBar({ used, limit, pendingAmount }) {
  const remaining = limit - used;
  const pct = Math.min(100, ((used + (pendingAmount || 0)) / limit) * 100);
  const level = pct >= 95 ? "full" : pct >= 75 ? "high" : "";

  return (
    <div className="allowance-bar">
      <div className="allowance-label">
        <span className="allowance-label-text">
          ISA Allowance {new Date().getFullYear()}/{(new Date().getFullYear() + 1).toString().slice(2)}
        </span>
        <span className="allowance-label-amount">
          £{remaining.toLocaleString()} remaining
        </span>
      </div>
      <div className="allowance-track">
        <div
          className={`allowance-fill ${level}`}
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  );
}

function StepIndicator({ current, total }) {
  return (
    <div className="steps">
      {Array.from({ length: total }, (_, i) => (
        <div
          key={i}
          className={`step ${i === current ? "active" : i < current ? "done" : ""}`}
        />
      ))}
    </div>
  );
}

// --- ISA Holder Journey ---

function IsaHolderView() {
  const [step, setStep] = useState(0);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState(null);
  const [form, setForm] = useState({
    isaAccountId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    familyMemberName: "",
    familyMemberEmail: "",
    familyMemberSortCode: "",
    familyMemberAccountNumber: "",
    amount: "",
    reference: "",
  });

  const update = (field) => (e) =>
    setForm((f) => ({ ...f, [field]: e.target.value }));

  const handleSubmit = useCallback(
    async (e) => {
      e.preventDefault();
      setLoading(true);
      try {
        const res = await initiatePayment({
          isaAccountId: form.isaAccountId,
          familyMemberName: form.familyMemberName,
          familyMemberEmail: form.familyMemberEmail,
          familyMemberSortCode: form.familyMemberSortCode,
          familyMemberAccountNumber: form.familyMemberAccountNumber,
          amount: parseFloat(form.amount),
          reference: form.reference,
        });
        setResult(res);
        setStep(2);
      } catch (err) {
        setResult({
          paymentId: "00000000-0000-0000-0000-000000000000",
          isaAccountId: form.isaAccountId,
          amount: parseFloat(form.amount),
          status: "Failed",
          createdAt: new Date().toISOString(),
          error: err.message,
        });
        setStep(2);
      } finally {
        setLoading(false);
      }
    },
    [form]
  );

  const reset = () => {
    setStep(0);
    setResult(null);
    setForm((f) => ({ ...f, amount: "", reference: "" }));
  };

  return (
    <div className="container fade-in">
      <StepIndicator current={step} total={3} />

      {step === 0 && (
        <div className="card">
          <h2 className="card-title">Send money to a family member's ISA</h2>
          <p className="card-desc">
            Gift money directly into a family member's ISA. The payment will be
            validated against their remaining ISA allowance.
          </p>
          <AllowanceBar used={12400} limit={20000} pendingAmount={0} />
          <form
            onSubmit={(e) => {
              e.preventDefault();
              setStep(1);
            }}
          >
            <div className="form-group">
              <label className="form-label">Family member's full name</label>
              <input
                className="form-input"
                value={form.familyMemberName}
                onChange={update("familyMemberName")}
                placeholder="Jane Doe"
                required
              />
            </div>
            <div className="form-group">
              <label className="form-label">Email address</label>
              <input
                className="form-input"
                type="email"
                value={form.familyMemberEmail}
                onChange={update("familyMemberEmail")}
                placeholder="jane.doe@example.com"
                required
              />
            </div>
            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Sort code</label>
                <input
                  className="form-input"
                  value={form.familyMemberSortCode}
                  onChange={update("familyMemberSortCode")}
                  placeholder="12-34-56"
                  required
                />
              </div>
              <div className="form-group">
                <label className="form-label">Account number</label>
                <input
                  className="form-input"
                  value={form.familyMemberAccountNumber}
                  onChange={update("familyMemberAccountNumber")}
                  placeholder="12345678"
                  required
                />
              </div>
            </div>
            <button type="submit" className="btn btn-primary">
              Continue
            </button>
          </form>
        </div>
      )}

      {step === 1 && (
        <div className="card fade-in">
          <h2 className="card-title">Payment details</h2>
          <p className="card-desc">
            How much would you like to send to {form.familyMemberName}?
          </p>
          <AllowanceBar
            used={12400}
            limit={20000}
            pendingAmount={parseFloat(form.amount) || 0}
          />
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label className="form-label">Amount (£)</label>
              <input
                className="form-input"
                type="number"
                min="1"
                max="20000"
                step="0.01"
                value={form.amount}
                onChange={update("amount")}
                placeholder="500.00"
                required
              />
            </div>
            <div className="form-group">
              <label className="form-label">Reference (optional)</label>
              <input
                className="form-input"
                value={form.reference}
                onChange={update("reference")}
                placeholder="Birthday gift"
              />
            </div>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading && <span className="spinner" />}
              {loading ? "Processing..." : "Send payment"}
            </button>
          </form>
          <button
            className="btn btn-secondary"
            style={{ marginTop: 8 }}
            onClick={() => setStep(0)}
          >
            Back
          </button>
        </div>
      )}

      {step === 2 && result && <ResultCard result={result} onReset={reset} />}
    </div>
  );
}

function ResultCard({ result, onReset }) {
  const config = {
    Accepted: {
      icon: "✅",
      title: "Payment accepted",
      desc: "A secure payment link has been sent to the family member.",
      badge: "status-accepted",
    },
    Rejected: {
      icon: "❌",
      title: "Payment rejected",
      desc: "The payment exceeds the remaining ISA allowance for this tax year.",
      badge: "status-rejected",
    },
    AllowanceConflict: {
      icon: "⚠️",
      title: "Concurrent payment detected",
      desc: "Another payment was processed at the same time. The ISA allowance balance has changed. Please re-initiate the payment.",
      badge: "status-conflict",
    },
    Failed: {
      icon: "💥",
      title: "Something went wrong",
      desc: "The payment could not be processed. This may be because the ISA service is unavailable. Please try again later.",
      badge: "status-rejected",
    },
  };

  const c = config[result.status] || config.Rejected;

  return (
    <div className="card result-card fade-in">
      <div className="result-icon">{c.icon}</div>
      <div className="result-title">{c.title}</div>
      <span className={`status-badge ${c.badge}`}>{result.status}</span>
      <p className="result-desc" style={{ marginTop: 12 }}>
        {c.desc}
      </p>
      <div className="result-details">
        <div className="result-row">
          <span className="result-row-label">Payment ID</span>
          <span className="result-row-value" style={{ fontSize: 11 }}>
            {result.paymentId}
          </span>
        </div>
        <div className="result-row">
          <span className="result-row-label">Amount</span>
          <span className="result-row-value">
            £{result.amount.toLocaleString()}
          </span>
        </div>
        <div className="result-row">
          <span className="result-row-label">Created</span>
          <span className="result-row-value">
            {new Date(result.createdAt).toLocaleString()}
          </span>
        </div>
      </div>
      {result.paymentLinkUrl && (
        <div className="payment-link-box">🔗 {result.paymentLinkUrl}</div>
      )}
      <button
        className="btn btn-secondary"
        style={{ marginTop: 16 }}
        onClick={onReset}
      >
        {result.status === "AllowanceConflict"
          ? "Re-initiate payment"
          : "Make another payment"}
      </button>
    </div>
  );
}

// --- Family Member Journey ---

function FamilyMemberView() {
  const [step, setStep] = useState(0);
  const [loading, setLoading] = useState(false);

  const mockPayment = {
    paymentId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    senderName: "John Doe",
    amount: 500,
    reference: "Birthday gift",
    isaAccountId: "xxx-xxx-7890",
    expiresAt: new Date(Date.now() + 72 * 60 * 60 * 1000).toISOString(),
  };

  const handleConfirm = async () => {
    setLoading(true);
    await delay(2000);
    setLoading(false);
    setStep(1);
  };

  return (
    <div className="container fade-in">
      {step === 0 && (
        <div className="card">
          <div className="family-hero">
            <h2>You've received a gift! 🎁</h2>
            <p>
              {mockPayment.senderName} wants to contribute to your ISA
            </p>
          </div>
          <div className="payment-summary">
            <div className="payment-amount">
              £{mockPayment.amount.toLocaleString()}
            </div>
            <div className="payment-ref">"{mockPayment.reference}"</div>
          </div>
          <div className="result-details">
            <div className="result-row">
              <span className="result-row-label">From</span>
              <span className="result-row-value">{mockPayment.senderName}</span>
            </div>
            <div className="result-row">
              <span className="result-row-label">To ISA account</span>
              <span className="result-row-value">{mockPayment.isaAccountId}</span>
            </div>
            <div className="result-row">
              <span className="result-row-label">Expires</span>
              <span className="result-row-value">
                {new Date(mockPayment.expiresAt).toLocaleDateString()}
              </span>
            </div>
          </div>
          <p
            className="card-desc"
            style={{ marginTop: 16, fontSize: 12, textAlign: "center" }}
          >
            By accepting, you confirm you are the named recipient and the
            payment will count towards your ISA allowance for the current tax
            year.
          </p>
          <button
            className="btn btn-primary"
            onClick={handleConfirm}
            disabled={loading}
          >
            {loading && <span className="spinner" />}
            {loading ? "Processing..." : "Accept payment"}
          </button>
        </div>
      )}

      {step === 1 && (
        <div className="card result-card fade-in">
          <div className="result-icon">🎉</div>
          <div className="result-title">Payment received!</div>
          <span className="status-badge status-accepted">Completed</span>
          <p className="result-desc" style={{ marginTop: 12 }}>
            £{mockPayment.amount.toLocaleString()} has been added to your ISA.
            You'll see it in your Moneybox app shortly.
          </p>
          <div className="result-details">
            <div className="result-row">
              <span className="result-row-label">Amount</span>
              <span className="result-row-value">
                £{mockPayment.amount.toLocaleString()}
              </span>
            </div>
            <div className="result-row">
              <span className="result-row-label">From</span>
              <span className="result-row-value">{mockPayment.senderName}</span>
            </div>
            <div className="result-row">
              <span className="result-row-label">Reference</span>
              <span className="result-row-value">{mockPayment.reference}</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// --- App ---

function App() {
  const [view, setView] = useState("isa-holder");

  return (
    <div>
      <Header view={view} onNavigate={setView} />
      {view === "isa-holder" ? <IsaHolderView /> : <FamilyMemberView />}
    </div>
  );
}

const root = ReactDOM.createRoot(document.getElementById("root"));
root.render(<App />);
