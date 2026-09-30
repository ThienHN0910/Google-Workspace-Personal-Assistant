# User-Guided Cleanup Feedback via Few-Shot Prompt Injection

Status: superseded by ADR-0002.

The original decision saved deletion examples and injected recent examples into Gemini prompts. It used "train" language, even though each Gemini call is independent. It also relied on prompt guardrails for financial mail and did not record Keep decisions. ADR-0002 replaces those assumptions with persistent user decisions and server-side safety checks.
