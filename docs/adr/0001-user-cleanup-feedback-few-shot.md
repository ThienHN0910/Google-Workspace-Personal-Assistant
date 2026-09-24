# User-Guided Cleanup Feedback via Few-Shot Prompt Injection

To train Gemini AI on personal email deletion preferences without generating brittle or proliferated single-email Regex rules, user-submitted deletion feedback (tags and rationale) is persisted as `CleanupFeedback` and injected into the batch background cleanup prompt as few-shot examples (top 5–10 recent items), while immediately trashing the selected email in Gmail. Financial institutions (`ProtectedBankDomains`) are protected by strict prompt guardrails preventing domain-level regex generation.
