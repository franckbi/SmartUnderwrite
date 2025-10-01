import { apiClient } from "./apiClient";
import {
  Rule,
  CreateRuleRequest,
  UpdateRuleRequest,
  Affiliate,
  CreateAffiliateRequest,
  UpdateAffiliateRequest,
  AuditLog,
  AuditLogFilter,
  ReportData,
} from "@/types/admin";
import { PagedResult } from "@/types/api";

export class AdminService {
  // Rules Management
  async getRules(): Promise<Rule[]> {
    return await apiClient.get<Rule[]>("/rules");
  }

  async getRule(id: number): Promise<Rule> {
    return await apiClient.get<Rule>(`/rules/${id}`);
  }

  async createRule(request: CreateRuleRequest): Promise<Rule> {
    return await apiClient.post<Rule>("/rules", request);
  }

  async updateRule(request: UpdateRuleRequest): Promise<Rule> {
    return await apiClient.put<Rule>(`/rules/${request.id}`, request);
  }

  async deleteRule(id: number): Promise<void> {
    return await apiClient.delete<void>(`/rules/${id}`);
  }

  async validateRule(
    conditions: string,
    actions: string
  ): Promise<{ isValid: boolean; errors: string[] }> {
    return await apiClient.post<{ isValid: boolean; errors: string[] }>(
      "/rules/validate",
      {
        conditions,
        actions,
      }
    );
  }

  // Affiliates Management
  async getAffiliates(): Promise<Affiliate[]> {
    return await apiClient.get<Affiliate[]>("/affiliates");
  }

  async getAffiliate(id: number): Promise<Affiliate> {
    return await apiClient.get<Affiliate>(`/affiliates/${id}`);
  }

  async createAffiliate(request: CreateAffiliateRequest): Promise<Affiliate> {
    return await apiClient.post<Affiliate>("/affiliates", request);
  }

  async updateAffiliate(
    request: UpdateAffiliateRequest & { id: number }
  ): Promise<Affiliate> {
    return await apiClient.put<Affiliate>(`/affiliates/${request.id}`, {
      name: request.name,
      externalId: request.externalId,
      isActive: request.isActive,
    });
  }

  async deleteAffiliate(id: number): Promise<Affiliate> {
    return await apiClient.post<Affiliate>(`/affiliates/${id}/deactivate`);
  }

  // Audit Logs
  async getAuditLogs(
    filter: AuditLogFilter = {}
  ): Promise<PagedResult<AuditLog>> {
    const params = new URLSearchParams();

    if (filter.userId) params.append("userId", filter.userId.toString());
    if (filter.action) params.append("action", filter.action);
    if (filter.entityType) params.append("entityType", filter.entityType);
    if (filter.fromDate) params.append("fromDate", filter.fromDate);
    if (filter.toDate) params.append("toDate", filter.toDate);
    if (filter.pageNumber)
      params.append("pageNumber", filter.pageNumber.toString());
    if (filter.pageSize) params.append("pageSize", filter.pageSize.toString());

    const queryString = params.toString();
    const url = `/audit${queryString ? `?${queryString}` : ""}`;

    return await apiClient.get<PagedResult<AuditLog>>(url);
  }

  // Reports
  async getReportData(fromDate?: string, toDate?: string): Promise<ReportData> {
    const params = new URLSearchParams();
    if (fromDate) params.append("fromDate", fromDate);
    if (toDate) params.append("toDate", toDate);

    const queryString = params.toString();
    const url = `/reports/dashboard${queryString ? `?${queryString}` : ""}`;

    return await apiClient.get<ReportData>(url);
  }
}

export const adminService = new AdminService();
