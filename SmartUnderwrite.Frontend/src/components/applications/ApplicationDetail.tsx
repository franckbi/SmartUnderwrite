import React, { useState, useEffect } from "react";
import {
  Box,
  Paper,
  Typography,
  Chip,
  Button,
  Card,
  CardContent,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  IconButton,
  Divider,
  Alert,
} from "@mui/material";
import {
  ArrowBack as BackIcon,
  Person as PersonIcon,
  AttachFile as FileIcon,
  Download as DownloadIcon,
  PlayArrow as EvaluateIcon,
} from "@mui/icons-material";
import { useParams, useNavigate } from "react-router-dom";
import {
  LoanApplication,
  ApplicationStatus,
  DecisionOutcome,
} from "@/types/application";
import { applicationService } from "@/services/applicationService";
import { useAuth } from "@/contexts/AuthContext";
import { UserRole } from "@/types/auth";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { useErrorHandler } from "@/hooks/useErrorHandler";

const statusColors: Record<
  ApplicationStatus,
  "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning"
> = {
  [ApplicationStatus.Submitted]: "info",
  [ApplicationStatus.Evaluated]: "primary",
  [ApplicationStatus.Approved]: "success",
  [ApplicationStatus.Rejected]: "error",
  [ApplicationStatus.ManualReview]: "warning",
};

const outcomeColors: Record<
  DecisionOutcome,
  "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning"
> = {
  [DecisionOutcome.Approve]: "success",
  [DecisionOutcome.Reject]: "error",
  [DecisionOutcome.ManualReview]: "warning",
};

export const ApplicationDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  const { error, handleError, clearError } = useErrorHandler();

  const [application, setApplication] = useState<LoanApplication | null>(null);
  const [loading, setLoading] = useState(true);
  const [evaluating, setEvaluating] = useState(false);

  const loadApplication = async () => {
    if (!id) return;

    try {
      setLoading(true);
      clearError();
      const result = await applicationService.getApplication(parseInt(id));
      setApplication(result);
    } catch (err) {
      handleError(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadApplication();
  }, [id]);

  const handleEvaluate = async () => {
    if (!application) return;

    try {
      setEvaluating(true);
      clearError();
      const result = await applicationService.evaluateApplication(
        application.id
      );
      setApplication(result);
    } catch (err) {
      handleError(err);
    } finally {
      setEvaluating(false);
    }
  };

  const handleDownloadDocument = async (
    documentId: number,
    fileName: string
  ) => {
    try {
      const blob = await applicationService.downloadDocument(documentId);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = fileName;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      handleError(err);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: "USD",
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  if (loading) {
    return <LoadingSpinner message="Loading application..." />;
  }

  if (!application) {
    return (
      <Box sx={{ p: 2 }}>
        <Typography variant="h6" color="error">
          Application not found
        </Typography>
        <Button
          startIcon={<BackIcon />}
          onClick={() => navigate("/applications")}
        >
          Back to Applications
        </Button>
      </Box>
    );
  }

  const latestDecision = application.decisions?.[0];
  const canEvaluate = hasRole(UserRole.Admin) || hasRole(UserRole.Underwriter);
  const needsEvaluation = application.status === ApplicationStatus.Submitted;

  return (
    <Box>
      <Box sx={{ mb: 3, display: "flex", alignItems: "center", gap: 2 }}>
        <Button
          startIcon={<BackIcon />}
          onClick={() => navigate("/applications")}
        >
          Back to Applications
        </Button>
        <Typography variant="h4" component="h1">
          Application #{application.id}
        </Typography>
        <Chip
          label={application.status}
          color={statusColors[application.status]}
        />
        {canEvaluate && needsEvaluation && (
          <Button
            variant="contained"
            startIcon={<EvaluateIcon />}
            onClick={handleEvaluate}
            disabled={evaluating}
          >
            {evaluating ? "Evaluating..." : "Evaluate"}
          </Button>
        )}
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
        {/* Applicant Information */}
        <Box sx={{ flex: "1 1 400px" }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <PersonIcon sx={{ mr: 1, verticalAlign: "middle" }} />
                Applicant Information
              </Typography>
              <Box sx={{ mt: 2 }}>
                <Typography>
                  <strong>Name:</strong> {application.applicant.firstName}{" "}
                  {application.applicant.lastName}
                </Typography>
                <Typography>
                  <strong>Email:</strong> {application.applicant.email}
                </Typography>
                <Typography>
                  <strong>Phone:</strong> {application.applicant.phone}
                </Typography>
                <Typography>
                  <strong>Date of Birth:</strong>{" "}
                  {new Date(
                    application.applicant.dateOfBirth
                  ).toLocaleDateString()}
                </Typography>
                <Typography>
                  <strong>Address:</strong>
                </Typography>
                <Typography sx={{ ml: 2 }}>
                  {application.applicant.address.street}
                  <br />
                  {application.applicant.address.city},{" "}
                  {application.applicant.address.state}{" "}
                  {application.applicant.address.zipCode}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Box>

        {/* Loan Information */}
        <Box sx={{ flex: "1 1 400px" }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Loan Information
              </Typography>
              <Box sx={{ mt: 2 }}>
                <Typography>
                  <strong>Product Type:</strong> {application.productType}
                </Typography>
                <Typography>
                  <strong>Amount:</strong> {formatCurrency(application.amount)}
                </Typography>
                <Typography>
                  <strong>Monthly Income:</strong>{" "}
                  {formatCurrency(application.incomeMonthly)}
                </Typography>
                <Typography>
                  <strong>Employment Type:</strong> {application.employmentType}
                </Typography>
                {application.creditScore && (
                  <Typography>
                    <strong>Credit Score:</strong> {application.creditScore}
                  </Typography>
                )}
                <Typography>
                  <strong>Created:</strong> {formatDate(application.createdAt)}
                </Typography>
                <Typography>
                  <strong>Updated:</strong> {formatDate(application.updatedAt)}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Box>

        {/* Documents */}
        <Box sx={{ flex: "1 1 400px" }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <FileIcon sx={{ mr: 1, verticalAlign: "middle" }} />
                Documents ({application.documents?.length || 0})
              </Typography>
              {application.documents && application.documents.length > 0 ? (
                <List dense>
                  {application.documents.map((doc) => (
                    <ListItem
                      key={doc.id}
                      secondaryAction={
                        <IconButton
                          edge="end"
                          onClick={() =>
                            handleDownloadDocument(doc.id, doc.fileName)
                          }
                          title="Download"
                        >
                          <DownloadIcon />
                        </IconButton>
                      }
                    >
                      <ListItemIcon>
                        <FileIcon />
                      </ListItemIcon>
                      <ListItemText
                        primary={doc.fileName}
                        secondary={`${(doc.fileSize / 1024).toFixed(
                          1
                        )} KB • ${formatDate(doc.uploadedAt)}`}
                      />
                    </ListItem>
                  ))}
                </List>
              ) : (
                <Typography color="text.secondary">
                  No documents uploaded
                </Typography>
              )}
            </CardContent>
          </Card>
        </Box>

        {/* Decisions */}
        <Box sx={{ flex: "1 1 400px" }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Decision History ({application.decisions?.length || 0})
              </Typography>
              {application.decisions && application.decisions.length > 0 ? (
                <List dense>
                  {application.decisions.map((decision, index) => (
                    <React.Fragment key={decision.id}>
                      <ListItem>
                        <ListItemText
                          primary={
                            <Box
                              sx={{
                                display: "flex",
                                alignItems: "center",
                                gap: 1,
                              }}
                            >
                              <Chip
                                label={decision.outcome}
                                color={outcomeColors[decision.outcome]}
                                size="small"
                              />
                              <Typography variant="body2">
                                Score: {decision.score}
                              </Typography>
                              {decision.isManual && (
                                <Chip
                                  label="Manual"
                                  size="small"
                                  variant="outlined"
                                />
                              )}
                            </Box>
                          }
                          secondary={
                            <Box>
                              <Typography variant="body2">
                                {formatDate(decision.decidedAt)}
                                {decision.decidedByUser &&
                                  ` by ${decision.decidedByUser}`}
                              </Typography>
                              {decision.reasons &&
                                decision.reasons.length > 0 && (
                                  <Typography variant="body2" sx={{ mt: 1 }}>
                                    <strong>Reasons:</strong>{" "}
                                    {decision.reasons.join(", ")}
                                  </Typography>
                                )}
                            </Box>
                          }
                        />
                      </ListItem>
                      {index < application.decisions.length - 1 && <Divider />}
                    </React.Fragment>
                  ))}
                </List>
              ) : (
                <Typography color="text.secondary">
                  No decisions made
                </Typography>
              )}
            </CardContent>
          </Card>
        </Box>
      </Box>
    </Box>
  );
};
