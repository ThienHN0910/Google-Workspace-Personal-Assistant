export interface ApiResponse<T = any> {
  success: boolean;
  message?: string;
  data?: T;
  errors?: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string;
  role: string;
}

export interface EmailMessage {
  id: string;
  threadId?: string;
  from: string;
  to?: string;
  subject: string;
  snippet?: string;
  body?: string;
  receivedAt?: string;
  isRead?: boolean;
  isStarred?: boolean;
  labelIds?: string[];
}

export interface CleanupRule {
  id: string;
  ruleName: string;
  senderPattern?: string;
  subjectPattern?: string;
  action: string;
  whitelistDomains?: string[];
  isActive: boolean;
  approvalStatus: string;
  source: string;
  confidenceScore?: number;
  executionCount?: number;
  lastExecutedAt?: string;
  createdAt: string;
}

export interface CleanupReview {
  id: string;
  emailId: string;
  sender: string;
  subject: string;
  snippet?: string;
  receivedAt?: string;
  proposedAction: 'Trash' | 'Keep' | string;
  reason: string;
  confidenceScore: number;
  ruleId?: string;
  status: 'Pending' | 'Kept' | 'Trashed' | 'ProcessingTrash' | string;
  createdAt: string;
}

export interface CleanupLog {
  id: string;
  ruleName: string;
  executedAt: string;
  totalProcessed: number;
  totalTrashed: number;
  totalSkipped: number;
  durationMs: number;
}

export interface SecurityAlert {
  id: string;
  alertType: string;
  title: string;
  description: string;
  severity: 'low' | 'medium' | 'high' | 'critical';
  fileId?: string;
  fileName?: string;
  isResolved: boolean;
  createdAt: string;
}

export interface BankTransaction {
  id: string;
  transactionDate: string;
  bankName: string;
  amount: number;
  type: string;
  description: string;
  balanceAfter?: number;
}

export interface TaskItem {
  id: string;
  title: string;
  due?: string;
  status: string;
  notes?: string;
}
