from __future__ import annotations

import argparse
from pathlib import Path

from .report import create_report


def main() -> None:
    parser = argparse.ArgumentParser(description="Genera indicadores y gráficos de solicitudes de préstamo.")
    parser.add_argument("--input", type=Path, required=True, help="Snapshot JSON exportado por la API")
    parser.add_argument("--output", type=Path, default=Path("artifacts/analytics"))
    args = parser.parse_args()
    create_report(args.input, args.output)
    print(f"Informe generado en {args.output.resolve()}")


if __name__ == "__main__":
    main()
