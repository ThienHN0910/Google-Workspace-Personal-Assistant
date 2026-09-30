"""Audit legacy cleanup rules. Dry-run by default; apply writes a local backup first.

Requires pymongo and python-dotenv. Reads MONGODB_CONNECTION_STRING and
MONGODB_DATABASE_NAME from the environment or the ignored root .env file.
No Gmail API calls are made.
"""

import argparse
import json
import os
from datetime import datetime, timezone
from pathlib import Path


VERCEL_RULE_ID = "6a9c0458c31984a2b041a780"
VERCEL_SENDER = r"^notifications@vercel\.com$"
VERCEL_SUBJECT = r"(?i)\b(failed\s+deployment|deployment\s+failed)\b"

# Explicit allowlist after review of each of the 42 rules on 2026-09-30.
SAFE_RULE_IDS = frozenset("""
6a9cad26c31984a2b041a9c1 6a9d1da2c31984a2b041ab27
6a9e8b3da3732a399a5e3f9d 6a9ed187a3732a399a5e4051
6aa02312b92e21a21ca63734 6aa2396dab16f33ebf6c0f89
6aa45dd5ab16f33ebf6c1a93 6aa7c5b6c47cd974e7b37463
6aa8d0e0c47cd974e7b37917 6aaaf564c47cd974e7b380ee
6aac46d8c47cd974e7b38589 6aafaeb4c47cd974e7b38f35
6ab0b9e3c47cd974e7b39335 6ab3e9b71a5b7b674ef86b7c
6ab413ca1a5b7b674ef872ee 6ab4ca81ff57085e6fc6ebd4
6ab557339f4a7a84ad7a59ab 6ab5814b9f4a7a84ad7a5efa
6ab77b94573221b4d8f44b66 6ab7c231573221b4d8f4551d
""".split())

# Account, login, security, broad cross-service, match-any, and ambiguous
# technical/social rules remain stored for rollback but cannot auto-trash.
DISABLE_RULE_IDS = frozenset("""
6aa1bad0b92e21a21ca63da2 6aa41795ab16f33ebf6c19a1
6aa56910c47cd974e7b36a7b 6aa6743ac47cd974e7b36f64
6aa88a90c47cd974e7b37824 6aa9a3d6c47cd974e7b37be2
6ab421cc1a5b7b674ef87440 6ab45a001a5b7b674ef87976
6ab4cef8f336a2b0fba89be0 6ab4cef9f336a2b0fba89be1
6ab556d79f4a7a84ad7a5887 6ab5570a9f4a7a84ad7a5935
6ab69a929f4a7a84ad7a7d98 6ab7de06573221b4d8f45abb
6ab8245e573221b4d8f463bf 6ab9ae0e573221b4d8f48d32
6ab9f46a573221b4d8f49571 6aba1e90573221b4d8f499a4
6abaff9c573221b4d8f4b357 6abb53ee573221b4d8f4bd99
6abbd288573221b4d8f4cae7
""".split())

EXPECTED_RULE_IDS = SAFE_RULE_IDS | DISABLE_RULE_IDS | {VERCEL_RULE_ID}
BACKUP_FIELDS = ("action", "isActive", "approvalStatus", "senderRegex",
                 "subjectRegex", "bodyRegex")


def redact_rule(rule):
    """Return only identifiers, action, and regex settings; never body/snippet/secrets."""
    result = {"id": str(rule["_id"])}
    result.update({key: rule[key] for key in BACKUP_FIELDS if key in rule})
    return result


def plan_rule(rule):
    rid = str(rule["_id"])
    before = redact_rule(rule)
    after = {}
    if rid == VERCEL_RULE_ID:
        after = {"senderRegex": VERCEL_SENDER, "subjectRegex": VERCEL_SUBJECT,
                 "isActive": bool(rule.get("isActive", True)),
                 "approvalStatus": 1 if rule.get("isActive", True) else 0}
        reason = "Verified Vercel failed deployment only"
    elif rid in SAFE_RULE_IDS:
        after = {"isActive": bool(rule.get("isActive", True)),
                 "approvalStatus": 1 if rule.get("isActive", True) else 0}
        reason = "Narrow promotional or newsletter subject and sender"
    elif rid in DISABLE_RULE_IDS:
        after = {"isActive": False, "approvalStatus": 0}
        reason = "Broad, account/security, or ambiguous technical rule"
    else:
        reason = "Unknown rule: left untouched"
    changed = any(rule.get(key) != value for key, value in after.items())
    return {"id": rid, "before": before, "after": after, "reason": reason,
            "changed": changed}


def plan_inventory(rules):
    return [plan_rule(rule) for rule in rules]


def matches_reviewed_snapshot(planned, reviewed):
    if not reviewed or reviewed[0].get("mode") != "dry-run":
        return False
    expected_before = {item["id"]: item["before"] for item in reviewed[1:]}
    return len(expected_before) == len(planned) and all(
        expected_before.get(item["id"]) == item["before"] for item in planned)


def config():
    from dotenv import dotenv_values
    values = dotenv_values(Path(__file__).resolve().parents[1] / ".env")
    uri = os.environ.get("MONGODB_CONNECTION_STRING") or values.get("MONGODB_CONNECTION_STRING")
    database = os.environ.get("MONGODB_DATABASE_NAME") or values.get("MONGODB_DATABASE_NAME")
    if not uri or not database:
        raise RuntimeError("MongoDB environment variables are missing")
    return uri, database


def backup_path():
    root = Path(os.environ.get("LOCALAPPDATA") or Path.home()) / "GOpsHub" / "cleanup-rule-audits"
    root.mkdir(parents=True, exist_ok=True)
    return root / f"cleanup-audit-{datetime.now(timezone.utc):%Y%m%d-%H%M%S}.json"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true", help="Apply audited changes after a local backup")
    parser.add_argument("--report", type=Path, help="Write UTF-8 dry-run report")
    parser.add_argument("--reviewed-report", type=Path,
                        help="Required for apply; compare every live rule to the reviewed dry-run")
    args = parser.parse_args()
    if args.apply and not args.reviewed_report:
        parser.error("--apply requires --reviewed-report")

    from pymongo import MongoClient
    uri, name = config()
    db = MongoClient(uri, serverSelectionTimeoutMS=10000)[name]
    rules = list(db.cleanup_rules.find({}))
    planned = plan_inventory(rules)
    observed = {item["id"] for item in planned}
    pending = list(db.email_action_logs.find({"action": "PendingApproval"},
                                             {"_id": 1, "emailId": 1, "sender": 1,
                                              "subject": 1, "reason": 1}))
    summary = {"mode": "apply" if args.apply else "dry-run", "rules": len(rules),
               "known": len(observed & EXPECTED_RULE_IDS),
               "unknown": len(observed - EXPECTED_RULE_IDS),
               "changes": sum(item["changed"] for item in planned),
               "legacyPending": len(pending)}
    lines = [json.dumps(summary, ensure_ascii=False)] + [
        json.dumps(item, ensure_ascii=False) for item in planned]
    for line in lines:
        print(line)

    if args.report and not args.apply:
        args.report.write_text("\n".join(lines) + "\n", encoding="utf-8")

    if not args.apply:
        return
    reviewed = [json.loads(line) for line in args.reviewed_report.read_text(encoding="utf-8").splitlines()]
    if not matches_reviewed_snapshot(planned, reviewed):
        raise RuntimeError("Live rule inventory differs from reviewed dry-run; re-audit")
    if not EXPECTED_RULE_IDS.issubset(observed):
        raise RuntimeError("Expected 42-rule inventory changed; re-audit before applying")
    backup = backup_path()
    backup.write_text(json.dumps({"rules": [redact_rule(rule) for rule in rules],
                                  "legacyPendingIds": [str(log["_id"]) for log in pending]},
                                 ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"backup": str(backup)}))

    updated = 0
    for rule, item in zip(rules, planned):
        if not item["changed"]:
            continue
        # Compare current fields with the snapshot; fail if another writer edited it.
        condition = {"_id": rule["_id"]}
        for key in item["after"]:
            condition[key] = rule[key] if key in rule else {"$exists": False}
        outcome = db.cleanup_rules.update_one(condition, {"$set": item["after"]})
        if outcome.matched_count != 1:
            raise RuntimeError(f"Concurrent rule edit detected: {item['id']}")
        updated += 1

    imported = 0
    now = datetime.now(timezone.utc)
    for log in pending:
        email_id = log.get("emailId")
        if not email_id:
            continue
        outcome = db.cleanup_reviews.update_one({"emailId": email_id}, {"$setOnInsert": {
            "emailId": email_id, "status": 0, "sender": log.get("sender") or "",
            "subject": log.get("subject"), "snippet": None,
            "aiReason": log.get("reason") or "Legacy pending review",
            "createdAt": now, "updatedAt": now}}, upsert=True)
        if outcome.upserted_id:
            imported += 1
    print(json.dumps({"updatedRules": updated, "importedReviews": imported}))


if __name__ == "__main__":
    main()
