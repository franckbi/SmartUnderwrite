import React, { useState, useEffect } from "react";
import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Chip,
  IconButton,
  TextField,
  MenuItem,
  Button,
  Typography,
  Toolbar,
  Card,
  CardContent,
} from "@mui/material";
import {
  Visibility as ViewIcon,
  Search as SearchIcon,
  Gavel as DecisionIcon,
} from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import {
  DecisionResponse,
  DecisionOutcome,
  DecisionFilter,
  DecisionSummary,
} from "@/types/decision";
import { PagedResult } from "@/types/api";
import { decisionService } from "@/services/decisionService";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { useErrorHandler } from "@/hooks/useErrorHandler";

const outcomeColors: Record<
  DecisionOutcome,
  "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning"
> = {
  [DecisionOutcome.Approve]: "success",
  [DecisionOutcome.Reject]: "error",
  [DecisionOutcome.ManualReview]: "warning",
};

export const DecisionList: React.FC = () => {
  const navigate = useNavigate();
  const { error, handleError, clearError } = useErrorHandler();

  const [decisions, setDecisions] = useState<PagedResult<DecisionResponse>>({
    items: [],
    totalCount: 0,
    pageNumber: 1,
    pageSize: 10,
    totalPages: 0,
  });
  const [summary, setSummary] = useState<DecisionSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<DecisionFilter>({
    pageNumber: 1,
    pageSize: 10,
  });

  const loadDecisions = async () => {
    try {
      setLoading(true);
      clearError();
      const [decisionsResult, summaryResult] = await Promise.all([
        decisionService.getDecisions(filter),
        decisionService.getDecisionSummary(),
      ]);
      setDecisions(decisionsResult);
      setSummary(summaryResult);
    } catch (err) {
      handleError(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadDecisions();
  }, [filter]);

  const handlePageChange = (event: unknown, newPage: number) => {
    setFilter((prev) => ({ ...prev, pageNumber: newPage + 1 }));
  };

  const handleRowsPerPageChange = (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    setFilter((prev) => ({
      ...prev,
      pageSize: parseInt(event.target.value, 10),
      pageNumber: 1,
    }));
  };

  const handleOutcomeFilterChange = (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const outcome = event.target.value as DecisionOutcome | "";
    setFilter((prev) => ({
      ...prev,
      outcome: outcome || undefined,
      pageNumber: 1,
    }));
  };

  const handleManualFilterChange = (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const value = event.target.value;
    setFilter((prev) => ({
      ...prev,
      isManual: value === "" ? undefined : value === "true",
      pageNumber: 1,
    }));
  };

  const handleViewApplication = (applicationId: number) => {
    navigate(`/applications/${applicationId}`);
  };

  const handleMakeDecisions = () => {
    navigate("/decisions/pending");
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  if (loading && decisions.items.length === 0) {
    return <LoadingSpinner message="Loading decisions..." />;
  }

  return (
    <Box>
      <Toolbar sx={{ pl: 0, pr: 0 }}>
        <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
          Decision History
        </Typography>
        <Button
          variant="contained"
          startIcon={<DecisionIcon />}
          onClick={handleMakeDecisions}
        >
          Review Pending
        </Button>
      </Toolbar>

      {/* Summary Cards */}
      {summary && (
        <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, mb: 3 }}>
          <Card sx={{ minWidth: 200, flex: "1 1 200px" }}>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Total Decisions
              </Typography>
              <Typography variant="h4">
                {summary.totalDecisions.toLocaleString()}
              </Typography>
            </CardContent>
          </Card>
          <Card sx={{ minWidth: 200, flex: "1 1 200px" }}>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Approval Rate
              </Typography>
              <Typography variant="h4" color="success.main">
                {summary.totalDecisions > 0
                  ? (
                      (summary.approvedCount / summary.totalDecisions) *
                      100
                    ).toFixed(1)
                  : 0}
                %
              </Typography>
            </CardContent>
          </Card>
          <Card sx={{ minWidth: 200, flex: "1 1 200px" }}>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Average Score
              </Typography>
              <Typography variant="h4">
                {summary.averageScore.toFixed(1)}
              </Typography>
            </CardContent>
          </Card>
          <Card sx={{ minWidth: 200, flex: "1 1 200px" }}>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Manual Decisions
              </Typography>
              <Typography variant="h4">
                {summary.manualDecisionCount.toLocaleString()}
              </Typography>
            </CardContent>
          </Card>
        </Box>
      )}

      <Paper sx={{ mb: 2, p: 2 }}>
        <Box
          sx={{
            display: "flex",
            flexWrap: "wrap",
            gap: 2,
            alignItems: "center",
          }}
        >
          <TextField
            select
            label="Outcome"
            value={filter.outcome || ""}
            onChange={handleOutcomeFilterChange}
            sx={{ minWidth: 150 }}
            size="small"
          >
            <MenuItem value="">All Outcomes</MenuItem>
            {Object.values(DecisionOutcome).map((outcome) => (
              <MenuItem key={outcome} value={outcome}>
                {outcome}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            label="Decision Type"
            value={
              filter.isManual === undefined ? "" : filter.isManual.toString()
            }
            onChange={handleManualFilterChange}
            sx={{ minWidth: 150 }}
            size="small"
          >
            <MenuItem value="">All Types</MenuItem>
            <MenuItem value="true">Manual</MenuItem>
            <MenuItem value="false">Automated</MenuItem>
          </TextField>
          <Button
            variant="outlined"
            startIcon={<SearchIcon />}
            onClick={loadDecisions}
            disabled={loading}
          >
            Refresh
          </Button>
        </Box>
      </Paper>

      {error && (
        <Paper
          sx={{
            p: 2,
            mb: 2,
            bgcolor: "error.light",
            color: "error.contrastText",
          }}
        >
          <Typography>{error}</Typography>
        </Paper>
      )}

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Decision ID</TableCell>
              <TableCell>Application ID</TableCell>
              <TableCell>Outcome</TableCell>
              <TableCell>Score</TableCell>
              <TableCell>Type</TableCell>
              <TableCell>Decided By</TableCell>
              <TableCell>Date</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {decisions.items.map((decision) => (
              <TableRow key={decision.id} hover>
                <TableCell>{decision.id}</TableCell>
                <TableCell>{decision.applicationId}</TableCell>
                <TableCell>
                  <Chip
                    label={decision.outcome}
                    color={outcomeColors[decision.outcome]}
                    size="small"
                  />
                </TableCell>
                <TableCell>{decision.score}</TableCell>
                <TableCell>
                  <Chip
                    label={decision.isManual ? "Manual" : "Automated"}
                    variant={decision.isManual ? "filled" : "outlined"}
                    size="small"
                  />
                </TableCell>
                <TableCell>{decision.decidedByUser}</TableCell>
                <TableCell>{formatDate(decision.decidedAt)}</TableCell>
                <TableCell>
                  <IconButton
                    size="small"
                    onClick={() =>
                      handleViewApplication(decision.applicationId)
                    }
                    title="View Application"
                  >
                    <ViewIcon />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        {decisions.items.length === 0 && !loading && (
          <Box sx={{ p: 4, textAlign: "center" }}>
            <Typography color="text.secondary">No decisions found</Typography>
          </Box>
        )}
        <TablePagination
          rowsPerPageOptions={[5, 10, 25, 50]}
          component="div"
          count={decisions.totalCount}
          rowsPerPage={decisions.pageSize}
          page={decisions.pageNumber - 1}
          onPageChange={handlePageChange}
          onRowsPerPageChange={handleRowsPerPageChange}
        />
      </TableContainer>
    </Box>
  );
};
