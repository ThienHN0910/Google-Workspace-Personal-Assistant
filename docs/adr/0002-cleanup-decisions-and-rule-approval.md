# Persist cleanup decisions and approve regex before activation

The cleanup system saves both Trash and Keep decisions with the user's reason and a short email snapshot. Each Gemini request receives relevant saved examples; the model itself is not trained and has no persistent session. A new email type goes to human review, while a known exact type may be moved to Trash only after server-side sender, subject, and safety checks. AI regex suggestions remain inactive drafts until a fresh server-side preview and approval.

This favors slower initial cleanup over accidental deletion. The user explicitly allowed verified Vercel failed-deployment notices as an exception to the normal technical-alert protection. Financial senders remain protected from automatic Trash. Cleanup does not Archive; historical Archive logs remain readable.
