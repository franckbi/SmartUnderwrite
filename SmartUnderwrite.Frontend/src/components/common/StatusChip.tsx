import React from "react";
import { Chip, useTheme, alpha } from "@mui/material";
import {
  CheckCircle,
  Cancel,
  Schedule,
  Warning,
  Info,
} from "@mui/icons-material";

interface StatusChipProps {
  status: string;
  variant?: "default" | "outlined" | "filled";
  size?: "small" | "medium";
  showIcon?: boolean;
}

const getStatusConfig = (status: string, theme: any) => {
  const statusLower = status.toLowerCase();

  if (
    statusLower.includes("approved") ||
    statusLower.includes("success") ||
    statusLower.includes("active")
  ) {
    return {
      color: theme.palette.success.main,
      bgcolor: alpha(theme.palette.success.main, 0.1),
      icon: <CheckCircle sx={{ fontSize: "inherit" }} />,
    };
  }

  if (
    statusLower.includes("rejected") ||
    statusLower.includes("error") ||
    statusLower.includes("failed")
  ) {
    return {
      color: theme.palette.error.main,
      bgcolor: alpha(theme.palette.error.main, 0.1),
      icon: <Cancel sx={{ fontSize: "inherit" }} />,
    };
  }

  if (
    statusLower.includes("pending") ||
    statusLower.includes("review") ||
    statusLower.includes("manual")
  ) {
    return {
      color: theme.palette.warning.main,
      bgcolor: alpha(theme.palette.warning.main, 0.1),
      icon: <Warning sx={{ fontSize: "inherit" }} />,
    };
  }

  if (
    statusLower.includes("submitted") ||
    statusLower.includes("processing") ||
    statusLower.includes("evaluated")
  ) {
    return {
      color: theme.palette.info.main,
      bgcolor: alpha(theme.palette.info.main, 0.1),
      icon: <Info sx={{ fontSize: "inherit" }} />,
    };
  }

  if (statusLower.includes("inactive") || statusLower.includes("disabled")) {
    return {
      color: theme.palette.grey[600],
      bgcolor: alpha(theme.palette.grey[600], 0.1),
      icon: <Schedule sx={{ fontSize: "inherit" }} />,
    };
  }

  // Default
  return {
    color: theme.palette.primary.main,
    bgcolor: alpha(theme.palette.primary.main, 0.1),
    icon: <Info sx={{ fontSize: "inherit" }} />,
  };
};

export const StatusChip: React.FC<StatusChipProps> = ({
  status,
  variant = "filled",
  size = "small",
  showIcon = true,
}) => {
  const theme = useTheme();
  const config = getStatusConfig(status, theme);

  return (
    <Chip
      label={status}
      size={size}
      icon={showIcon ? config.icon : undefined}
      sx={{
        fontWeight: 600,
        fontSize: size === "small" ? "0.75rem" : "0.875rem",
        height: size === "small" ? 24 : 32,
        ...(variant === "filled" && {
          bgcolor: config.bgcolor,
          color: config.color,
          border: `1px solid ${alpha(config.color, 0.3)}`,
          "& .MuiChip-icon": {
            color: config.color,
          },
        }),
        ...(variant === "outlined" && {
          bgcolor: "transparent",
          color: config.color,
          border: `1px solid ${config.color}`,
          "& .MuiChip-icon": {
            color: config.color,
          },
        }),
        transition: "all 0.2s ease",
        "&:hover": {
          transform: "scale(1.05)",
          boxShadow: `0 2px 8px ${alpha(config.color, 0.3)}`,
        },
      }}
    />
  );
};
