import pandas as pd

from loan_analytics.metrics import calculate_metrics
from loan_analytics.quality import normalize_and_validate


def sample_frame() -> pd.DataFrame:
    return pd.DataFrame([
        {"loanId": "1", "applicantId": "a", "amount": 1000, "termMonths": 12, "status": "Approved", "createdAt": "2026-01-01T00:00:00Z", "decidedAt": "2026-01-01T06:00:00Z"},
        {"loanId": "2", "applicantId": "b", "amount": 3000, "termMonths": 24, "status": "Rejected", "createdAt": "2026-01-02T00:00:00Z", "decidedAt": "2026-01-02T18:00:00Z"},
        {"loanId": "3", "applicantId": "a", "amount": 2000, "termMonths": 6, "status": "Pending", "createdAt": "2026-01-03T00:00:00Z", "decidedAt": None},
    ])


def test_metrics_use_decided_loans_as_approval_denominator():
    frame, quality = normalize_and_validate(sample_frame())
    metrics = calculate_metrics(frame)
    assert quality.valid
    assert metrics["total_applications"] == 3
    assert metrics["decision_rate"] == 2 / 3
    assert metrics["approval_rate_decided"] == 0.5
    assert metrics["median_decision_hours"] == 12


def test_quality_rejects_inconsistent_pending_decision():
    raw = sample_frame()
    raw.loc[2, "decidedAt"] = "2026-01-03T04:00:00Z"
    _, quality = normalize_and_validate(raw)
    assert not quality.valid
    assert "Hay solicitudes pendientes con fecha de decisión." in quality.issues
