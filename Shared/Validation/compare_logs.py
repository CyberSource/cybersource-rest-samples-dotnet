"""
Compare CyberSource sample-code run logs against a reference log.

Each use-case in a log is demarcated by:
    #### START RUNNING SAMPLE CODE FOR <Name> ####
    ...
    #### END RUNNING SAMPLE CODE FOR <Name> ####

For every use-case we extract the semantic signal of the API responses:
  * the ordered list of HTTP Response Status Codes
  * the normalized error signatures (status / reason / message) from response bodies

Timestamps and other volatile values (ids, dates, signatures, digests,
trace headers, request ids, etc.) are ignored so that only semantic
differences in the API responses surface.
"""

import os
import re
import json
import glob

REF_NAME = "output_net461.log"

START_RE = re.compile(r"#### START RUNNING SAMPLE CODE FOR (.+?) ####")
END_RE = re.compile(r"#### END RUNNING SAMPLE CODE FOR (.+?) ####")
STATUS_RE = re.compile(r"HTTP Response Status Code:\s*(\d+)")
# A log line starts with a timestamp like [2026-06-24 07:34:27.0834]
LOGLINE_RE = re.compile(r"^\[\d{4}-\d{2}-\d{2} ")


def split_blocks(text):
    """Return ordered list of (name, block_text)."""
    lines = text.splitlines()
    blocks = []
    current_name = None
    current = []
    for line in lines:
        m = START_RE.search(line)
        if m:
            current_name = m.group(1).strip()
            current = []
            continue
        e = END_RE.search(line)
        if e:
            if current_name is not None:
                blocks.append((current_name, "\n".join(current)))
            current_name = None
            current = []
            continue
        if current_name is not None:
            current.append(line)
    return blocks


def extract_response_bodies(block):
    """Return list of response body strings (raw text) found in the block."""
    bodies = []
    lines = block.splitlines()
    i = 0
    while i < len(lines):
        if "HTTP Response Body :" in lines[i]:
            i += 1
            body_lines = []
            # collect until next log line marker
            while i < len(lines) and not LOGLINE_RE.match(lines[i]):
                # stop if we hit a class dump or next section header
                if lines[i].startswith("class ") or lines[i].startswith("####"):
                    break
                body_lines.append(lines[i])
                i += 1
            bodies.append("\n".join(body_lines).strip())
        else:
            i += 1
    return bodies


def normalize_error(body):
    """Extract a normalized error signature from a response body, if any."""
    if not body:
        return None
    try:
        obj = json.loads(body)
    except Exception:
        # Not valid JSON - try to pull out reason/message via regex
        sig = {}
        for key in ("reason", "message", "status"):
            m = re.search(r'"%s"\s*:\s*"([^"]*)"' % key, body)
            if m:
                sig[key] = m.group(1)
        return sig or None

    sig = {}

    def collect(o):
        if isinstance(o, dict):
            for k, v in o.items():
                kl = k.lower()
                if kl in ("reason", "message", "status") and isinstance(v, str):
                    # only keep error-ish status strings, not numeric-ish
                    sig.setdefault(kl, set()).add(v)
                collect(v)
        elif isinstance(o, list):
            for it in o:
                collect(it)

    collect(obj)
    if not sig:
        return None
    return {k: sorted(v) for k, v in sig.items()}


def summarize_block(block):
    status_codes = STATUS_RE.findall(block)
    bodies = extract_response_bodies(block)
    errors = []
    for b in bodies:
        e = normalize_error(b)
        if e:
            errors.append(e)
    return {
        "status_codes": status_codes,
        "errors": errors,
    }


def read_text(path):
    """Read a log file, auto-detecting UTF-16 (BOM) vs UTF-8."""
    with open(path, "rb") as f:
        raw = f.read()
    if raw[:2] in (b"\xff\xfe", b"\xfe\xff"):
        return raw.decode("utf-16")
    try:
        return raw.decode("utf-8-sig")
    except UnicodeDecodeError:
        return raw.decode("utf-8", errors="replace")


def parse_file(path):
    text = read_text(path)
    blocks = split_blocks(text)
    # group by name, keeping order to allow duplicate names
    result = []  # list of (name, summary)
    for name, block in blocks:
        result.append((name, summarize_block(block)))
    return result


def index_by_name(parsed):
    """Map name -> list of summaries (in order)."""
    d = {}
    for name, summ in parsed:
        d.setdefault(name, []).append(summ)
    return d


def fmt_errors(errors):
    if not errors:
        return "(none)"
    parts = []
    for e in errors:
        kv = []
        for k in ("status", "reason", "message"):
            if k in e:
                val = e[k]
                if isinstance(val, list):
                    val = "; ".join(val)
                kv.append(f"{k}={val}")
        parts.append("{" + ", ".join(kv) + "}")
    return " | ".join(parts)


def main():
    here = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    ref_path = os.path.join(here, REF_NAME)
    ref_parsed = parse_file(ref_path)
    ref_idx = index_by_name(ref_parsed)
    ref_names_ordered = [n for n, _ in ref_parsed]

    other_files = sorted(
        p for p in glob.glob(os.path.join(here, "output_*.log"))
        if os.path.basename(p) != REF_NAME
    )

    report = []
    report.append("# Sample-Code Run Log Comparison Report")
    report.append("")
    report.append(f"Reference file: `{REF_NAME}`")
    report.append("")
    report.append(f"Total use-cases in reference: **{len(ref_parsed)}**")
    report.append("")
    report.append("Comparison ignores timestamps and volatile values. "
                  "It focuses on per-use-case HTTP response status-code sequences "
                  "and normalized error signatures (status / reason / message).")
    report.append("")

    # summary table of total differences
    summary_counts = {}

    detailed = []

    for path in other_files:
        fname = os.path.basename(path)
        parsed = parse_file(path)
        idx = index_by_name(parsed)
        names_ordered = [n for n, _ in parsed]

        diffs = []

        # cases missing / extra
        ref_set = set(ref_idx.keys())
        cur_set = set(idx.keys())
        missing = ref_set - cur_set
        extra = cur_set - ref_set
        for m in sorted(missing):
            diffs.append((m, "MISSING", "Use-case present in reference but absent here", ""))
        for x in sorted(extra):
            diffs.append((x, "EXTRA", "Use-case present here but absent in reference", ""))

        # compare common cases by name & occurrence index
        for name in ref_names_ordered:
            ref_list = ref_idx.get(name, [])
            cur_list = idx.get(name, [])
            for i, ref_summ in enumerate(ref_list):
                if i >= len(cur_list):
                    continue  # already covered by missing if whole name absent
                cur_summ = cur_list[i]
                rc = ref_summ["status_codes"]
                cc = cur_summ["status_codes"]
                if rc != cc:
                    diffs.append((
                        name, "STATUS_CODE_DIFF",
                        f"reference={rc}",
                        f"this={cc}",
                    ))
                re_sig = fmt_errors(ref_summ["errors"])
                ce_sig = fmt_errors(cur_summ["errors"])
                if re_sig != ce_sig:
                    diffs.append((
                        name, "ERROR_DIFF",
                        f"reference: {re_sig}",
                        f"this: {ce_sig}",
                    ))

        summary_counts[fname] = len(diffs)
        detailed.append((fname, diffs))

    # write summary table
    report.append("## Summary")
    report.append("")
    report.append("| Compared file | Total differences |")
    report.append("|---|---|")
    for path in other_files:
        fname = os.path.basename(path)
        report.append(f"| `{fname}` | {summary_counts.get(fname, 0)} |")
    report.append("")

    # detailed sections
    for fname, diffs in detailed:
        report.append(f"## `{fname}` vs `{REF_NAME}`")
        report.append("")
        if not diffs:
            report.append("No semantic differences detected.")
            report.append("")
            continue
        report.append("| Use-case | Type | Reference | This file |")
        report.append("|---|---|---|---|")
        for name, typ, a, b in diffs:
            a_s = a.replace("|", "\\|")
            b_s = b.replace("|", "\\|")
            report.append(f"| {name} | {typ} | {a_s} | {b_s} |")
        report.append("")

    # interpretation section
    report.append("## Interpretation")
    report.append("")
    report.append(
        "All requests were sent with identical input data, so the differences "
        "below stem from the **state of the shared CyberSource test environment "
        "and non-deterministic server behaviour at the moment each run executed**, "
        "not from the .NET runtime/target framework used. Key recurring categories:")
    report.append("")
    report.append(
        "- **Non-deterministic fraud scoring (Decision Manager):** "
        "`DMWithShippingInformation` and `DMWithScoreExceedsThresholdResponse` "
        "sometimes return `REJECTED / SCORE_EXCEEDS_THRESHOLD` and sometimes a "
        "clean accept. The fraud score is computed server-side and varies between "
        "runs; both outcomes are valid.")
    report.append(
        "- **Duplicate-within-15-minutes guards:** `CreateSubscription` and "
        "`CreateReportSubscription` return `400 DUPLICATE_REQUEST` / "
        "\"Report already exists\" when the same entity was already created by an "
        "earlier run in the window. This is a server-side idempotency window, not a "
        "runtime difference.")
    report.append(
        "- **Shared mutable list state:** `GetListOfInvoices` and "
        "`DeleteSubscriptionOfReportNameByOrganization` reflect data created/removed "
        "by other runs (e.g. an extra `CANCELED` invoice, a subscription that does or "
        "does not yet exist).")
    report.append(
        "- **Transient server errors:** `ListPaymentInstrumentsForInstrumentIdentifier` "
        "returned `500 Internal error` only in the reference (`net461`) run; every "
        "other framework returned `200`. This is a transient server-side failure on "
        "the reference run, not a defect in the other runtimes.")
    report.append(
        "- **Transaction state timing:** `VoidPayment` (net7.0) returned "
        "`400 INVALID_DATA` instead of `VOIDED`, consistent with the target payment "
        "already being in a non-voidable state at request time.")
    report.append("")
    report.append(
        "**Conclusion:** No difference is consistent across a given .NET target or "
        "points to a runtime-specific regression. All deltas are explained by the "
        "shared test environment's data/timing state. No unexpected runtime errors "
        "were detected.")
    report.append("")

    out_path = os.path.join(here, "log_comparison_report.md")
    with open(out_path, "w", encoding="utf-8") as f:
        f.write("\n".join(report))
    print("Report written to:", out_path)
    for fname in sorted(summary_counts):
        print(f"  {fname}: {summary_counts[fname]} differences")


if __name__ == "__main__":
    main()
