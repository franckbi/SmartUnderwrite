import React from "react";
import {
  Box,
  CircularProgress,
  Typography,
  Fade,
  useTheme,
  alpha,
  keyframes,
} from "@mui/material";

interface LoadingSpinnerProps {
  message?: string;
  size?: number;
  variant?: "default" | "overlay" | "inline";
}

const pulseAnimation = keyframes`
  0% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(1.05);
    opacity: 0.7;
  }
  100% {
    transform: scale(1);
    opacity: 1;
  }
`;

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  message = "Loading...",
  size = 40,
  variant = "default",
}) => {
  const theme = useTheme();

  if (variant === "overlay") {
    return (
      <Fade in timeout={300}>
        <Box
          sx={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
            bgcolor: alpha(theme.palette.background.default, 0.8),
            backdropFilter: "blur(4px)",
            zIndex: theme.zIndex.modal,
            gap: 3,
          }}
        >
          <Box
            sx={{
              animation: `${pulseAnimation} 2s ease-in-out infinite`,
            }}
          >
            <CircularProgress
              size={size}
              thickness={4}
              sx={{
                color: theme.palette.primary.main,
              }}
            />
          </Box>
          {message && (
            <Typography
              variant="h6"
              color="text.primary"
              sx={{
                fontWeight: 500,
                textAlign: "center",
                maxWidth: 300,
              }}
            >
              {message}
            </Typography>
          )}
        </Box>
      </Fade>
    );
  }

  if (variant === "inline") {
    return (
      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          gap: 2,
          py: 1,
        }}
      >
        <CircularProgress size={20} thickness={4} />
        {message && (
          <Typography variant="body2" color="text.secondary">
            {message}
          </Typography>
        )}
      </Box>
    );
  }

  return (
    <Fade in timeout={300}>
      <Box
        sx={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          minHeight: "200px",
          gap: 3,
          p: 4,
        }}
      >
        <Box
          sx={{
            position: "relative",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <CircularProgress
            size={size}
            thickness={4}
            sx={{
              color: theme.palette.primary.main,
              animation: `${pulseAnimation} 2s ease-in-out infinite`,
            }}
          />
          <Box
            sx={{
              position: "absolute",
              width: size * 0.6,
              height: size * 0.6,
              borderRadius: "50%",
              bgcolor: alpha(theme.palette.primary.main, 0.1),
              animation: `${pulseAnimation} 2s ease-in-out infinite reverse`,
            }}
          />
        </Box>
        {message && (
          <Typography
            variant="body1"
            color="text.secondary"
            sx={{
              fontWeight: 500,
              textAlign: "center",
              maxWidth: 300,
            }}
          >
            {message}
          </Typography>
        )}
      </Box>
    </Fade>
  );
};
