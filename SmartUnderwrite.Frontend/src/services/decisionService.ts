import { apiClient } from "./apiClient";
import {
  DecisionRequest,
  DecisionResponse,
  DecisionFilter,
  DecisionSummary,
} from "@/types/decision";
import { PagedResult } from "@/types/api";
import { LoanApplication } from "@/types/application";

export class DecisionService {
  async getDecisions(
    filter: DecisionFilter = {}
  ): Promise<PagedResult<DecisionResponse>> {
    const params = new URLSearchParams();

    if (filter.outcome) params.append("outcome", filter.outcome);
    if (filter.isManual !== undefined)
      params.append("isManual", filter.isManual.toString());
    if (filter.fromDate) params.append("fromDate", filter.fromDate);
    if (filter.toDate) params.append("toDate", filter.toDate);
    if (filter.pageNumber)
      params.append("pageNumber", filter.pageNumber.toString());
    if (filter.pageSize) params.append("pageSize", filter.pageSize.toString());

    const queryString = params.toString();
    const url = `/decisions${queryString ? `?${queryString}` : ""}`;

    return await apiClient.get<PagedResult<DecisionResponse>>(url);
  }

  async getDecision(id: number): Promise<DecisionResponse> {
    return await apiClient.get<DecisionResponse>(`/decisions/${id}`);
  }

  async makeDecision(request: DecisionRequest): Promise<LoanApplication> {
    return await apiClient.post<LoanApplication>("/decisions", request);
  }

  async getDecisionSummary(): Promise<DecisionSummary> {
    return await apiClient.get<DecisionSummary>("/decisions/summary");
  }

  async getPendingApplications(): Promise<PagedResult<LoanApplication>> {
    return await apiClient.get<PagedResult<LoanApplication>>(
      "/applications?status=ManualReview"
    );
  }
}

export const decisionService = new DecisionService();
