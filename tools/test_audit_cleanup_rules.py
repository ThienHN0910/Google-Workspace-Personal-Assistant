import unittest

from tools.audit_cleanup_rules import EXPECTED_RULE_IDS, plan_rule, redact_rule, plan_inventory, matches_reviewed_snapshot


class CleanupRuleAuditTests(unittest.TestCase):
    def test_inventory_covers_all_42_known_rules(self):
        self.assertEqual(42, len(EXPECTED_RULE_IDS))
        changes = plan_inventory([{"_id": rid, "isActive": True} for rid in EXPECTED_RULE_IDS])
        self.assertEqual(42, len(changes))
        self.assertEqual(set(EXPECTED_RULE_IDS), {item["id"] for item in changes})

    def test_match_any_rules_are_disabled(self):
        for rid in ("6aba1e90573221b4d8f499a4", "6abaff9c573221b4d8f4b357"):
            planned = plan_rule({"_id": rid, "isActive": True, "senderRegex": "(?i).*"})
            self.assertFalse(planned["after"]["isActive"])
            self.assertEqual(0, planned["after"]["approvalStatus"])

    def test_sensitive_google_and_github_rules_are_disabled(self):
        for rid in ("6ab4cef8f336a2b0fba89be0", "6ab4cef9f336a2b0fba89be1",
                    "6ab5570a9f4a7a84ad7a5935", "6abb53ee573221b4d8f4bd99"):
            self.assertFalse(plan_rule({"_id": rid, "isActive": True})["after"]["isActive"])

    def test_vercel_rule_is_narrowed_and_approved(self):
        planned = plan_rule({"_id": "6a9c0458c31984a2b041a780", "isActive": True,
                             "senderRegex": "old", "subjectRegex": "old"})
        after = planned["after"]
        self.assertEqual("^notifications@vercel\\.com$", after["senderRegex"])
        self.assertIn("failed", after["subjectRegex"])
        self.assertTrue(after["isActive"])
        self.assertEqual(1, after["approvalStatus"])

    def test_second_run_is_idempotent_and_unknown_rule_untouched(self):
        old = {"_id": "6aba1e90573221b4d8f499a4", "isActive": True, "senderRegex": "(?i).*"}
        first = plan_rule(old)
        self.assertTrue(first["changed"])
        second = plan_rule({**old, **first["after"]})
        self.assertFalse(second["changed"])
        self.assertFalse(plan_rule({"_id": "new-rule", "isActive": True})["changed"])

    def test_report_excludes_private_content_and_credentials(self):
        safe = redact_rule({"_id": "r", "senderRegex": "^sender@example.com$",
                            "subjectRegex": "Sale", "body": "private body",
                            "snippet": "private snippet", "MONGODB_CONNECTION_STRING": "secret"})
        self.assertEqual({"id": "r", "senderRegex": "^sender@example.com$", "subjectRegex": "Sale"}, safe)

    def test_apply_requires_unchanged_reviewed_inventory(self):
        planned = plan_inventory([{"_id": "6aba1e90573221b4d8f499a4", "isActive": True}])
        report = [{"mode": "dry-run"}, *planned]
        self.assertTrue(matches_reviewed_snapshot(planned, report))
        changed = plan_inventory([{"_id": "6aba1e90573221b4d8f499a4", "isActive": False}])
        self.assertFalse(matches_reviewed_snapshot(changed, report))


if __name__ == "__main__":
    unittest.main()
