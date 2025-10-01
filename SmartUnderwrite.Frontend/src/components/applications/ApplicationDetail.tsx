import React, { useState, useEffect } from "react";
import {
  Box,
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
  Avatar,
  LinearProgress,
  Fade,
  Grow,
} from "@mui/material";
import {
  ArrowBack as BackIcon,
  Person as PersonIcon,
  AttachFile as FileIcon,
  Download as DownloadIcon,
  PlayArrow as EvaluateIcon,
  AccountBalance as LoanIcon,
  Gavel as DecisionIcon,
  Email as EmailIcon,
  Phone as PhoneIcon,
  LocationOn as LocationIcon,
  CalendarToday as CalendarIcon,
  TrendingUp as ScoreIcon,
  Work as WorkIcon,
  AttachMoney as MoneyIcon,
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

  const canEvaluate = hasRole(UserRole.Admin) || hasRole(UserRole.Underwriter);
  const needsEvaluation = application.status === ApplicationStatus.Submitted;

  return (
    <Fade in timeout={600}>
      <Box sx={{ p: { xs: 2, md: 3 } }}>
        {/* Header Section */}
        <Box
          sx={{
            mb: 4,
            p: 3,
            background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
            borderRadius: 3,
            color: "white",
            position: "relative",
            overflow: "hidden",
            "&::before": {
              content: '""',
              position: "absolute",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              background: "rgba(255, 255, 255, 0.1)",
              backdropFilter: "blur(10px)",
            },
          }}
        >
          <Box sx={{ position: "relative", zIndex: 1 }}>
            <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 2 }}>
              <Button
                startIcon={<BackIcon />}
                onClick={() => navigate("/applications")}
                sx={{
                  color: "white",
                  borderColor: "rgba(255, 255, 255, 0.3)",
                  "&:hover": {
                    borderColor: "rgba(255, 255, 255, 0.5)",
                    backgroundColor: "rgba(255, 255, 255, 0.1)",
                  },
                }}
                variant="outlined"
              >
                Back to Applications
              </Button>
              {canEvaluate && needsEvaluation && (
                <Button
                  variant="contained"
                  startIcon={<EvaluateIcon />}
                  onClick={handleEvaluate}
                  disabled={evaluating}
                  sx={{
                    backgroundColor: "rgba(255, 255, 255, 0.2)",
                    color: "white",
                    "&:hover": {
                      backgroundColor: "rgba(255, 255, 255, 0.3)",
                    },
                    "&:disabled": {
                      backgroundColor: "rgba(255, 255, 255, 0.1)",
                      color: "rgba(255, 255, 255, 0.5)",
                    },
                  }}
                >
                  {evaluating ? "Evaluating..." : "Evaluate"}
                </Button>
              )}
            </Box>

            <Box
              sx={{
                display: "flex",
                alignItems: "center",
                gap: 2,
                flexWrap: "wrap",
              }}
            >
              <Avatar
                sx={{
                  width: 56,
                  height: 56,
                  backgroundColor: "rgba(255, 255, 255, 0.2)",
                  fontSize: "1.5rem",
                  fontWeight: "bold",
                }}
              >
                {application.applicant.firstName[0]}
                {application.applicant.lastName[0]}
              </Avatar>
              <Box sx={{ flex: 1 }}>
                <Typography
                  variant="h4"
                  component="h1"
                  sx={{ fontWeight: 700, mb: 1 }}
                >
                  Application #{application.id}
                </Typography>
                <Box
                  sx={{
                    display: "flex",
                    alignItems: "center",
                    gap: 2,
                    flexWrap: "wrap",
                  }}
                >
                  <Chip
                    label={application.status}
                    color={statusColors[application.status]}
                    sx={{
                      fontWeight: 600,
                      backgroundColor: "rgba(255, 255, 255, 0.9)",
                      color: "primary.main",
                    }}
                  />
                  <Typography variant="body1" sx={{ opacity: 0.9 }}>
                    {application.applicant.firstName}{" "}
                    {application.applicant.lastName}
                  </Typography>
                  <Typography variant="body2" sx={{ opacity: 0.8 }}>
                    {formatCurrency(application.amount)} •{" "}
                    {application.productType}
                  </Typography>
                </Box>
              </Box>
            </Box>
          </Box>

          {evaluating && (
            <Box sx={{ position: "absolute", bottom: 0, left: 0, right: 0 }}>
              <LinearProgress
                sx={{ backgroundColor: "rgba(255, 255, 255, 0.2)" }}
              />
            </Box>
          )}
        </Box>

        {error && (
          <Fade in>
            <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>
              {error}
            </Alert>
          </Fade>
        )}

        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "1fr",
              md: "repeat(2, 1fr)",
              lg: "repeat(3, 1fr)",
            },
            gap: 3,
            mb: 3,
          }}
        >
          {/* Applicant Information */}
          <Grow in timeout={800}>
            <Card
              sx={{
                height: "100%",
                borderRadius: 3,
                boxShadow: "0 4px 20px rgba(0, 0, 0, 0.08)",
                border: "1px solid rgba(0, 0, 0, 0.05)",
                transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
                "&:hover": {
                  transform: "translateY(-4px)",
                  boxShadow: "0 8px 30px rgba(0, 0, 0, 0.12)",
                },
              }}
            >
              <CardContent sx={{ p: 3 }}>
                <Box sx={{ display: "flex", alignItems: "center", mb: 3 }}>
                  <Avatar sx={{ backgroundColor: "primary.main", mr: 2 }}>
                    <PersonIcon />
                  </Avatar>
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    Applicant Information
                  </Typography>
                </Box>

                <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    <PersonIcon
                      sx={{ color: "text.secondary", fontSize: 20 }}
                    />
                    <Typography variant="body2" color="text.secondary">
                      Name:
                    </Typography>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {application.applicant.firstName}{" "}
                      {application.applicant.lastName}
                    </Typography>
                  </Box>

                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    <EmailIcon sx={{ color: "text.secondary", fontSize: 20 }} />
                    <Typography variant="body2" color="text.secondary">
                      Email:
                    </Typography>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {application.applicant.email}
                    </Typography>
                  </Box>

                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    <PhoneIcon sx={{ color: "text.secondary", fontSize: 20 }} />
                    <Typography variant="body2" color="text.secondary">
                      Phone:
                    </Typography>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {application.applicant.phone}
                    </Typography>
                  </Box>

                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    <CalendarIcon
                      sx={{ color: "text.secondary", fontSize: 20 }}
                    />
                    <Typography variant="body2" color="text.secondary">
                      Date of Birth:
                    </Typography>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {new Date(
                        application.applicant.dateOfBirth
                      ).toLocaleDateString()}
                    </Typography>
                  </Box>

                  <Divider sx={{ my: 1 }} />

                  <Box
                    sx={{ display: "flex", alignItems: "flex-start", gap: 1 }}
                  >
                    <LocationIcon
                      sx={{ color: "text.secondary", fontSize: 20, mt: 0.2 }}
                    />
                    <Box>
                      <Typography
                        variant="body2"
                        color="text.secondary"
                        sx={{ mb: 0.5 }}
                      >
                        Address:
                      </Typography>
                      <Typography
                        variant="body2"
                        sx={{ fontWeight: 500, lineHeight: 1.4 }}
                      >
                        {application.applicant.address.street}
                        <br />
                        {application.applicant.address.city},{" "}
                        {application.applicant.address.state}{" "}
                        {application.applicant.address.zipCode}
                      </Typography>
                    </Box>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Grow>

          {/* Loan Information */}
          <Grow in timeout={1000}>
            <Card
              sx={{
                height: "100%",
                borderRadius: 3,
                boxShadow: "0 4px 20px rgba(0, 0, 0, 0.08)",
                border: "1px solid rgba(0, 0, 0, 0.05)",
                transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
                "&:hover": {
                  transform: "translateY(-4px)",
                  boxShadow: "0 8px 30px rgba(0, 0, 0, 0.12)",
                },
              }}
            >
              <CardContent sx={{ p: 3 }}>
                <Box sx={{ display: "flex", alignItems: "center", mb: 3 }}>
                  <Avatar sx={{ backgroundColor: "success.main", mr: 2 }}>
                    <LoanIcon />
                  </Avatar>
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    Loan Information
                  </Typography>
                </Box>

                <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    <LoanIcon sx={{ color: "text.secondary", fontSize: 20 }} />
                    <Typography variant="body2" color="text.secondary">
                      Product Type:
                    </Typography>
                    <Chip
                      label={application.productType}
                      size="small"
                      color="primary"
                      variant="outlined"
                    />
                  </Box>

                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    <MoneyIcon sx={{ color: "text.secondary", fontSize: 20 }} />
                    <Typography variant="body2" color="text.secondary">
                      Amount:
                    </Typography>
                    <Typography
                      variant="body2"
                      sx={{ fontWeight: 600, color: "success.main" }}
                    >
                      {formatCurrency(application.amount)}
                    </Typography>
                  </Box>

                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    <MoneyIcon sx={{ color: "text.secondary", fontSize: 20 }} />
                    <Typography variant="body2" color="text.secondary">
                      Monthly Income:
                    </Typography>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {formatCurrency(application.incomeMonthly)}
                    </Typography>
                  </Box>

                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    <WorkIcon sx={{ color: "text.secondary", fontSize: 20 }} />
                    <Typography variant="body2" color="text.secondary">
                      Employment:
                    </Typography>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {application.employmentType}
                    </Typography>
                  </Box>

                  {application.creditScore && (
                    <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                      <ScoreIcon
                        sx={{ color: "text.secondary", fontSize: 20 }}
                      />
                      <Typography variant="body2" color="text.secondary">
                        Credit Score:
                      </Typography>
                      <Chip
                        label={application.creditScore}
                        size="small"
                        color={
                          application.creditScore >= 700
                            ? "success"
                            : application.creditScore >= 600
                            ? "warning"
                            : "error"
                        }
                      />
                    </Box>
                  )}

                  <Divider sx={{ my: 1 }} />

                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    <CalendarIcon
                      sx={{ color: "text.secondary", fontSize: 20 }}
                    />
                    <Typography variant="body2" color="text.secondary">
                      Created:
                    </Typography>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {formatDate(application.createdAt)}
                    </Typography>
                  </Box>

                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    <CalendarIcon
                      sx={{ color: "text.secondary", fontSize: 20 }}
                    />
                    <Typography variant="body2" color="text.secondary">
                      Updated:
                    </Typography>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {formatDate(application.updatedAt)}
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Grow>

          {/* Documents */}
          <Grow in timeout={1200}>
            <Card
              sx={{
                height: "100%",
                borderRadius: 3,
                boxShadow: "0 4px 20px rgba(0, 0, 0, 0.08)",
                border: "1px solid rgba(0, 0, 0, 0.05)",
                transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
                "&:hover": {
                  transform: "translateY(-4px)",
                  boxShadow: "0 8px 30px rgba(0, 0, 0, 0.12)",
                },
              }}
            >
              <CardContent sx={{ p: 3 }}>
                <Box sx={{ display: "flex", alignItems: "center", mb: 3 }}>
                  <Avatar sx={{ backgroundColor: "info.main", mr: 2 }}>
                    <FileIcon />
                  </Avatar>
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    Documents ({application.documents?.length || 0})
                  </Typography>
                </Box>

                {application.documents && application.documents.length > 0 ? (
                  <List
                    dense
                    sx={{ "& .MuiListItem-root": { borderRadius: 2, mb: 1 } }}
                  >
                    {application.documents.map((doc, index) => (
                      <Fade in timeout={300 + index * 100} key={doc.id}>
                        <ListItem
                          sx={{
                            backgroundColor: "rgba(0, 0, 0, 0.02)",
                            "&:hover": {
                              backgroundColor: "rgba(0, 0, 0, 0.04)",
                            },
                          }}
                          secondaryAction={
                            <IconButton
                              edge="end"
                              onClick={() =>
                                handleDownloadDocument(doc.id, doc.fileName)
                              }
                              title="Download"
                              sx={{
                                color: "primary.main",
                                "&:hover": {
                                  backgroundColor: "primary.light",
                                  color: "white",
                                },
                              }}
                            >
                              <DownloadIcon />
                            </IconButton>
                          }
                        >
                          <ListItemIcon>
                            <FileIcon color="primary" />
                          </ListItemIcon>
                          <ListItemText
                            primary={
                              <Typography
                                variant="body2"
                                sx={{ fontWeight: 500 }}
                              >
                                {doc.fileName}
                              </Typography>
                            }
                            secondary={
                              <Typography
                                variant="caption"
                                color="text.secondary"
                              >
                                {(doc.fileSize / 1024).toFixed(1)} KB •{" "}
                                {formatDate(doc.uploadedAt)}
                              </Typography>
                            }
                          />
                        </ListItem>
                      </Fade>
                    ))}
                  </List>
                ) : (
                  <Box
                    sx={{
                      textAlign: "center",
                      py: 4,
                      color: "text.secondary",
                      backgroundColor: "rgba(0, 0, 0, 0.02)",
                      borderRadius: 2,
                      border: "2px dashed rgba(0, 0, 0, 0.1)",
                    }}
                  >
                    <FileIcon sx={{ fontSize: 48, mb: 1, opacity: 0.5 }} />
                    <Typography variant="body2">
                      No documents uploaded
                    </Typography>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Grow>
        </Box>

        {/* Decisions - Full Width */}
        <Box>
          <Grow in timeout={1400}>
            <Card
              sx={{
                height: "100%",
                borderRadius: 3,
                boxShadow: "0 4px 20px rgba(0, 0, 0, 0.08)",
                border: "1px solid rgba(0, 0, 0, 0.05)",
                transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
                "&:hover": {
                  transform: "translateY(-4px)",
                  boxShadow: "0 8px 30px rgba(0, 0, 0, 0.12)",
                },
              }}
            >
              <CardContent sx={{ p: 3 }}>
                <Box sx={{ display: "flex", alignItems: "center", mb: 3 }}>
                  <Avatar sx={{ backgroundColor: "warning.main", mr: 2 }}>
                    <DecisionIcon />
                  </Avatar>
                  <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    Decision History ({application.decisions?.length || 0})
                  </Typography>
                </Box>

                {application.decisions && application.decisions.length > 0 ? (
                  <List
                    dense
                    sx={{ "& .MuiListItem-root": { borderRadius: 2, mb: 2 } }}
                  >
                    {application.decisions.map((decision, index) => (
                      <Fade in timeout={300 + index * 150} key={decision.id}>
                        <Box>
                          <ListItem
                            sx={{
                              backgroundColor: "rgba(0, 0, 0, 0.02)",
                              border: "1px solid rgba(0, 0, 0, 0.05)",
                              "&:hover": {
                                backgroundColor: "rgba(0, 0, 0, 0.04)",
                              },
                            }}
                          >
                            <ListItemText
                              primary={
                                <Box
                                  sx={{
                                    display: "flex",
                                    alignItems: "center",
                                    gap: 1,
                                    flexWrap: "wrap",
                                    mb: 1,
                                  }}
                                >
                                  <Chip
                                    label={decision.outcome}
                                    color={outcomeColors[decision.outcome]}
                                    size="small"
                                    sx={{ fontWeight: 600 }}
                                  />
                                  <Chip
                                    label={`Score: ${decision.score}`}
                                    size="small"
                                    variant="outlined"
                                    color="primary"
                                  />
                                  {decision.isManual && (
                                    <Chip
                                      label="Manual Decision"
                                      size="small"
                                      variant="outlined"
                                      color="secondary"
                                    />
                                  )}
                                </Box>
                              }
                              secondary={
                                <Box>
                                  <Typography variant="body2" sx={{ mb: 1 }}>
                                    <CalendarIcon
                                      sx={{
                                        fontSize: 16,
                                        mr: 0.5,
                                        verticalAlign: "middle",
                                      }}
                                    />
                                    {formatDate(decision.decidedAt)}
                                    {decision.decidedByUser && (
                                      <span style={{ marginLeft: 8 }}>
                                        <PersonIcon
                                          sx={{
                                            fontSize: 16,
                                            mr: 0.5,
                                            verticalAlign: "middle",
                                          }}
                                        />
                                        by {decision.decidedByUser}
                                      </span>
                                    )}
                                  </Typography>
                                  {decision.reasons &&
                                    decision.reasons.length > 0 && (
                                      <Box
                                        sx={{
                                          mt: 1,
                                          p: 2,
                                          backgroundColor:
                                            "rgba(0, 0, 0, 0.02)",
                                          borderRadius: 1,
                                          border:
                                            "1px solid rgba(0, 0, 0, 0.05)",
                                        }}
                                      >
                                        <Typography
                                          variant="body2"
                                          sx={{ fontWeight: 500, mb: 1 }}
                                        >
                                          Decision Reasons:
                                        </Typography>
                                        <Box
                                          sx={{
                                            display: "flex",
                                            flexWrap: "wrap",
                                            gap: 0.5,
                                          }}
                                        >
                                          {decision.reasons.map(
                                            (reason, reasonIndex) => (
                                              <Chip
                                                key={reasonIndex}
                                                label={reason}
                                                size="small"
                                                variant="outlined"
                                                color="default"
                                              />
                                            )
                                          )}
                                        </Box>
                                      </Box>
                                    )}
                                </Box>
                              }
                            />
                          </ListItem>
                          {index < application.decisions.length - 1 && (
                            <Divider sx={{ my: 1, mx: 2 }} />
                          )}
                        </Box>
                      </Fade>
                    ))}
                  </List>
                ) : (
                  <Box
                    sx={{
                      textAlign: "center",
                      py: 4,
                      color: "text.secondary",
                      backgroundColor: "rgba(0, 0, 0, 0.02)",
                      borderRadius: 2,
                      border: "2px dashed rgba(0, 0, 0, 0.1)",
                    }}
                  >
                    <DecisionIcon sx={{ fontSize: 48, mb: 1, opacity: 0.5 }} />
                    <Typography variant="body2">
                      No decisions made yet
                    </Typography>
                    {needsEvaluation && (
                      <Typography variant="caption" color="text.secondary">
                        This application is ready for evaluation
                      </Typography>
                    )}
                  </Box>
                )}
              </CardContent>
            </Card>
          </Grow>
        </Box>
      </Box>
    </Fade>
  );
};
