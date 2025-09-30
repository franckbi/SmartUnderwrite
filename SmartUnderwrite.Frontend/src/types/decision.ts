export interface DecisionRequest {
  applicationId: number;
  outcome: DecisionOutcome;
  reasons: string[];
  notes?: string;
}

export interface DecisionResponse {
  id: number;
  applicationId: number;
  outcome: DecisionOutcome;
  score: number;
  reasons: string[];
  notes?: string;
  decidedByUserId: number;
  decidedByUser: string;
  decidedAt: string;
  isManual: boolean;
}

export enum DecisionOutcome {
  Approve = "Approve",
  Reject = "Reject",
  ManualReview = "ManualReview",
}

export interface DecisionFilter {
  outcome?: DecisionOutcome;
  isManual?: boolean;
  fromDate?: string;
  toDate?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface DecisionSummary {
  totalDecisions: number;
  approvedCount: number;
  rejectedCount: number;
  manualReviewCount: number;
  averageScore: number;
  manualDecisionCount: number;
  automatedDecisionCount: number;
}
