#!/usr/bin/env bash
# Read-only WSL entry point. Only the Windows collector queries Unity.
set -uo pipefail
if (( $# != 0 )); then
    printf 'Usage: cfcheck (no arguments)\n' >&2
    exit 2
fi
if ! command -v powershell.exe >/dev/null || ! command -v python3 >/dev/null; then
    printf 'cfcheck requires Windows powershell.exe and Ubuntu python3.\n' >&2
    exit 2
fi
if summary=$(powershell.exe -NoProfile -NonInteractive -File 'C:\Users\USER\Desktop\Unity\Crazy Frog\Tools\Validation\Invoke-CFCheck.ps1'); then
    collector_exit=0
else
    collector_exit=$?
fi
# Validate the locator and read the report via /mnt/c, never rerun collection.
if ! printf '%s' "$summary" | python3 -c '
import json, pathlib, re, sys
try:
    locator = json.load(sys.stdin)
    run_id = locator["runId"]
    if not re.fullmatch(r"[0-9]{8}T[0-9]{9}Z-[a-f0-9]{32}", run_id):
        raise ValueError("invalid run-id")
    root = pathlib.Path("/mnt/c/Users/USER/Desktop/Unity/Crazy Frog/ValidationResults")
    path = root / run_id / "report.json"
    expected = "C:/Users/USER/Desktop/Unity/Crazy Frog/ValidationResults/" + run_id + "/report.json"
    if locator["reportPath"].replace(chr(92), "/") != expected:
        raise ValueError("unexpected report path")
    report = json.loads(path.read_text(encoding="utf-8-sig"))
    code = int(sys.argv[1])
    mapping = {"PASS": 0, "FAIL": 1, "ERROR": 2, "INCOMPLETE": 3}
    if report["runId"] != run_id or report["exitCode"] != code or locator["exitCode"] != code or mapping[report["overall"]] != code or locator["overall"] != report["overall"]:
        raise ValueError("report/exit-code mismatch")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    print("CFCheck report: " + str(path), file=sys.stderr)
except Exception as error:
    print("CFCheck wrapper ERROR: " + str(error), file=sys.stderr)
    sys.exit(2)
' "$collector_exit"; then
    exit 2
fi
exit "$collector_exit"
