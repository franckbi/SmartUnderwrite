import React, { useState } from "react";
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  MenuItem,
  Alert,
  Card,
  CardContent,
} from "@mui/material";
import { ArrowBack as BackIcon, Save as SaveIcon } from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import {
  CreateApplicationRequest,
  CreateApplicantRequest,
} from "@/types/application";
import { applicationService } from "@/services/applicationService";
import { useErrorHandler } from "@/hooks/useErrorHandler";

const employmentTypes = [
  "Full-time",
  "Part-time",
  "Self-employed",
  "Contract",
  "Unemployed",
  "Retired",
];

const productTypes = [
  "Personal Loan",
  "Auto Loan",
  "Home Loan",
  "Business Loan",
];

export const SimpleCreateApplicationForm: React.FC = () => {
  const navigate = useNavigate();
  const { error, fieldErrors, handleError, clearError, clearFieldError } =
    useErrorHandler();

  const [submitting, setSubmitting] = useState(false);

  const [formData, setFormData] = useState({
    // Applicant
    firstName: "",
    lastName: "",
    ssn: "",
    dateOfBirth: "",
    email: "",
    phone: "",
    street: "",
    city: "",
    state: "",
    zipCode: "",
    // Loan
    productType: "",
    amount: "",
    incomeMonthly: "",
    employmentType: "",
    creditScore: "",
  });

  const handleChange =
    (field: string) => (event: React.ChangeEvent<HTMLInputElement>) => {
      const value = event.target.value;
      setFormData((prev) => ({ ...prev, [field]: value }));
      clearFieldError(field);
    };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    try {
      setSubmitting(true);
      clearError();

      const applicant: CreateApplicantRequest = {
        firstName: formData.firstName,
        lastName: formData.lastName,
        ssn: formData.ssn,
        dateOfBirth: formData.dateOfBirth,
        email: formData.email,
        phone: formData.phone,
        address: {
          street: formData.street,
          city: formData.city,
          state: formData.state,
          zipCode: formData.zipCode,
        },
      };

      const request: CreateApplicationRequest = {
        applicant,
        productType: formData.productType,
        amount: parseFloat(formData.amount),
        incomeMonthly: parseFloat(formData.incomeMonthly),
        employmentType: formData.employmentType,
        creditScore: formData.creditScore
          ? parseInt(formData.creditScore)
          : undefined,
      };

      const result = await applicationService.createApplication(request);
      navigate(`/applications/${result.id}`);
    } catch (err) {
      handleError(err);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Box>
      <Box sx={{ mb: 3, display: "flex", alignItems: "center", gap: 2 }}>
        <Button
          startIcon={<BackIcon />}
          onClick={() => navigate("/applications")}
        >
          Back to Applications
        </Button>
        <Typography variant="h4" component="h1">
          New Loan Application
        </Typography>
      </Box>

      <Paper sx={{ p: 3 }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit} noValidate>
          {/* Applicant Information */}
          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Applicant Information
              </Typography>

              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, mb: 2 }}>
                <TextField
                  required
                  label="First Name"
                  value={formData.firstName}
                  onChange={handleChange("firstName")}
                  error={!!fieldErrors.firstName}
                  helperText={fieldErrors.firstName}
                  sx={{ flex: "1 1 250px" }}
                />
                <TextField
                  required
                  label="Last Name"
                  value={formData.lastName}
                  onChange={handleChange("lastName")}
                  error={!!fieldErrors.lastName}
                  helperText={fieldErrors.lastName}
                  sx={{ flex: "1 1 250px" }}
                />
              </Box>

              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, mb: 2 }}>
                <TextField
                  required
                  label="Social Security Number"
                  value={formData.ssn}
                  onChange={handleChange("ssn")}
                  error={!!fieldErrors.ssn}
                  helperText={fieldErrors.ssn}
                  placeholder="XXX-XX-XXXX"
                  sx={{ flex: "1 1 250px" }}
                />
                <TextField
                  required
                  label="Date of Birth"
                  type="date"
                  value={formData.dateOfBirth}
                  onChange={handleChange("dateOfBirth")}
                  error={!!fieldErrors.dateOfBirth}
                  helperText={fieldErrors.dateOfBirth}
                  InputLabelProps={{ shrink: true }}
                  sx={{ flex: "1 1 250px" }}
                />
              </Box>

              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, mb: 2 }}>
                <TextField
                  required
                  label="Email"
                  type="email"
                  value={formData.email}
                  onChange={handleChange("email")}
                  error={!!fieldErrors.email}
                  helperText={fieldErrors.email}
                  sx={{ flex: "1 1 250px" }}
                />
                <TextField
                  required
                  label="Phone"
                  value={formData.phone}
                  onChange={handleChange("phone")}
                  error={!!fieldErrors.phone}
                  helperText={fieldErrors.phone}
                  placeholder="(XXX) XXX-XXXX"
                  sx={{ flex: "1 1 250px" }}
                />
              </Box>

              <Typography variant="subtitle1" sx={{ mt: 2, mb: 1 }}>
                Address
              </Typography>

              <TextField
                required
                fullWidth
                label="Street Address"
                value={formData.street}
                onChange={handleChange("street")}
                error={!!fieldErrors.street}
                helperText={fieldErrors.street}
                sx={{ mb: 2 }}
              />

              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2 }}>
                <TextField
                  required
                  label="City"
                  value={formData.city}
                  onChange={handleChange("city")}
                  error={!!fieldErrors.city}
                  helperText={fieldErrors.city}
                  sx={{ flex: "2 1 200px" }}
                />
                <TextField
                  required
                  label="State"
                  value={formData.state}
                  onChange={handleChange("state")}
                  error={!!fieldErrors.state}
                  helperText={fieldErrors.state}
                  sx={{ flex: "1 1 100px" }}
                />
                <TextField
                  required
                  label="ZIP Code"
                  value={formData.zipCode}
                  onChange={handleChange("zipCode")}
                  error={!!fieldErrors.zipCode}
                  helperText={fieldErrors.zipCode}
                  sx={{ flex: "1 1 120px" }}
                />
              </Box>
            </CardContent>
          </Card>

          {/* Loan Information */}
          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Loan Information
              </Typography>

              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, mb: 2 }}>
                <TextField
                  required
                  select
                  label="Product Type"
                  value={formData.productType}
                  onChange={handleChange("productType")}
                  error={!!fieldErrors.productType}
                  helperText={fieldErrors.productType}
                  sx={{ flex: "1 1 250px" }}
                >
                  {productTypes.map((type) => (
                    <MenuItem key={type} value={type}>
                      {type}
                    </MenuItem>
                  ))}
                </TextField>
                <TextField
                  required
                  label="Loan Amount"
                  type="number"
                  value={formData.amount}
                  onChange={handleChange("amount")}
                  error={!!fieldErrors.amount}
                  helperText={fieldErrors.amount}
                  InputProps={{ startAdornment: "$" }}
                  sx={{ flex: "1 1 250px" }}
                />
              </Box>

              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2, mb: 2 }}>
                <TextField
                  required
                  label="Monthly Income"
                  type="number"
                  value={formData.incomeMonthly}
                  onChange={handleChange("incomeMonthly")}
                  error={!!fieldErrors.incomeMonthly}
                  helperText={fieldErrors.incomeMonthly}
                  InputProps={{ startAdornment: "$" }}
                  sx={{ flex: "1 1 250px" }}
                />
                <TextField
                  required
                  select
                  label="Employment Type"
                  value={formData.employmentType}
                  onChange={handleChange("employmentType")}
                  error={!!fieldErrors.employmentType}
                  helperText={fieldErrors.employmentType}
                  sx={{ flex: "1 1 250px" }}
                >
                  {employmentTypes.map((type) => (
                    <MenuItem key={type} value={type}>
                      {type}
                    </MenuItem>
                  ))}
                </TextField>
              </Box>

              <TextField
                label="Credit Score (Optional)"
                type="number"
                value={formData.creditScore}
                onChange={handleChange("creditScore")}
                error={!!fieldErrors.creditScore}
                helperText={fieldErrors.creditScore || "Leave blank if unknown"}
                inputProps={{ min: 300, max: 850 }}
                sx={{ width: "250px" }}
              />
            </CardContent>
          </Card>

          <Box sx={{ display: "flex", justifyContent: "flex-end", gap: 2 }}>
            <Button
              variant="outlined"
              onClick={() => navigate("/applications")}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              variant="contained"
              disabled={submitting}
              startIcon={<SaveIcon />}
            >
              {submitting ? "Submitting..." : "Submit Application"}
            </Button>
          </Box>
        </Box>
      </Paper>
    </Box>
  );
};
