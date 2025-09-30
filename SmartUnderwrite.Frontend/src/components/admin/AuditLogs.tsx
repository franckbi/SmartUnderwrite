import React, { useState, useEffect } from "react";
import {
  Box,
  Paper,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  TextField,
  MenuItem,
  Button,
  Chip,
  Alert,
  Toolbar,
  Accordion,
  AccordionSummary,
  AccordionDetails,
} from "@mui/material";
import {
  Search as SearchIcon,
  ExpandMore as ExpandMoreIcon,
  Refresh as RefreshIcon,
} from "@mui/icons-material";
import { AuditLog, AuditLogFilter } from "@/types/admin";
import { PagedResult } from "@/types/api";
import { adminService } from "@/services/adminService";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { useErrorHandler } from "@/hooks/useErrorHandler";

export const AuditLogs: React.FC = () => {
  const { error, handleError, clearError } = useErrorHandler();

  const [logs, setLogs] = useState<PagedResult<AuditLog>>({
    items: [],
    totalCount: 0,
    pageNumber: 1,
    pageSize: 25,
    totalPages: 0,
  });
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<AuditLogFilter>({
    pageNumber: 1,
    pageSize: 25,
  });

  const loadLogs = async () => {
    try {
      setLoading(true);
      clearError();
      const result = await adminService.getAuditLogs(filter);
      setLogs(result);
    } catch (err) {
      handleError(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadLogs();
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

  const handleFilterChange =
    (field: keyof AuditLogFilter) =>
    (event: React.ChangeEvent<HTMLInputElement>) => {
      const value = event.target.value;
      setFilter((prev) => ({
        ...prev,
        [field]: value || undefined,
        pageNumber: 1,
      }));
    };

  const handleDateFilterChange =
    (field: "fromDate" | "toDate") =>
    (event: React.ChangeEvent<HTMLInputElement>) => {
      const value = event.target.value;
      setFilter((prev) => ({
        ...prev,
        [field]: value || undefined,
        pageNumber: 1,
      }));
    };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  const formatJsonData = (jsonString?: string) => {
    if (!jsonString) return "N/A";
    try {
      const parsed = JSON.parse(jsonString);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return jsonString;
    }
  };

  const getActionColor = (
    action: string
  ):
    | "default"
    | "primary"
    | "secondary"
    | "error"
    | "info"
    | "success"
    | "warning" => {
    switch (action.toLowerCase()) {
      case "create":
        return "success";
      case "update":
        return "info";
      case "delete":
        return "error";
      case "login":
        return "primary";
      case "logout":
        return "secondary";
      default:
        return "default";
    }
  };

  if (loading && logs.items.length === 0) {
    return <LoadingSpinner message="Loading audit logs..." />;
  }

  return (
    <Box>
      <Toolbar sx={{ pl: 0, pr: 0 }}>
        <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
          Audit Logs
        </Typography>
        <Button
          variant="outlined"
          startIcon={<RefreshIcon />}
          onClick={loadLogs}
          disabled={loading}
        >
          Refresh
        </Button>
      </Toolbar>

      {/* Filters */}
      <Paper sx={{ mb: 2, p: 2 }}>
        <Typography variant="subtitle1" gutterBottom>
          Filters
        </Typography>
        <Box
          sx={{
            display: "flex",
            flexWrap: "wrap",
            gap: 2,
            alignItems: "center",
          }}
        >
          <TextField
            label="Action"
            value={filter.action || ""}
            onChange={handleFilterChange("action")}
            sx={{ minWidth: 150 }}
            size="small"
            placeholder="e.g., Create, Update, Delete"
          />
          <TextField
            label="Entity Type"
            value={filter.entityType || ""}
            onChange={handleFilterChange("entityType")}
            sx={{ minWidth: 150 }}
            size="small"
            placeholder="e.g., Application, Rule"
          />
          <TextField
            label="From Date"
            type="datetime-local"
            value={filter.fromDate || ""}
            onChange={handleDateFilterChange("fromDate")}
            sx={{ minWidth: 200 }}
            size="small"
            InputLabelProps={{ shrink: true }}
          />
          <TextField
            label="To Date"
            type="datetime-local"
            value={filter.toDate || ""}
            onChange={handleDateFilterChange("toDate")}
            sx={{ minWidth: 200 }}
            size="small"
            InputLabelProps={{ shrink: true }}
          />
          <Button
            variant="outlined"
            startIcon={<SearchIcon />}
            onClick={loadLogs}
            disabled={loading}
          >
            Search
          </Button>
        </Box>
      </Paper>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Timestamp</TableCell>
              <TableCell>User</TableCell>
              <TableCell>Action</TableCell>
              <TableCell>Entity</TableCell>
              <TableCell>Entity ID</TableCell>
              <TableCell>IP Address</TableCell>
              <TableCell>Details</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {logs.items.map((log) => (
              <TableRow key={log.id} hover>
                <TableCell>{formatDate(log.timestamp)}</TableCell>
                <TableCell>{log.userName}</TableCell>
                <TableCell>
                  <Chip
                    label={log.action}
                    color={getActionColor(log.action)}
                    size="small"
                  />
                </TableCell>
                <TableCell>{log.entityType}</TableCell>
                <TableCell>{log.entityId}</TableCell>
                <TableCell>{log.ipAddress}</TableCell>
                <TableCell>
                  {(log.oldValues || log.newValues) && (
                    <Accordion>
                      <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                        <Typography variant="body2">View Changes</Typography>
                      </AccordionSummary>
                      <AccordionDetails>
                        <Box
                          sx={{
                            display: "flex",
                            flexDirection: "column",
                            gap: 2,
                          }}
                        >
                          {log.oldValues && (
                            <Box>
                              <Typography variant="subtitle2" gutterBottom>
                                Old Values:
                              </Typography>
                              <Paper sx={{ p: 1, bgcolor: "grey.50" }}>
                                <pre
                                  style={{
                                    fontSize: "12px",
                                    margin: 0,
                                    whiteSpace: "pre-wrap",
                                  }}
                                >
                                  {formatJsonData(log.oldValues)}
                                </pre>
                              </Paper>
                            </Box>
                          )}
                          {log.newValues && (
                            <Box>
                              <Typography variant="subtitle2" gutterBottom>
                                New Values:
                              </Typography>
                              <Paper sx={{ p: 1, bgcolor: "grey.50" }}>
                                <pre
                                  style={{
                                    fontSize: "12px",
                                    margin: 0,
                                    whiteSpace: "pre-wrap",
                                  }}
                                >
                                  {formatJsonData(log.newValues)}
                                </pre>
                              </Paper>
                            </Box>
                          )}
                        </Box>
                      </AccordionDetails>
                    </Accordion>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        {logs.items.length === 0 && !loading && (
          <Box sx={{ p: 4, textAlign: "center" }}>
            <Typography color="text.secondary">No audit logs found</Typography>
          </Box>
        )}
        <TablePagination
          rowsPerPageOptions={[10, 25, 50, 100]}
          component="div"
          count={logs.totalCount}
          rowsPerPage={logs.pageSize}
          page={logs.pageNumber - 1}
          onPageChange={handlePageChange}
          onRowsPerPageChange={handleRowsPerPageChange}
        />
      </TableContainer>
    </Box>
  );
};
