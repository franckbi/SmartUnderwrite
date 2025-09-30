import React, { useState } from "react";
import {
  Box,
  Paper,
  Typography,
  Button,
  Card,
  CardContent,
  Chip,
  TextField,
  FormControl,
  FormGroup,
  FormControlLabel,
  Checkbox,
  Alert,
  Divider,
} from "@mui/material";
import {
  ThumbUp as ApproveIcon,
  ThumbDown as RejectIcon,
  HourglassEmpty as ReviewIcon,
  Save as SaveIcon,
} from "@mui/icons-material";
import { LoanApplication, ApplicationStatus } from "@/types/application";
import { DecisionRequest, DecisionOutcome } from "@/types/decision";
import { decisionService } from "@/services/decisionService";
import { useErrorHandler } from "@/hooks/useErrorHandler";

interface ManualDecisionInterfaceProps {
  application: LoanApplication;
  onDecisionMade?: (application: LoanApplication) => void;
}

const commonReasons = {
  [DecisionOutcome.Approve]: [
    "Strong credit profile",
    "Sufficient income",
    "Low debt-to-income ratio",
    "Stable employment history",
    "Adequate collateral",
    "Good payment history",
  ],
  [DecisionOutcome.Reject]: [
    "Insufficient income",
    "Poor credit history",
    "High debt-to-income ratio",
    "Unstable employment",
    "Inadequate collateral",
    "Recent delinquencies",
    "Incomplete documentation",
  ],
  [DecisionOutcome.ManualReview]: [
    "Requires additional documentation",
    "Borderline credit score",
    "Complex financial situation",
    "Unusual employment circumstances",
    "Requires senior review",
  ],
};

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

export const ManualDecisionInterface: React.FC<
  ManualDecisionInterfaceProps
> = ({ application, onDecisionMade }) => {
  const { error, handleError, clearError } = useErrorHandler();
  const [submitting, setSubmitting] = useState(false);
  const [decision, setDecision] = useState<{
    outcome: DecisionOutcome | "";
    reasons: string[];
    notes: string;
  }>({
    outcome: "",
    reasons: [],
    notes: "",
  });

  const handleOutcomeChange = (outcome: DecisionOutcome) => {
    setDecision((prev) => ({
      ...prev,
      outcome,
      reasons: [], // Clear reasons when outcome changes
    }));
  };

  const handleReasonToggle = (reason: string) => {
    setDecision((prev) => ({
      ...prev,
      reasons: prev.reasons.includes(reason)
        ? prev.reasons.filter((r) => r !== reason)
        : [...prev.reasons, reason],
    }));
  };

  const handleNotesChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setDecision((prev) => ({ ...prev, notes: event.target.value }));
  };

  const handleSubmit = async () => {
    if (!decision.outcome) {
      handleError({ message: "Please select a decision outcome" } as any);
      return;
    }

    if (decision.reasons.length === 0) {
      handleError({ message: "Please select at least one reason" } as any);
      return;
    }

    try {
      setSubmitting(true);
      clearError();

      const request: DecisionRequest = {
        applicationId: application.id,
        outcome: decision.outcome,
        reasons: decision.reasons,
        notes: decision.notes || undefined,
      };

      const updatedApplication = await decisionService.makeDecision(request);

      if (onDecisionMade) {
        onDecisionMade(updatedApplication);
      }
    } catch (err) {
      handleError(err);
    } finally {
      setSubmitting(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: "USD",
    }).format(amount);
  };

  const latestDecision = application.decisions?.[0];
  const currentScore = latestDecision?.score || 0;

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Manual Decision - Application #{application.id}
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Application Summary */}
      <Card sx={{ mb: 3 }}>
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
              {application.applicant.firstName} {application.applicant.lastName}
            </Typography>
            <Chip
              label={application.status}
              color={statusColors[application.status]}
            />
          </Box>

          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <Box>
              <Typography variant="body2" color="text.secondary">
                Product
              </Typography>
              <Typography variant="body1">{application.productType}</Typography>
            </Box>
            <Box>
              <Typography variant="body2" color="text.secondary">
                Amount
              </Typography>
              <Typography variant="body1">
                {formatCurrency(application.amount)}
              </Typography>
            </Box>
            <Box>
              <Typography variant="body2" color="text.secondary">
                Monthly Income
              </Typography>
              <Typography variant="body1">
                {formatCurrency(application.incomeMonthly)}
              </Typography>
            </Box>
            <Box>
              <Typography variant="body2" color="text.secondary">
                Employment
              </Typography>
              <Typography variant="body1">
                {application.employmentType}
              </Typography>
            </Box>
            {application.creditScore && (
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Credit Score
                </Typography>
                <Typography variant="body1">
                  {application.creditScore}
                </Typography>
              </Box>
            )}
            {currentScore > 0 && (
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Risk Score
                </Typography>
                <Typography variant="body1">{currentScore}</Typography>
              </Box>
            )}
          </Box>
        </CardContent>
      </Card>

      {/* Decision Interface */}
      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" gutterBottom>
          Make Decision
        </Typography>

        {/* Outcome Selection */}
        <Box sx={{ mb: 3 }}>
          <Typography variant="subtitle1" gutterBottom>
            Decision Outcome
          </Typography>
          <Box sx={{ display: "flex", gap: 2, flexWrap: "wrap" }}>
            <Button
              variant={
                decision.outcome === DecisionOutcome.Approve
                  ? "contained"
                  : "outlined"
              }
              color="success"
              startIcon={<ApproveIcon />}
              onClick={() => handleOutcomeChange(DecisionOutcome.Approve)}
            >
              Approve
            </Button>
            <Button
              variant={
                decision.outcome === DecisionOutcome.Reject
                  ? "contained"
                  : "outlined"
              }
              color="error"
              startIcon={<RejectIcon />}
              onClick={() => handleOutcomeChange(DecisionOutcome.Reject)}
            >
              Reject
            </Button>
            <Button
              variant={
                decision.outcome === DecisionOutcome.ManualReview
                  ? "contained"
                  : "outlined"
              }
              color="warning"
              startIcon={<ReviewIcon />}
              onClick={() => handleOutcomeChange(DecisionOutcome.ManualReview)}
            >
              Needs Review
            </Button>
          </Box>
        </Box>

        {/* Reasons Selection */}
        {decision.outcome && (
          <Box sx={{ mb: 3 }}>
            <Typography variant="subtitle1" gutterBottom>
              Reasons for {decision.outcome}
            </Typography>
            <FormControl component="fieldset">
              <FormGroup>
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                  {commonReasons[decision.outcome].map((reason) => (
                    <FormControlLabel
                      key={reason}
                      control={
                        <Checkbox
                          checked={decision.reasons.includes(reason)}
                          onChange={() => handleReasonToggle(reason)}
                        />
                      }
                      label={reason}
                      sx={{ minWidth: "250px" }}
                    />
                  ))}
                </Box>
              </FormGroup>
            </FormControl>
          </Box>
        )}

        {/* Notes */}
        <Box sx={{ mb: 3 }}>
          <TextField
            fullWidth
            multiline
            rows={3}
            label="Additional Notes (Optional)"
            value={decision.notes}
            onChange={handleNotesChange}
            placeholder="Add any additional comments or explanations..."
          />
        </Box>

        <Divider sx={{ mb: 3 }} />

        {/* Submit Button */}
        <Box sx={{ display: "flex", justifyContent: "flex-end" }}>
          <Button
            variant="contained"
            size="large"
            startIcon={<SaveIcon />}
            onClick={handleSubmit}
            disabled={
              submitting || !decision.outcome || decision.reasons.length === 0
            }
          >
            {submitting ? "Submitting Decision..." : "Submit Decision"}
          </Button>
        </Box>
      </Paper>
    </Box>
  );
};
