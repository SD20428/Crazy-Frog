# Crazy Frog P1 collector

Windows PowerShell 5.1+ and the existing Unity CLI are required. No packages are installed.

```powershell
powershell.exe -NoProfile -NonInteractive -File "C:\Users\USER\Desktop\Unity\Crazy Frog\Tools\Validation\Invoke-CFCheck.ps1"
$LASTEXITCODE
```

Only `status`, `editor status`, `console status`, `console get`, `script recompile_status`, and EditMode/PlayMode `test list` are allowed by this implementation. The project and Unity version are fixed. It never requests compilation, enters Play Mode, executes tests, starts an Editor, clears Console, saves scenes, initializes Git, or changes MCP configuration.

Each invocation reserves a UTC timestamp + GUID directory under `ValidationResults/`. It saves raw stdout/stderr per command and atomically publishes `report.json` using a move that cannot replace an existing file. Stdout contains one JSON locator with `reportPath`, `runId`, `overall`, and `exitCode`. Paths in that locator are Windows paths. Reports use UTF-8 without BOM and UTC ISO timestamps.

| Exit | Verdict | Meaning |
|---|---|---|
| 0 | PASS | Reserved for a future scope that actually executes all required validation; P1 never emits it |
| 1 | FAIL | Current Console errors or compile failure observed, with required queries completed |
| 2 | ERROR | Lock unavailable, CLI/connection/version failure, malformed JSON, command exit/success failure, invalid required evidence, or report write failure |
| 3 | INCOMPLETE | Queries completed but fresh compilation and tests were intentionally not executed; includes zero discovered tests |

ERROR takes precedence over FAIL; observed failure evidence remains in `reasons` and command responses. Warnings alone do not cause FAIL. Counts describe the current Console, not just errors caused by this collector. Compile status can describe a previous request; `freshCompilation=false` and `execution=not_run` always apply to this invocation. Test counts are discoveries, not passed test counts. A Console ground-truth sample older than two seconds is rejected rather than accepted as proof. Console capture can be truncated; `dropped`/`reset` and the 200-entry limit are preserved.

The process keeps `ValidationResults/.collector.lock` open with `FileShare.None` throughout collection and report publication. A competing invocation writes its own ERROR report, without querying Unity. The empty lock file persists; the OS releases the handle on exit/crash. Never delete the file to break an active lock. All Windows/WSL automation must use this collector. This is a cooperative collector lock, not a lock on the Unity Editor or arbitrary manual editing; stop manual editing during collection if a consistent snapshot is required. The collector itself never changes game files.

The optional `-UnityCliPath` parameter is for an alternative installed CLI or isolated collector failure tests. It does not change the project or permitted query arguments. `-CommandTimeoutSeconds` defaults to 30. A timeout terminates only the child CLI query process, never Unity. Discovery requests may still finish in the Editor; no test execution was requested.

## WSL

The repository copy `Tools/Validation/cfcheck.sh` is the replacement for `/home/nyod09/cfcheck.sh`. Back up the existing home script under a unique name before applying it. The wrapper calls Windows PowerShell without an execution-policy bypass, reads the report via `/mnt/c`, prints the full report JSON to stdout, and preserves exit 1/2/3. Its report-location message goes to stderr. It requires Ubuntu `python3` and Windows interop.

```bash
bash ~/cfcheck.sh
code=$?
printf 'collector exit=%s\n' "$code"
# Existing alias cfcheck="bash ~/cfcheck.sh" continues to work.
```

Do not pipe through a command that hides the exit code. For a pipeline use `set -o pipefail`. No Linux Unity process or second Editor is needed. The wrapper does not call the old script. If execution policy or sandbox approval blocks a call, report that error and obtain the required permission; do not change policy, configure MCP, or use an alternate execution path to bypass it.

## Verification

```powershell
# Parse without running the collector.
$tokens = $null; $parseErrors = $null
[System.Management.Automation.Language.Parser]::ParseFile("C:\Users\USER\Desktop\Unity\Crazy Frog\Tools\Validation\Invoke-CFCheck.ps1", [ref]$tokens, [ref]$parseErrors) | Out-Null
$parseErrors

# JSON Schema validation requires PowerShell 7 (collector itself supports 5.1).
Test-Json -LiteralPath '<absolute report.json path>' -SchemaFile 'C:\Users\USER\Desktop\Unity\Crazy Frog\Tools\Validation\report.schema.json'
```

Run twice and confirm different run IDs, unchanged hash of the first report, all required command exit/success fields, and INCOMPLETE with `freshCompilation=false` / both test executions `not_run`. Invoke with a nonexistent `-UnityCliPath` to verify ERROR/2 and skipped dependent queries. Hold the lock from a separate process while invoking to verify ERROR/2 without CLI queries. Reports from these collector checks are infrastructure evidence, not game tests.

The included infrastructure check script automates these checks (PowerShell 7):

```powershell
pwsh -NoProfile -NonInteractive -File "C:\Users\USER\Desktop\Unity\Crazy Frog\Tools\Validation\Test-CFCheck.ps1" -IncludeLive
```

Without `-IncludeLive` it checks syntax, missing CLI, a harmless failing native process, lock contention, exit-code propagation, and report schemas without querying Unity. With the switch it also collects twice against the existing Editor. It does not induce errors in the Editor. Timeout handling and a live Editor with real compile/Console errors have not been fault-injected.

## Applied and verified: 2026-10-29 KST

- `/home/nyod09/cfcheck.sh` was replaced after a byte-verified backup at `/home/nyod09/cfcheck.sh.backup-20261028T153249Z-ac89d6ff01b54d21a03d9090052e1b38`.
- Infrastructure checks: 29 assertions passed, including two live collections, distinct result directories, first-report hash preservation, missing CLI, failed native query, and lock contention.
- Installed WSL wrapper ran end to end: all seven queries succeeded and process/report exit codes were both 3 (INCOMPLETE).
- Latest WSL report: `ValidationResults/20261028T153334475Z-47e673c95261446a98525e4548bd7780/report.json`.
- Infrastructure evidence: `ValidationResults/collector-verification-9f777e7115584b2bb6bd835ce96e601e/checks.json`.
- Assets/Packages/ProjectSettings: 1,695 files matched the earlier audit SHA-256 baseline, with no differences.
- Actual game tests and fresh compilation were intentionally not run. P1 implementation checks passing do not mean the game passed regression tests.
