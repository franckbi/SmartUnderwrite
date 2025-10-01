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
  Business as BusinessIcon,
} from "@mui/icons-material";
import {
  Affiliate,
  CreateAffiliateRequest,
  UpdateAffiliateRequest,
} from "@/types/admin";
import { adminService } from "@/services/adminService";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { useErrorHandler } from "@/hooks/useErrorHandler";

interface AffiliateDialogProps {
  open: boolean;
  affiliate?: Affiliate;
  onClose: () => void;
  onSave: (affiliate: CreateAffiliateRequest | UpdateAffiliateRequest) => void;
}

const AffiliateDialog: React.FC<AffiliateDialogProps> = ({
  open,
  affiliate,
  onClose,
  onSave,
}) => {
  const { error, fieldErrors, handleError, clearError, clearFieldError } =
    useErrorHandler();
  const [formData, setFormData] = useState({
    name: "",
    externalId: "",
    isActive: true,
  });

  useEffect(() => {
    if (affiliate) {
      setFormData({
        name: affiliate.name,
        externalId: affiliate.externalId,
        isActive: affiliate.isActive,
      });
    } else {
      setFormData({
        name: "",
        externalId: "",
        isActive: true,
      });
    }
    clearError();
  }, [affiliate, open]);

  const handleChange =
    (field: string) => (event: React.ChangeEvent<HTMLInputElement>) => {
      const value =
        event.target.type === "checkbox"
          ? event.target.checked
          : event.target.value;
      setFormData((prev) => ({ ...prev, [field]: value }));
      clearFieldError(field);
    };

  const handleSave = () => {
    if (affiliate) {
      onSave({ ...formData, id: affiliate.id } as UpdateAffiliateRequest);
    } else {
      onSave(formData as CreateAffiliateRequest);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        {affiliate ? "Edit Affiliate" : "Create New Affiliate"}
      </DialogTitle>
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
            label="Affiliate Name"
            value={formData.name}
            onChange={handleChange("name")}
            error={!!fieldErrors.name}
            helperText={fieldErrors.name}
          />

          <TextField
            required
            fullWidth
            label="External ID"
            value={formData.externalId}
            onChange={handleChange("externalId")}
            error={!!fieldErrors.externalId}
            helperText={
              fieldErrors.externalId || "Unique identifier for this affiliate"
            }
            placeholder="e.g., PFP001, CCS002"
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
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          onClick={handleSave}
          disabled={!formData.name || !formData.externalId}
        >
          {affiliate ? "Update" : "Create"}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export const AffiliatesManagement: React.FC = () => {
  const { error, handleError, clearError } = useErrorHandler();
  const [affiliates, setAffiliates] = useState<Affiliate[]>([]);
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedAffiliate, setSelectedAffiliate] = useState<
    Affiliate | undefined
  >();
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [affiliateToDelete, setAffiliateToDelete] = useState<Affiliate | null>(
    null
  );

  const loadAffiliates = async () => {
    try {
      setLoading(true);
      clearError();
      const result = await adminService.getAffiliates();
      setAffiliates(result);
    } catch (err) {
      handleError(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadAffiliates();
  }, []);

  const handleCreateAffiliate = () => {
    setSelectedAffiliate(undefined);
    setDialogOpen(true);
  };

  const handleEditAffiliate = (affiliate: Affiliate) => {
    setSelectedAffiliate(affiliate);
    setDialogOpen(true);
  };

  const handleDeleteAffiliate = (affiliate: Affiliate) => {
    setAffiliateToDelete(affiliate);
    setDeleteConfirmOpen(true);
  };

  const handleSaveAffiliate = async (
    affiliateData: CreateAffiliateRequest | UpdateAffiliateRequest
  ) => {
    try {
      clearError();
      if ("id" in affiliateData) {
        await adminService.updateAffiliate(affiliateData);
      } else {
        await adminService.createAffiliate(affiliateData);
      }
      setDialogOpen(false);
      await loadAffiliates();
    } catch (err) {
      handleError(err);
    }
  };

  const confirmDelete = async () => {
    if (!affiliateToDelete) return;

    try {
      clearError();
      await adminService.deleteAffiliate(affiliateToDelete.id);
      setDeleteConfirmOpen(false);
      setAffiliateToDelete(null);
      await loadAffiliates();
    } catch (err) {
      handleError(err);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  if (loading) {
    return <LoadingSpinner message="Loading affiliates..." />;
  }

  return (
    <Box>
      <Toolbar sx={{ pl: 0, pr: 0 }}>
        <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
          Affiliates Management
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={handleCreateAffiliate}
        >
          Create Affiliate
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
              <TableCell>External ID</TableCell>
              <TableCell>Users</TableCell>
              <TableCell>Applications</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Created</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {affiliates.map((affiliate) => (
              <TableRow key={affiliate.id} hover>
                <TableCell>
                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    <BusinessIcon color="primary" />
                    <Typography variant="subtitle2">
                      {affiliate.name}
                    </Typography>
                  </Box>
                </TableCell>
                <TableCell>
                  <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
                    {affiliate.externalId}
                  </Typography>
                </TableCell>
                <TableCell>{affiliate.userCount}</TableCell>
                <TableCell>{affiliate.applicationCount}</TableCell>
                <TableCell>
                  <Chip
                    label={affiliate.isActive ? "Active" : "Inactive"}
                    color={affiliate.isActive ? "success" : "default"}
                    size="small"
                  />
                </TableCell>
                <TableCell>{formatDate(affiliate.createdAt)}</TableCell>
                <TableCell>
                  <IconButton
                    size="small"
                    onClick={() => handleEditAffiliate(affiliate)}
                    title="Edit Affiliate"
                  >
                    <EditIcon />
                  </IconButton>
                  <IconButton
                    size="small"
                    onClick={() => handleDeleteAffiliate(affiliate)}
                    title="Delete Affiliate"
                    color="error"
                  >
                    <DeleteIcon />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        {affiliates.length === 0 && (
          <Box sx={{ p: 4, textAlign: "center" }}>
            <Typography color="text.secondary">No affiliates found</Typography>
          </Box>
        )}
      </TableContainer>

      <AffiliateDialog
        open={dialogOpen}
        affiliate={selectedAffiliate}
        onClose={() => setDialogOpen(false)}
        onSave={handleSaveAffiliate}
      />

      {/* Delete Confirmation Dialog */}
      <Dialog
        open={deleteConfirmOpen}
        onClose={() => setDeleteConfirmOpen(false)}
      >
        <DialogTitle>Confirm Deactivation</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to deactivate the affiliate "
            {affiliateToDelete?.name}"? This will prevent them from creating new
            applications.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteConfirmOpen(false)}>Cancel</Button>
          <Button onClick={confirmDelete} color="error" variant="contained">
            Deactivate
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
