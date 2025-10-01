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
  Avatar,
  Tooltip,
  Fade,
  useTheme,
  alpha,
} from "@mui/material";
import {
  Visibility as ViewIcon,
  Add as AddIcon,
  Search as SearchIcon,
  Person as PersonIcon,
  AttachMoney as MoneyIcon,
  Business as BusinessIcon,
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
  const theme = useTheme();
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
            <TableRow sx={{ bgcolor: alpha(theme.palette.primary.main, 0.05) }}>
              <TableCell sx={{ fontWeight: 600 }}>ID</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>Applicant</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>Product</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>Amount</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>Status</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>Created</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {applications.items.map((application, index) => (
              <Fade in timeout={300 + index * 100} key={application.id}>
                <TableRow
                  hover
                  sx={{
                    cursor: "pointer",
                    transition: "all 0.2s ease",
                    "&:hover": {
                      bgcolor: alpha(theme.palette.primary.main, 0.02),
                      transform: "scale(1.001)",
                    },
                  }}
                  onClick={() => handleViewApplication(application.id)}
                >
                  <TableCell>
                    <Typography
                      variant="body2"
                      sx={{
                        fontWeight: 600,
                        color: theme.palette.primary.main,
                      }}
                    >
                      #{application.id}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
                      <Avatar
                        sx={{
                          width: 32,
                          height: 32,
                          bgcolor: theme.palette.secondary.main,
                          fontSize: "0.875rem",
                        }}
                      >
                        {application.applicant.firstName.charAt(0)}
                        {application.applicant.lastName.charAt(0)}
                      </Avatar>
                      <Box>
                        <Typography variant="body2" sx={{ fontWeight: 500 }}>
                          {application.applicant.firstName}{" "}
                          {application.applicant.lastName}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {application.applicant.email}
                        </Typography>
                      </Box>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                      <BusinessIcon fontSize="small" color="action" />
                      <Typography variant="body2">
                        {application.productType}
                      </Typography>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                      <MoneyIcon fontSize="small" color="success" />
                      <Typography
                        variant="body2"
                        sx={{
                          fontWeight: 600,
                          color: theme.palette.success.main,
                        }}
                      >
                        {formatCurrency(application.amount)}
                      </Typography>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={application.status}
                      color={statusColors[application.status]}
                      size="small"
                      sx={{ fontWeight: 500 }}
                    />
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {formatDate(application.createdAt)}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Tooltip title="View Details">
                      <IconButton
                        size="small"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleViewApplication(application.id);
                        }}
                        sx={{
                          transition: "all 0.2s ease",
                          "&:hover": {
                            bgcolor: alpha(theme.palette.primary.main, 0.1),
                            transform: "scale(1.1)",
                          },
                        }}
                      >
                        <ViewIcon />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              </Fade>
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
