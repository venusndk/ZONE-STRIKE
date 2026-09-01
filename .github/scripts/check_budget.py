#!/usr/bin/env python3
"""Enforces the performance budgets from docs/TECHNICAL_ARCHITECTURE.md §2/§9/§10 against a
budget-report.json produced by the Unity-side BudgetGate.RunAndExport editor method
(see .github/workflows/build.yml's budget-gate job comment — that Editor script is a tracked
follow-up implementation task, not yet written).

Expected report shape:
{
  "drawCallsLowTier": <int>,
  "drawCallsMidHighTier": <int>,
  "coreLocalAddressablesBytes": <int>,
  "gcAllocBytesPerFrameSteadyState": <int>
}

Exits non-zero (failing the CI job) if any budget is exceeded, per Technical Architecture §9's
explicit "fail the build, not just warn" requirement.
"""
import json
import sys

BUDGETS = {
    "drawCallsLowTier": 120,
    "drawCallsMidHighTier": 180,
    "coreLocalAddressablesBytes": 800 * 1024 * 1024,  # 800MB, Technical Architecture §6
    "gcAllocBytesPerFrameSteadyState": 0,              # Technical Architecture §10.3: zero steady-state allocation target
}


def main(report_path: str) -> int:
    with open(report_path, "r", encoding="utf-8") as f:
        report = json.load(f)

    failures = []
    for key, limit in BUDGETS.items():
        value = report.get(key)
        if value is None:
            failures.append(f"  MISSING metric '{key}' in report — BudgetGate export is incomplete.")
            continue
        if value > limit:
            failures.append(f"  FAIL {key}: {value} exceeds budget of {limit}")

    if failures:
        print("Performance budget gate FAILED:")
        for line in failures:
            print(line)
        return 1

    print("Performance budget gate PASSED — all metrics within Technical Architecture §2/§9/§10 budgets.")
    return 0


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: check_budget.py <path-to-budget-report.json>")
        sys.exit(2)
    sys.exit(main(sys.argv[1]))
