from __future__ import annotations

import json
from pathlib import Path

import matplotlib.pyplot as plt
import pandas as pd

from .metrics import calculate_metrics, monthly_summary
from .quality import normalize_and_validate


def load_snapshot(path: Path) -> tuple[dict, pd.DataFrame]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if payload.get("schemaVersion") != "1.0":
        raise ValueError("Versión de esquema no soportada; se esperaba 1.0.")
    if payload.get("currency") != "COP":
        raise ValueError("La moneda del conjunto debe ser COP.")
    return payload, pd.DataFrame(payload.get("loans", []))


def create_report(input_path: Path, output_dir: Path) -> None:
    metadata, raw = load_snapshot(input_path)
    frame, quality = normalize_and_validate(raw)
    if not quality.valid:
        raise ValueError("Falló la calidad de datos:\n- " + "\n- ".join(quality.issues))
    output_dir.mkdir(parents=True, exist_ok=True)
    metrics = calculate_metrics(frame)
    monthly = monthly_summary(frame)
    pd.DataFrame([metrics]).to_csv(output_dir / "metrics.csv", index=False)
    monthly.to_csv(output_dir / "monthly_summary.csv", index=False)

    status_counts = frame["status"].value_counts().reindex(["Pending", "Approved", "Rejected"], fill_value=0)
    fig, axes = plt.subplots(1, 2, figsize=(11, 4.5))
    status_counts.plot.bar(ax=axes[0], color=["#f2c94c", "#27ae60", "#c0392b"], title="Solicitudes por estado")
    axes[0].set_xlabel(""); axes[0].set_ylabel("Solicitudes"); axes[0].tick_params(axis="x", rotation=0)
    frame["amount"].plot.hist(ax=axes[1], bins=min(12, max(3, len(frame) // 3)), color="#1473e6", title="Distribución de montos")
    axes[1].set_xlabel("Monto solicitado (COP)"); axes[1].set_ylabel("Frecuencia")
    fig.tight_layout(); fig.savefig(output_dir / "portfolio_overview.png", dpi=160); plt.close(fig)

    report = f"""# Informe de solicitudes de préstamo

Datos extraídos en `{metadata['extractedAt']}` desde `{metadata['source']}`. Moneda: COP.

## Calidad

Se analizaron {quality.row_count} registros. Las reglas de completitud, unicidad, dominio y coherencia temporal se cumplieron.

## Indicadores

- Solicitudes: {metrics['total_applications']:,}
- Monto solicitado: COP {metrics['total_requested_cop']:,.0f}
- Mediana del monto: COP {metrics['median_amount_cop']:,.0f}
- Solicitudes pendientes: {metrics['pending_applications']:,}
- Solicitudes con decisión: {metrics['decision_rate']:.1%} del total
- Tasa de aprobación: {metrics['approval_rate_decided']:.1%} de las solicitudes decididas
- Mediana del tiempo de decisión: {metrics['median_decision_hours']:.1f} horas
- Solicitantes únicos: {metrics['unique_applicants']:,}

## Lectura de negocio

La tasa de aprobación usa únicamente solicitudes decididas como denominador; las pendientes se informan por separado para evitar sesgo. Estos datos son sintéticos y sirven para demostrar el flujo analítico. No permiten medir riesgo, mora ni rentabilidad porque no incluyen ingresos, pagos, tasas de interés o incumplimientos.

![Resumen del portafolio](portfolio_overview.png)
"""
    (output_dir / "report.md").write_text(report, encoding="utf-8")
