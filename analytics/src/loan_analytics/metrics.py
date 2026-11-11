from __future__ import annotations

import pandas as pd


def calculate_metrics(frame: pd.DataFrame) -> dict[str, float | int]:
    total = len(frame)
    decided = frame[frame["status"].isin(["Approved", "Rejected"])]
    approved = int((frame["status"] == "Approved").sum())
    pending = int((frame["status"] == "Pending").sum())
    return {
        "total_applications": total,
        "total_requested_cop": float(frame["amount"].sum()) if total else 0.0,
        "median_amount_cop": float(frame["amount"].median()) if total else 0.0,
        "pending_applications": pending,
        "decision_rate": len(decided) / total if total else 0.0,
        "approval_rate_decided": approved / len(decided) if len(decided) else 0.0,
        "median_decision_hours": float(decided["decisionHours"].median()) if len(decided) else 0.0,
        "unique_applicants": int(frame["applicantId"].nunique()) if total else 0,
    }


def monthly_summary(frame: pd.DataFrame) -> pd.DataFrame:
    work = frame.assign(month=frame["createdAt"].dt.strftime("%Y-%m"))
    return (work.groupby(["month", "status"], observed=True)
            .agg(applications=("loanId", "count"), requested_cop=("amount", "sum"))
            .reset_index().sort_values(["month", "status"]))
