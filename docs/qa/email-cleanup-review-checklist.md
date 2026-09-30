# Email cleanup review checklist

1. Run cleanup with a new unread promotional message. Confirm it appears under **Chờ duyệt dọn dẹp** and Gmail has not moved it.
2. Try Xóa and Giữ with a blank reason. Confirm both controls are disabled. Enter a reason, choose Giữ, then confirm the message remains in Inbox and the saved example shows Giữ plus the reason.
3. Choose Xóa on another pending review with a reason. Confirm Gmail moves it to Trash once, the pending count decreases, and the reason appears in saved examples.
4. Create a regex draft, open its preview, and check sample sender, subject, and blockers. A rule matching a Keep sample, pending review, or protected alert must not activate. A narrow valid rule can activate after a fresh preview.
5. Confirm there is no Archive action in the cleanup review, rule editor, or cleanup summary. Historical Archive logs may still display as history.
6. Confirm the manual reason dialog has no preselected tag and explains that saved examples are sent with later AI requests, without claiming model training or memory.
