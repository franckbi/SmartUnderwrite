import React from "react";
import { Box, Typography } from "@mui/material";
import { AffiliatesManagement } from "@/components/admin/AffiliatesManagement";

export const AffiliatesPage: React.FC = () => {
  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Affiliates Management
      </Typography>
      <AffiliatesManagement />
    </Box>
  );
};
