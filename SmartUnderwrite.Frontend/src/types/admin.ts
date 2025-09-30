export interface Rule {
  id: number;
  name: string;
  description: string;
  conditions: string;
  actions: string;
  isActive: boolean;
  priority: number;
  createdAt: string;
  updatedAt: string;
  createdBy: string;
}

export interface CreateRuleRequest {
  name: string;
  description: string;
  conditions: string;
  actions: string;
  isActive: boolean;
  priority: number;
}

export interface UpdateRuleRequest extends CreateRuleRequest {
  id: number;
}

export interface Affiliate {
  id: number;
  name: string;
  contactEmail: string;
  contactPhone: string;
  address: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  userCount: number;
}

export interface CreateAffiliateRequest {
  name: string;
  contactEmail: string;
  contactPhone: string;
  address: string;
  isActive: boolean;
}

export interface UpdateAffiliateRequest extends CreateAffiliateRequest {
  id: number;
}

export interface AuditLog {
  id: number;
  userId: number;
  userName: string;
  action: string;
  entityType: string;
  entityId: string;
  oldValues?: string;
  newValues?: string;
  timestamp: string;
  ipAddress: string;
  userAgent: string;
}

export interface AuditLogFilter {
  userId?: number;
  action?: string;
  entityType?: string;
  fromDate?: string;
  toDate?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface ReportData {
  totalApplications: number;
  approvedApplications: number;
  rejectedApplications: number;
  pendingApplications: number;
  averageProcessingTime: number;
  approvalRate: number;
  totalLoanAmount: number;
  averageLoanAmount: number;
  topAffiliates: Array<{
    affiliateId: number;
    affiliateName: string;
    applicationCount: number;
    approvalRate: number;
  }>;
  dailyStats: Array<{
    date: string;
    applications: number;
    approvals: number;
    rejections: number;
  }>;
}
