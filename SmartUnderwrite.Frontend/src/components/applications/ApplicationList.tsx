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
} from "@mui/material";
import {
  Visibility as ViewIcon,
  Add as AddIcon,
  Search as SearchIcon,
} from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import {
  LoanApplication,
  ApplicationStatus,
  ApplicationFilter,
} from "@/types/application";
import { PagedResult } from "@/types/api";
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

export const ApplicationList: React.FC = () => {
  const navigate = useNavigate();
  const { hasRole } = useAuth();
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
  const [filter, setFilter] = useState<ApplicationFilter>({
    pageNumber: 1,
    pageSize: 10,
  });

  const loadApplications = async () => {
    try {
      setLoading(true);
      clearError();
      const result = await applicationService.getApplications(filter);
      setApplications(result);
    } catch (err) {
      handleError(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadApplications();
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

  const handleStatusFilterChange = (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const status = event.target.value as ApplicationStatus | "";
    setFilter((prev) => ({
      ...prev,
      status: status || undefined,
      pageNumber: 1,
    }));
  };

  const handleViewApplication = (id: number) => {
    navigate(`/applications/${id}`);
  };

  const handleCreateApplication = () => {
    navigate("/applications/create");
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

  if (loading && applications.items.length === 0) {
    return <LoadingSpinner message="Loading applications..." />;
  }

  return (
    <Box>
      <Toolbar sx={{ pl: 0, pr: 0 }}>
        <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
          Loan Applications
        </Typography>
        {hasRole(UserRole.Affiliate) && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={handleCreateApplication}
          >
            New Application
          </Button>
        )}
      </Toolbar>

      <Paper sx={{ mb: 2, p: 2 }}>
        <Box sx={{ display: "flex", gap: 2, alignItems: "center" }}>
          <TextField
            select
            label="Status"
            value={filter.status || ""}
            onChange={handleStatusFilterChange}
            sx={{ minWidth: 150 }}
            size="small"
          >
            <MenuItem value="">All Statuses</MenuItem>
            {Object.values(ApplicationStatus).map((status) => (
              <MenuItem key={status} value={status}>
                {status}
              </MenuItem>
            ))}
          </TextField>
          <Button
            variant="outlined"
            startIcon={<SearchIcon />}
            onClick={loadApplications}
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
              <TableCell>ID</TableCell>
              <TableCell>Applicant</TableCell>
              <TableCell>Product</TableCell>
              <TableCell>Amount</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Created</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {applications.items.map((application) => (
              <TableRow key={application.id} hover>
                <TableCell>{application.id}</TableCell>
                <TableCell>
                  {application.applicant.firstName}{" "}
                  {application.applicant.lastName}
                </TableCell>
                <TableCell>{application.productType}</TableCell>
                <TableCell>{formatCurrency(application.amount)}</TableCell>
                <TableCell>
                  <Chip
                    label={application.status}
                    color={statusColors[application.status]}
                    size="small"
                  />
                </TableCell>
                <TableCell>{formatDate(application.createdAt)}</TableCell>
                <TableCell>
                  <IconButton
                    size="small"
                    onClick={() => handleViewApplication(application.id)}
                    title="View Details"
                  >
                    <ViewIcon />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        {applications.items.length === 0 && !loading && (
          <Box sx={{ p: 4, textAlign: "center" }}>
            <Typography color="text.secondary">
              No applications found
            </Typography>
          </Box>
        )}
        <TablePagination
          rowsPerPageOptions={[5, 10, 25, 50]}
          component="div"
          count={applications.totalCount}
          rowsPerPage={applications.pageSize}
          page={applications.pageNumber - 1}
          onPageChange={handlePageChange}
          onRowsPerPageChange={handleRowsPerPageChange}
        />
      </TableContainer>
    </Box>
  );
};
