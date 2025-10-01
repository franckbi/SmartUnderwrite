import React from "react";
import { Box, Typography } from "@mui/material";
import { RulesManagement } from "@/components/admin/RulesManagement";

export const AdminPage: React.FC = () => {
  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Rules Management
      </Typography>
      <RulesManagement />
    </Box>
  );
};
