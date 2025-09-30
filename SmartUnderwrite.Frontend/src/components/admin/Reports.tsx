import React, { useState, useEffect } from "react";
import {
  Box,
  Paper,
  Typography,
  Card,
  CardContent,
  TextField,
  Button,
  Alert,
  Toolbar,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  LinearProgress,
} from "@mui/material";
import {
  Refresh as RefreshIcon,
  Download as DownloadIcon,
  TrendingUp as TrendingUpIcon,
} from "@mui/icons-material";
import { ReportData } from "@/types/admin";
import { adminService } from "@/services/adminService";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { useErrorHandler } from "@/hooks/useErrorHandler";

export const Reports: React.FC = () => {
  const { error, handleError, clearError } = useErrorHandler();
  const [reportData, setReportData] = useState<ReportData | null>(null);
  const [loading, setLoading] = useState(true);
  const [dateRange, setDateRange] = useState({
    fromDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000)
      .toISOString()
      .split("T")[0], // 30 days ago
    toDate: new Date().toISOString().split("T")[0], // today
  });

  const loadReportData = async () => {
    try {
      setLoading(true);
      clearError();
      const result = await adminService.getReportData(
        dateRange.fromDate,
        dateRange.toDate
      );
      setReportData(result);
    } catch (err) {
      handleError(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadReportData();
  }, []);

  const handleDateChange =
    (field: "fromDate" | "toDate") =>
    (event: React.ChangeEvent<HTMLInputElement>) => {
      setDateRange((prev) => ({
        ...prev,
        [field]: event.target.value,
      }));
    };

  const handleRefresh = () => {
    loadReportData();
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: "USD",
    }).format(amount);
  };

  const formatPercentage = (value: number) => {
    return `${(value * 100).toFixed(1)}%`;
  };

  const formatDuration = (hours: number) => {
    if (hours < 24) {
      return `${hours.toFixed(1)} hours`;
    }
    return `${(hours / 24).toFixed(1)} days`;
  };

  if (loading && !reportData) {
    return <LoadingSpinner message="Loading reports..." />;
  }

  return (
    <Box>
      <Toolbar sx={{ pl: 0, pr: 0 }}>
        <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
          Reports & Analytics
        </Typography>
        <Button
          variant="outlined"
          startIcon={<RefreshIcon />}
          onClick={handleRefresh}
          disabled={loading}
          sx={{ mr: 1 }}
        >
          Refresh
        </Button>
        <Button
          variant="contained"
          startIcon={<DownloadIcon />}
          disabled={!reportData}
        >
          Export
        </Button>
      </Toolbar>

      {/* Date Range Filter */}
      <Paper sx={{ mb: 3, p: 2 }}>
        <Typography variant="subtitle1" gutterBottom>
          Date Range
        </Typography>
        <Box sx={{ display: "flex", gap: 2, alignItems: "center" }}>
          <TextField
            label="From Date"
            type="date"
            value={dateRange.fromDate}
            onChange={handleDateChange("fromDate")}
            InputLabelProps={{ shrink: true }}
            size="small"
          />
          <TextField
            label="To Date"
            type="date"
            value={dateRange.toDate}
            onChange={handleDateChange("toDate")}
            InputLabelProps={{ shrink: true }}
            size="small"
          />
          <Button variant="outlined" onClick={handleRefresh} disabled={loading}>
            Apply
          </Button>
        </Box>
      </Paper>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {reportData && (
        <>
          {/* Key Metrics Cards */}
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, mb: 3 }}>
            <Card sx={{ minWidth: 250, flex: "1 1 250px" }}>
              <CardContent>
                <Typography color="text.secondary" gutterBottom>
                  Total Applications
                </Typography>
                <Typography variant="h4">
                  {reportData.totalApplications.toLocaleString()}
                </Typography>
              </CardContent>
            </Card>

            <Card sx={{ minWidth: 250, flex: "1 1 250px" }}>
              <CardContent>
                <Typography color="text.secondary" gutterBottom>
                  Approval Rate
                </Typography>
                <Typography variant="h4" color="success.main">
                  {formatPercentage(reportData.approvalRate)}
                </Typography>
                <Box sx={{ mt: 1 }}>
                  <LinearProgress
                    variant="determinate"
                    value={reportData.approvalRate * 100}
                    color="success"
                  />
                </Box>
              </CardContent>
            </Card>

            <Card sx={{ minWidth: 250, flex: "1 1 250px" }}>
              <CardContent>
                <Typography color="text.secondary" gutterBottom>
                  Total Loan Amount
                </Typography>
                <Typography variant="h4">
                  {formatCurrency(reportData.totalLoanAmount)}
                </Typography>
              </CardContent>
            </Card>

            <Card sx={{ minWidth: 250, flex: "1 1 250px" }}>
              <CardContent>
                <Typography color="text.secondary" gutterBottom>
                  Avg Processing Time
                </Typography>
                <Typography variant="h4">
                  {formatDuration(reportData.averageProcessingTime)}
                </Typography>
              </CardContent>
            </Card>
          </Box>

          {/* Application Status Breakdown */}
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, mb: 3 }}>
            <Card sx={{ minWidth: 200, flex: "1 1 200px" }}>
              <CardContent>
                <Typography color="text.secondary" gutterBottom>
                  Approved
                </Typography>
                <Typography variant="h5" color="success.main">
                  {reportData.approvedApplications.toLocaleString()}
                </Typography>
              </CardContent>
            </Card>

            <Card sx={{ minWidth: 200, flex: "1 1 200px" }}>
              <CardContent>
                <Typography color="text.secondary" gutterBottom>
                  Rejected
                </Typography>
                <Typography variant="h5" color="error.main">
                  {reportData.rejectedApplications.toLocaleString()}
                </Typography>
              </CardContent>
            </Card>

            <Card sx={{ minWidth: 200, flex: "1 1 200px" }}>
              <CardContent>
                <Typography color="text.secondary" gutterBottom>
                  Pending Review
                </Typography>
                <Typography variant="h5" color="warning.main">
                  {reportData.pendingApplications.toLocaleString()}
                </Typography>
              </CardContent>
            </Card>

            <Card sx={{ minWidth: 200, flex: "1 1 200px" }}>
              <CardContent>
                <Typography color="text.secondary" gutterBottom>
                  Average Loan Amount
                </Typography>
                <Typography variant="h5">
                  {formatCurrency(reportData.averageLoanAmount)}
                </Typography>
              </CardContent>
            </Card>
          </Box>

          {/* Top Affiliates */}
          <Paper sx={{ mb: 3 }}>
            <Box sx={{ p: 2, borderBottom: 1, borderColor: "divider" }}>
              <Typography
                variant="h6"
                sx={{ display: "flex", alignItems: "center", gap: 1 }}
              >
                <TrendingUpIcon />
                Top Performing Affiliates
              </Typography>
            </Box>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Affiliate</TableCell>
                    <TableCell align="right">Applications</TableCell>
                    <TableCell align="right">Approval Rate</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {reportData.topAffiliates.map((affiliate) => (
                    <TableRow key={affiliate.affiliateId}>
                      <TableCell>{affiliate.affiliateName}</TableCell>
                      <TableCell align="right">
                        {affiliate.applicationCount.toLocaleString()}
                      </TableCell>
                      <TableCell align="right">
                        {formatPercentage(affiliate.approvalRate)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              {reportData.topAffiliates.length === 0 && (
                <Box sx={{ p: 4, textAlign: "center" }}>
                  <Typography color="text.secondary">
                    No affiliate data available
                  </Typography>
                </Box>
              )}
            </TableContainer>
          </Paper>

          {/* Daily Statistics */}
          <Paper>
            <Box sx={{ p: 2, borderBottom: 1, borderColor: "divider" }}>
              <Typography variant="h6">Daily Application Statistics</Typography>
            </Box>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Date</TableCell>
                    <TableCell align="right">Applications</TableCell>
                    <TableCell align="right">Approvals</TableCell>
                    <TableCell align="right">Rejections</TableCell>
                    <TableCell align="right">Approval Rate</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {reportData.dailyStats.map((stat) => {
                    const approvalRate =
                      stat.applications > 0
                        ? stat.approvals / stat.applications
                        : 0;
                    return (
                      <TableRow key={stat.date}>
                        <TableCell>
                          {new Date(stat.date).toLocaleDateString()}
                        </TableCell>
                        <TableCell align="right">
                          {stat.applications.toLocaleString()}
                        </TableCell>
                        <TableCell align="right">
                          {stat.approvals.toLocaleString()}
                        </TableCell>
                        <TableCell align="right">
                          {stat.rejections.toLocaleString()}
                        </TableCell>
                        <TableCell align="right">
                          {formatPercentage(approvalRate)}
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
              {reportData.dailyStats.length === 0 && (
                <Box sx={{ p: 4, textAlign: "center" }}>
                  <Typography color="text.secondary">
                    No daily statistics available
                  </Typography>
                </Box>
              )}
            </TableContainer>
          </Paper>
        </>
      )}
    </Box>
  );
};
