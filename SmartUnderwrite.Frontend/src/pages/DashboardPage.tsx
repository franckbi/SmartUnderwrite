import React from "react";
import {
  Box,
  Typography,
  Card,
  CardContent,
  CardActions,
  Button,
  Avatar,
  Chip,
  Paper,
  useTheme,
  alpha,
  Fade,
  Grow,
} from "@mui/material";
import {
  Assignment,
  Gavel,
  Settings,
  Business,
  History,
  TrendingUp,
  ArrowForward,
} from "@mui/icons-material";
import { useAuth } from "@/contexts/AuthContext";
import { UserRole } from "@/types/auth";

export const DashboardPage: React.FC = () => {
  const { user, hasRole } = useAuth();
  const theme = useTheme();

  const getDashboardCards = () => {
    const cards = [
      {
        title: "Applications",
        description: "View and manage loan applications",
        action: "View Applications",
        path: "/applications",
        icon: Assignment,
        color: theme.palette.primary.main,
        show: true,
      },
    ];

    if (hasRole(UserRole.Underwriter) || hasRole(UserRole.Admin)) {
      cards.push({
        title: "Decisions",
        description: "Review and make manual decisions",
        action: "View Decisions",
        path: "/decisions",
        icon: Gavel,
        color: theme.palette.secondary.main,
        show: true,
      });
    }

    if (hasRole(UserRole.Admin)) {
      cards.push(
        {
          title: "Rules Management",
          description: "Configure underwriting rules",
          action: "Manage Rules",
          path: "/admin/rules",
          icon: Settings,
          color: theme.palette.warning.main,
          show: true,
        },
        {
          title: "Affiliates",
          description: "Manage affiliate organizations",
          action: "Manage Affiliates",
          path: "/admin/affiliates",
          icon: Business,
          color: theme.palette.info.main,
          show: true,
        },
        {
          title: "Audit Logs",
          description: "View system audit trail",
          action: "View Logs",
          path: "/admin/audit",
          icon: History,
          color: theme.palette.success.main,
          show: true,
        }
      );
    }

    return cards.filter((card) => card.show);
  };

  const cards = getDashboardCards();

  return (
    <Box>
      {/* Welcome Header */}
      <Fade in timeout={800}>
        <Paper
          elevation={0}
          sx={{
            background: `linear-gradient(135deg, ${alpha(
              theme.palette.primary.main,
              0.1
            )} 0%, ${alpha(theme.palette.secondary.main, 0.1)} 100%)`,
            p: 4,
            mb: 4,
            borderRadius: 3,
            border: `1px solid ${alpha(theme.palette.primary.main, 0.1)}`,
          }}
        >
          <Box sx={{ display: "flex", alignItems: "center", gap: 3 }}>
            <Avatar
              sx={{
                width: 80,
                height: 80,
                bgcolor: theme.palette.primary.main,
                fontSize: "2rem",
                fontWeight: "bold",
              }}
            >
              {user?.firstName?.charAt(0)}
              {user?.lastName?.charAt(0)}
            </Avatar>
            <Box>
              <Typography
                variant="h3"
                component="h1"
                gutterBottom
                sx={{ fontWeight: 600 }}
              >
                Welcome back, {user?.firstName}!
              </Typography>
              <Box sx={{ display: "flex", gap: 1, flexWrap: "wrap" }}>
                {user?.roles?.map((role) => (
                  <Chip
                    key={role}
                    label={role}
                    color="primary"
                    variant="outlined"
                    size="small"
                  />
                ))}
                {user?.affiliateId && (
                  <Chip
                    label={`Affiliate ID: ${user.affiliateId}`}
                    color="secondary"
                    variant="outlined"
                    size="small"
                  />
                )}
              </Box>
            </Box>
          </Box>
        </Paper>
      </Fade>

      {/* Quick Actions */}
      <Typography
        variant="h5"
        component="h2"
        gutterBottom
        sx={{ fontWeight: 600, mb: 3 }}
      >
        Quick Actions
      </Typography>

      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "1fr",
            sm: "repeat(2, 1fr)",
            md: "repeat(3, 1fr)",
          },
          gap: 3,
        }}
      >
        {cards.map((card, index) => {
          const IconComponent = card.icon;
          return (
            <Grow in timeout={600 + index * 200}>
              <Card
                sx={{
                  height: "100%",
                  transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
                  cursor: "pointer",
                  "&:hover": {
                    transform: "translateY(-8px)",
                    boxShadow: theme.shadows[8],
                    "& .card-icon": {
                      transform: "scale(1.1)",
                    },
                    "& .card-arrow": {
                      transform: "translateX(4px)",
                    },
                  },
                }}
                onClick={() => (window.location.href = card.path)}
              >
                <CardContent sx={{ pb: 1 }}>
                  <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
                    <Avatar
                      className="card-icon"
                      sx={{
                        bgcolor: alpha(card.color, 0.1),
                        color: card.color,
                        width: 56,
                        height: 56,
                        transition: "transform 0.3s ease",
                      }}
                    >
                      <IconComponent fontSize="large" />
                    </Avatar>
                  </Box>
                  <Typography
                    variant="h6"
                    component="h3"
                    gutterBottom
                    sx={{ fontWeight: 600 }}
                  >
                    {card.title}
                  </Typography>
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{ mb: 2 }}
                  >
                    {card.description}
                  </Typography>
                </CardContent>
                <CardActions sx={{ pt: 0 }}>
                  <Button
                    size="small"
                    endIcon={
                      <ArrowForward
                        className="card-arrow"
                        sx={{ transition: "transform 0.3s ease" }}
                      />
                    }
                    sx={{ fontWeight: 600 }}
                  >
                    {card.action}
                  </Button>
                </CardActions>
              </Card>
            </Grow>
          );
        })}
      </Box>
    </Box>
  );
};
