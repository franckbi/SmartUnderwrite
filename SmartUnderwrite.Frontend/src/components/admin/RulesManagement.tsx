import React, { useState, useEffect } from "react";
import {
  Box,
  Paper,
  Typography,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Switch,
  FormControlLabel,
  Alert,
  Toolbar,
} from "@mui/material";
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Code as CodeIcon,
} from "@mui/icons-material";
import { Rule, CreateRuleRequest, UpdateRuleRequest } from "@/types/admin";
import { adminService } from "@/services/adminService";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { useErrorHandler } from "@/hooks/useErrorHandler";

interface RuleDialogProps {
  open: boolean;
  rule?: Rule;
  onClose: () => void;
  onSave: (rule: CreateRuleRequest | UpdateRuleRequest) => void;
}

const RuleDialog: React.FC<RuleDialogProps> = ({
  open,
  rule,
  onClose,
  onSave,
}) => {
  const { error, fieldErrors, handleError, clearError, clearFieldError } =
    useErrorHandler();
  const [formData, setFormData] = useState({
    name: "",
    description: "",
    conditions: "",
    actions: "",
    isActive: true,
    priority: 1,
  });
  const [validating, setValidating] = useState(false);
  const [validationResult, setValidationResult] = useState<{
    isValid: boolean;
    errors: string[];
  } | null>(null);

  useEffect(() => {
    if (rule) {
      setFormData({
        name: rule.name,
        description: rule.description,
        conditions: rule.conditions,
        actions: rule.actions,
        isActive: rule.isActive,
        priority: rule.priority,
      });
    } else {
      setFormData({
        name: "",
        description: "",
        conditions: "",
        actions: "",
        isActive: true,
        priority: 1,
      });
    }
    clearError();
    setValidationResult(null);
  }, [rule, open]);

  const handleChange =
    (field: string) => (event: React.ChangeEvent<HTMLInputElement>) => {
      const value =
        event.target.type === "checkbox"
          ? event.target.checked
          : event.target.value;
      setFormData((prev) => ({ ...prev, [field]: value }));
      clearFieldError(field);
      setValidationResult(null);
    };

  const handleValidate = async () => {
    try {
      setValidating(true);
      clearError();
      const result = await adminService.validateRule(
        formData.conditions,
        formData.actions
      );
      setValidationResult(result);
    } catch (err) {
      handleError(err);
    } finally {
      setValidating(false);
    }
  };

  const handleSave = () => {
    if (rule) {
      onSave({ ...formData, id: rule.id } as UpdateRuleRequest);
    } else {
      onSave(formData as CreateRuleRequest);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>{rule ? "Edit Rule" : "Create New Rule"}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 1 }}>
          <TextField
            required
            fullWidth
            label="Rule Name"
            value={formData.name}
            onChange={handleChange("name")}
            error={!!fieldErrors.name}
            helperText={fieldErrors.name}
          />

          <TextField
            fullWidth
            multiline
            rows={2}
            label="Description"
            value={formData.description}
            onChange={handleChange("description")}
            error={!!fieldErrors.description}
            helperText={fieldErrors.description}
          />

          <Box sx={{ display: "flex", gap: 2 }}>
            <TextField
              type="number"
              label="Priority"
              value={formData.priority}
              onChange={handleChange("priority")}
              error={!!fieldErrors.priority}
              helperText={
                fieldErrors.priority || "Lower numbers = higher priority"
              }
              inputProps={{ min: 1, max: 100 }}
              sx={{ width: 150 }}
            />
            <FormControlLabel
              control={
                <Switch
                  checked={formData.isActive}
                  onChange={handleChange("isActive")}
                />
              }
              label="Active"
            />
          </Box>

          <TextField
            required
            fullWidth
            multiline
            rows={4}
            label="Conditions (JSON)"
            value={formData.conditions}
            onChange={handleChange("conditions")}
            error={!!fieldErrors.conditions}
            helperText={
              fieldErrors.conditions ||
              "JSON object defining when this rule applies"
            }
            placeholder='{"creditScore": {"$gte": 650}, "incomeMonthly": {"$gte": 3000}}'
          />

          <TextField
            required
            fullWidth
            multiline
            rows={4}
            label="Actions (JSON)"
            value={formData.actions}
            onChange={handleChange("actions")}
            error={!!fieldErrors.actions}
            helperText={
              fieldErrors.actions ||
              "JSON object defining what happens when rule matches"
            }
            placeholder='{"outcome": "Approve", "score": 85, "reasons": ["Good credit score"]}'
          />

          <Box sx={{ display: "flex", gap: 2, alignItems: "center" }}>
            <Button
              variant="outlined"
              startIcon={<CodeIcon />}
              onClick={handleValidate}
              disabled={validating || !formData.conditions || !formData.actions}
            >
              {validating ? "Validating..." : "Validate JSON"}
            </Button>

            {validationResult && (
              <Chip
                label={validationResult.isValid ? "Valid" : "Invalid"}
                color={validationResult.isValid ? "success" : "error"}
              />
            )}
          </Box>

          {validationResult && !validationResult.isValid && (
            <Alert severity="error">
              <Typography variant="subtitle2">Validation Errors:</Typography>
              <ul>
                {validationResult.errors.map((error, index) => (
                  <li key={index}>{error}</li>
                ))}
              </ul>
            </Alert>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          onClick={handleSave}
          disabled={!formData.name || !formData.conditions || !formData.actions}
        >
          {rule ? "Update" : "Create"}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export const RulesManagement: React.FC = () => {
  const { error, handleError, clearError } = useErrorHandler();
  const [rules, setRules] = useState<Rule[]>([]);
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedRule, setSelectedRule] = useState<Rule | undefined>();
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [ruleToDelete, setRuleToDelete] = useState<Rule | null>(null);

  const loadRules = async () => {
    try {
      setLoading(true);
      clearError();
      const result = await adminService.getRules();
      setRules(result);
    } catch (err) {
      handleError(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadRules();
  }, []);

  const handleCreateRule = () => {
    setSelectedRule(undefined);
    setDialogOpen(true);
  };

  const handleEditRule = (rule: Rule) => {
    setSelectedRule(rule);
    setDialogOpen(true);
  };

  const handleDeleteRule = (rule: Rule) => {
    setRuleToDelete(rule);
    setDeleteConfirmOpen(true);
  };

  const handleSaveRule = async (
    ruleData: CreateRuleRequest | UpdateRuleRequest
  ) => {
    try {
      clearError();
      if ("id" in ruleData) {
        await adminService.updateRule(ruleData);
      } else {
        await adminService.createRule(ruleData);
      }
      setDialogOpen(false);
      await loadRules();
    } catch (err) {
      handleError(err);
    }
  };

  const confirmDelete = async () => {
    if (!ruleToDelete) return;

    try {
      clearError();
      await adminService.deleteRule(ruleToDelete.id);
      setDeleteConfirmOpen(false);
      setRuleToDelete(null);
      await loadRules();
    } catch (err) {
      handleError(err);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  if (loading) {
    return <LoadingSpinner message="Loading rules..." />;
  }

  return (
    <Box>
      <Toolbar sx={{ pl: 0, pr: 0 }}>
        <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
          Rules Management
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={handleCreateRule}
        >
          Create Rule
        </Button>
      </Toolbar>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Description</TableCell>
              <TableCell>Priority</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Created</TableCell>
              <TableCell>Created By</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rules.map((rule) => (
              <TableRow key={rule.id} hover>
                <TableCell>
                  <Typography variant="subtitle2">{rule.name}</Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2" sx={{ maxWidth: 300 }}>
                    {rule.description}
                  </Typography>
                </TableCell>
                <TableCell>{rule.priority}</TableCell>
                <TableCell>
                  <Chip
                    label={rule.isActive ? "Active" : "Inactive"}
                    color={rule.isActive ? "success" : "default"}
                    size="small"
                  />
                </TableCell>
                <TableCell>{formatDate(rule.createdAt)}</TableCell>
                <TableCell>{rule.createdBy}</TableCell>
                <TableCell>
                  <IconButton
                    size="small"
                    onClick={() => handleEditRule(rule)}
                    title="Edit Rule"
                  >
                    <EditIcon />
                  </IconButton>
                  <IconButton
                    size="small"
                    onClick={() => handleDeleteRule(rule)}
                    title="Delete Rule"
                    color="error"
                  >
                    <DeleteIcon />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        {rules.length === 0 && (
          <Box sx={{ p: 4, textAlign: "center" }}>
            <Typography color="text.secondary">No rules configured</Typography>
          </Box>
        )}
      </TableContainer>

      <RuleDialog
        open={dialogOpen}
        rule={selectedRule}
        onClose={() => setDialogOpen(false)}
        onSave={handleSaveRule}
      />

      {/* Delete Confirmation Dialog */}
      <Dialog
        open={deleteConfirmOpen}
        onClose={() => setDeleteConfirmOpen(false)}
      >
        <DialogTitle>Confirm Delete</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete the rule "{ruleToDelete?.name}"?
            This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteConfirmOpen(false)}>Cancel</Button>
          <Button onClick={confirmDelete} color="error" variant="contained">
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
