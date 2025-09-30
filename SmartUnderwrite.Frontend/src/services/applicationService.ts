import { apiClient } from "./apiClient";
import {
  LoanApplication,
  CreateApplicationRequest,
  ApplicationFilter,
  DocumentUploadRequest,
  Document,
} from "@/types/application";
import { PagedResult } from "@/types/api";

export class ApplicationService {
  async getApplications(
    filter: ApplicationFilter = {}
  ): Promise<PagedResult<LoanApplication>> {
    const params = new URLSearchParams();

    if (filter.status) params.append("status", filter.status);
    if (filter.affiliateId)
      params.append("affiliateId", filter.affiliateId.toString());
    if (filter.fromDate) params.append("fromDate", filter.fromDate);
    if (filter.toDate) params.append("toDate", filter.toDate);
    if (filter.pageNumber)
      params.append("pageNumber", filter.pageNumber.toString());
    if (filter.pageSize) params.append("pageSize", filter.pageSize.toString());

    const queryString = params.toString();
    const url = `/applications${queryString ? `?${queryString}` : ""}`;

    return await apiClient.get<PagedResult<LoanApplication>>(url);
  }

  async getApplication(id: number): Promise<LoanApplication> {
    return await apiClient.get<LoanApplication>(`/applications/${id}`);
  }

  async createApplication(
    request: CreateApplicationRequest
  ): Promise<LoanApplication> {
    return await apiClient.post<LoanApplication>("/applications", request);
  }

  async uploadDocument(
    applicationId: number,
    request: DocumentUploadRequest
  ): Promise<Document> {
    const formData = new FormData();
    formData.append("file", request.file);
    if (request.description) {
      formData.append("description", request.description);
    }

    // Override content type for file upload
    const response = await fetch(
      `/api/applications/${applicationId}/documents`,
      {
        method: "POST",
        headers: {
          Authorization: `Bearer ${localStorage.getItem("token")}`,
        },
        body: formData,
      }
    );

    if (!response.ok) {
      const error = await response.json();
      throw error;
    }

    return await response.json();
  }

  async downloadDocument(documentId: number): Promise<Blob> {
    const response = await fetch(`/api/documents/${documentId}`, {
      headers: {
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
    });

    if (!response.ok) {
      throw new Error("Failed to download document");
    }

    return await response.blob();
  }

  async evaluateApplication(applicationId: number): Promise<LoanApplication> {
    return await apiClient.post<LoanApplication>(
      `/applications/${applicationId}/evaluate`
    );
  }
}

export const applicationService = new ApplicationService();
