import React, { useState, useEffect } from "react";
import {
  Box,
  Paper,
  Typography,
  Button,
  Card,
  CardContent,
  CardActions,
  Chip,
  Alert,
  Pagination,
} from "@mui/material";
import {
  ArrowBack as BackIcon,
  Gavel as DecisionIcon,
} from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import { LoanApplication } from "@/types/application";
import { PagedResult } from "@/types/api";
import { decisionService } from "@/services/decisionService";
import { ManualDecisionInterface } from "./ManualDecisionInterface";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { useErrorHandler } from "@/hooks/useErrorHandler";

export const PendingApplications: React.FC = () => {
  const navigate = useNavigate();
  const { error, handleError, clearError } = useErrorHandler();

  const [applications, setApplications] = useState<
    PagedResult<LoanApplication>
  >({
    items: [],
    totalCount: 0,
    pageNumber: 1,
    pageSize: 10,
    totalPages: 0,
  });
  const [loading, setLoading] = useState(true);
  const [selectedApplication, setSelectedApplication] =
    useState<LoanApplication | null>(null);
  const [currentPage, setCurrentPage] = useState(1);

  const loadPendingApplications = async () => {
    try {
      setLoading(true);
      clearError();
      const result = await decisionService.getPendingApplications();
      setApplications(result);
    } catch (err) {
      handleError(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadPendingApplications();
  }, []);

  const handleSelectApplication = (application: LoanApplication) => {
    setSelectedApplication(application);
  };

  const handleDecisionMade = (updatedApplication: LoanApplication) => {
    // Remove the application from the pending list
    setApplications((prev) => ({
      ...prev,
      items: prev.items.filter((app) => app.id !== updatedApplication.id),
      totalCount: prev.totalCount - 1,
    }));

    // Clear selection
    setSelectedApplication(null);

    // Show success message
    clearError();
  };

  const handleBackToList = () => {
    setSelectedApplication(null);
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: "USD",
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  if (loading) {
    return <LoadingSpinner message="Loading pending applications..." />;
  }

  if (selectedApplication) {
    return (
      <Box>
        <Box sx={{ mb: 3, display: "flex", alignItems: "center", gap: 2 }}>
          <Button startIcon={<BackIcon />} onClick={handleBackToList}>
            Back to Pending List
          </Button>
        </Box>
        <ManualDecisionInterface
          application={selectedApplication}
          onDecisionMade={handleDecisionMade}
        />
      </Box>
    );
  }

  return (
    <Box>
      <Box sx={{ mb: 3, display: "flex", alignItems: "center", gap: 2 }}>
        <Button startIcon={<BackIcon />} onClick={() => navigate("/decisions")}>
          Back to Decisions
        </Button>
        <Typography variant="h4" component="h1">
          Pending Manual Review
        </Typography>
        <Chip
          label={`${applications.totalCount} applications`}
          color="warning"
        />
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {applications.totalCount === 0 ? (
        <Paper sx={{ p: 4, textAlign: "center" }}>
          <DecisionIcon sx={{ fontSize: 64, color: "grey.400", mb: 2 }} />
          <Typography variant="h6" gutterBottom>
            No Applications Pending Review
          </Typography>
          <Typography color="text.secondary">
            All applications have been processed. Great job!
          </Typography>
        </Paper>
      ) : (
        <>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            {applications.items.map((application) => {
              const latestDecision = application.decisions?.[0];

              return (
                <Card
                  key={application.id}
                  sx={{ minWidth: 350, flex: "1 1 350px" }}
                >
                  <CardContent>
                    <Box
                      sx={{
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                        mb: 2,
                      }}
                    >
                      <Typography variant="h6">
                        Application #{application.id}
                      </Typography>
                      <Chip
                        label={application.status}
                        color="warning"
                        size="small"
                      />
                    </Box>

                    <Typography variant="subtitle1" gutterBottom>
                      {application.applicant.firstName}{" "}
                      {application.applicant.lastName}
                    </Typography>

                    <Box sx={{ mb: 2 }}>
                      <Typography variant="body2" color="text.secondary">
                        Product: {application.productType}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Amount: {formatCurrency(application.amount)}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Income: {formatCurrency(application.incomeMonthly)}
                        /month
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Submitted: {formatDate(application.createdAt)}
                      </Typography>
                      {latestDecision && (
                        <Typography variant="body2" color="text.secondary">
                          Risk Score: {latestDecision.score}
                        </Typography>
                      )}
                    </Box>

                    {latestDecision && latestDecision.reasons.length > 0 && (
                      <Box sx={{ mb: 2 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          gutterBottom
                        >
                          Evaluation Notes:
                        </Typography>
                        <Box
                          sx={{ display: "flex", flexWrap: "wrap", gap: 0.5 }}
                        >
                          {latestDecision.reasons
                            .slice(0, 3)
                            .map((reason, index) => (
                              <Chip
                                key={index}
                                label={reason}
                                size="small"
                                variant="outlined"
                              />
                            ))}
                          {latestDecision.reasons.length > 3 && (
                            <Chip
                              label={`+${
                                latestDecision.reasons.length - 3
                              } more`}
                              size="small"
                              variant="outlined"
                            />
                          )}
                        </Box>
                      </Box>
                    )}
                  </CardContent>

                  <CardActions>
                    <Button
                      size="small"
                      onClick={() =>
                        navigate(`/applications/${application.id}`)
                      }
                    >
                      View Details
                    </Button>
                    <Button
                      size="small"
                      variant="contained"
                      startIcon={<DecisionIcon />}
                      onClick={() => handleSelectApplication(application)}
                    >
                      Make Decision
                    </Button>
                  </CardActions>
                </Card>
              );
            })}
          </Box>

          {applications.totalPages > 1 && (
            <Box sx={{ display: "flex", justifyContent: "center", mt: 4 }}>
              <Pagination
                count={applications.totalPages}
                page={currentPage}
                onChange={(event, page) => setCurrentPage(page)}
                color="primary"
              />
            </Box>
          )}
        </>
      )}
    </Box>
  );
};
