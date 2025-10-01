import React from "react";
import { Box, Typography } from "@mui/material";
import { Reports } from "@/components/admin/Reports";

export const ReportsPage: React.FC = () => {
  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Reports & Analytics
      </Typography>
      <Reports />
    </Box>
  );
};
