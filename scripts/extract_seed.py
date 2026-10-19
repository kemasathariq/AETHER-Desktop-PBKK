#!/usr/bin/env python3
"""Builds src/Aether.Infrastructure/Seed/aether_seed.json from references/aether_source/bundle.js.

The reference file is only read, never modified. Run from the repository root:
    python3 scripts/extract_seed.py
Only fields that exist in the source are copied; nothing is generated.
"""
import json
import re
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "references" / "aether_source" / "bundle.js"
OUT = ROOT / "src" / "Aether.Infrastructure" / "Seed" / "aether_seed.json"


def load_bundle():
    text = SRC.read_text(encoding="utf-8")
    return json.loads(text[text.index("{"): text.rindex("}") + 1])


def iso_date(value):
    """Accepts '2026-04-29' or '30-Apr-2026'; returns ISO text or None."""
    if not value:
        return None
    for fmt in ("%Y-%m-%d", "%d-%b-%Y"):
        try:
            return datetime.strptime(value, fmt).strftime("%Y-%m-%d")
        except ValueError:
            pass
    return None


def split_header(header):
    m = re.match(r"^(.*?)\s*\((.*)\)$", header)
    return (m.group(1), m.group(2)) if m else (header, None)


def main():
    data = load_bundle()
    incidents = []
    for i in data["incidents"]:
        incidents.append({
            "sourceSerialNo": i["serial_no"],
            "mtoNo": i["mto_no"],
            "arNo": i["ar_no"],
            "plant": i["plant"],
            "tagNumber": i["tag_number"],
            "eqClass": i["eq_class"],
            "eqType": i["eq_type"],
            "discipline": i["discipline"],
            "component": i["component"],
            "failureMechanism": i["f_mechanism"],
            "title": i["risk_case_title"],
            "highestImpact": i["highest_impact"],
            "preRisk": i["pre_risk"],
            "riskScore": i["risk_score"],
            "picRca": i["pic_rca"],
            "status": i["overall_status"],
            "occurredOn": iso_date(i["date_occur"]),
            "rcaDueDate": iso_date(i.get("rca_due_date")),
            "downtimeHours": i["downtime_hrs"],
            "actualLossKUsd": i["act_loss_k_usd"],
            "potentialLossKUsd": i["pot_loss_k_usd"],
        })

    assets, records, actions = [], [], []
    ar_to_serial = {i["ar_no"]: i["serial_no"] for i in data["incidents"]}
    for tag, m in data["equipment_master"].items():
        assets.append({
            "tag": tag,
            "name": m["Equipment Name"],
            "type": m["Equipment Type"],
            "eqClass": m["Equipment Class"],
            "plantUnit": m["Plant / Unit"],
            "discipline": m["Discipline"],
            "criticality": m["Criticality"],
            "designLife": m["Design Life"],
            "monitoringMethod": m["Monitoring Method"],
            "linkedArNo": m["Linked RCA / AR No."],
            "dominantFailureMode": m["Dominant Failure Mode"],
        })
        limits = {l["parameter"]: l for l in data["hero_cases"][tag]["limits"]}
        for w in data["weekly_condition"][tag]:
            for key, value in w.items():
                if key in ("Week", "Date", "Health Status", "Remark"):
                    continue
                name, unit = split_header(key)
                lim = limits.get(key)
                records.append({
                    "assetTag": tag,
                    "weekNo": w["Week"],
                    "measuredOn": iso_date(w["Date"]),
                    "parameter": name,
                    "unit": unit,
                    "value": value,
                    "alarmLimit": lim["alarm"] if lim else None,
                    "tripLimit": lim["trip"] if lim else None,
                    "limitDirection": lim["direction"] if lim else None,
                    "healthStatus": w["Health Status"],
                    "remark": w["Remark"] or None,
                })
        hero = data["hero_cases"][tag]
        serial = ar_to_serial.get(hero["AR Number"])
        for category, key in (("Corrective", "corrective"), ("Proactive", "proactive")):
            for a in hero[key]:
                actions.append({
                    "incidentSerialNo": serial,
                    "description": a["action"],
                    "owner": a["pic"],
                    "targetDate": iso_date(a["plan_date"]),
                    "status": a["status"],
                    "category": category,
                    "rootCauseCode": a["rc"],
                })

    out = {
        "seedVersion": 1,
        "source": "references/aether_source/bundle.js (CALIBER 2026 Case 2 data)",
        "incidents": incidents,
        "assets": assets,
        "conditionRecords": records,
        "actions": actions,
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(out, ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"incidents={len(incidents)} assets={len(assets)} records={len(records)} actions={len(actions)}")


if __name__ == "__main__":
    main()
