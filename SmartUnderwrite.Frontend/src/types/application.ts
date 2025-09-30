export interface Applicant {
  id: number;
  firstName: string;
  lastName: string;
  ssnHash: string;
  dateOfBirth: string;
  address: Address;
  phone: string;
  email: string;
}

export interface Address {
  street: string;
  city: string;
  state: string;
  zipCode: string;
}

export interface LoanApplication {
  id: number;
  affiliateId: number;
  applicant: Applicant;
  productType: string;
  amount: number;
  incomeMonthly: number;
  employmentType: string;
  creditScore?: number;
  status: ApplicationStatus;
  createdAt: string;
  updatedAt: string;
  documents: Document[];
  decisions: Decision[];
}

export interface Document {
  id: number;
  loanApplicationId: number;
  fileName: string;
  fileSize: number;
  contentType: string;
  uploadedAt: string;
  uploadedBy: string;
}

export interface Decision {
  id: number;
  loanApplicationId: number;
  outcome: DecisionOutcome;
  score: number;
  reasons: string[];
  decidedByUserId?: number;
  decidedByUser?: string;
  decidedAt: string;
  isManual: boolean;
}

export enum ApplicationStatus {
  Submitted = "Submitted",
  Evaluated = "Evaluated",
  Approved = "Approved",
  Rejected = "Rejected",
  ManualReview = "ManualReview",
}

export enum DecisionOutcome {
  Approve = "Approve",
  Reject = "Reject",
  ManualReview = "ManualReview",
}

export interface CreateApplicationRequest {
  applicant: CreateApplicantRequest;
  productType: string;
  amount: number;
  incomeMonthly: number;
  employmentType: string;
  creditScore?: number;
}

export interface CreateApplicantRequest {
  firstName: string;
  lastName: string;
  ssn: string; // Will be hashed on server
  dateOfBirth: string;
  address: Address;
  phone: string;
  email: string;
}

export interface ApplicationFilter {
  status?: ApplicationStatus;
  affiliateId?: number;
  fromDate?: string;
  toDate?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface DocumentUploadRequest {
  file: File;
  description?: string;
}
