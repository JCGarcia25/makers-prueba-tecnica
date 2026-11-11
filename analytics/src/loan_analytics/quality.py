from __future__ import annotations

from dataclasses import dataclass

import pandas as pd

REQUIRED_COLUMNS = {
    "loanId", "applicantId", "amount", "termMonths", "status", "createdAt", "decidedAt"
}
VALID_STATUSES = {"Pending", "Approved", "Rejected"}


@dataclass(frozen=True)
class QualityResult:
    valid: bool
    row_count: int
    issues: tuple[str, ...]


def normalize_and_validate(raw: pd.DataFrame) -> tuple[pd.DataFrame, QualityResult]:
    missing = sorted(REQUIRED_COLUMNS - set(raw.columns))
    if missing:
        return raw.copy(), QualityResult(False, len(raw), (f"Columnas faltantes: {', '.join(missing)}",))

    frame = raw.copy()
    frame["createdAt"] = pd.to_datetime(frame["createdAt"], utc=True, errors="coerce")
    frame["decidedAt"] = pd.to_datetime(frame["decidedAt"], utc=True, errors="coerce")
    frame["amount"] = pd.to_numeric(frame["amount"], errors="coerce")
    frame["termMonths"] = pd.to_numeric(frame["termMonths"], errors="coerce")

    issues: list[str] = []
    if frame["loanId"].duplicated().any():
        issues.append(f"Identificadores duplicados: {int(frame['loanId'].duplicated().sum())}")
    if frame["loanId"].isna().any() or frame["applicantId"].isna().any():
        issues.append("Hay identificadores vacíos.")
    if frame["amount"].isna().any() or (frame["amount"] <= 0).any():
        issues.append("Hay montos vacíos, no numéricos o no positivos.")
    if frame["termMonths"].isna().any() or (~frame["termMonths"].between(1, 120)).any():
        issues.append("Hay plazos vacíos o fuera del rango de 1 a 120 meses.")
    invalid_statuses = sorted(set(frame["status"].dropna()) - VALID_STATUSES)
    if invalid_statuses:
        issues.append(f"Estados desconocidos: {', '.join(map(str, invalid_statuses))}")
    if frame["createdAt"].isna().any():
        issues.append("Hay fechas de creación inválidas.")

    decided = frame["status"].isin(["Approved", "Rejected"])
    if frame.loc[decided, "decidedAt"].isna().any():
        issues.append("Hay solicitudes decididas sin fecha de decisión.")
    if frame.loc[~decided, "decidedAt"].notna().any():
        issues.append("Hay solicitudes pendientes con fecha de decisión.")
    chronology_error = frame["decidedAt"].notna() & (frame["decidedAt"] < frame["createdAt"])
    if chronology_error.any():
        issues.append("Hay decisiones anteriores a la creación de la solicitud.")

    frame["decisionHours"] = (frame["decidedAt"] - frame["createdAt"]).dt.total_seconds() / 3600
    return frame, QualityResult(not issues, len(frame), tuple(issues))
