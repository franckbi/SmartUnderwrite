import React from "react";
import { Box, Typography } from "@mui/material";
import { AuditLogs } from "@/components/admin/AuditLogs";

export const AuditLogsPage: React.FC = () => {
  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Audit Logs
      </Typography>
      <AuditLogs />
    </Box>
  );
};
