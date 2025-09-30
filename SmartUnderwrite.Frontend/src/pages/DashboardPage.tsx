import React from "react";
import {
  Box,
  Typography,
  Card,
  CardContent,
  CardActions,
  Button,
} from "@mui/material";
import { useAuth } from "@/contexts/AuthContext";
import { UserRole } from "@/types/auth";

export const DashboardPage: React.FC = () => {
  const { user, hasRole } = useAuth();

  const getDashboardCards = () => {
    const cards = [
      {
        title: "Applications",
        description: "View and manage loan applications",
        action: "View Applications",
        path: "/applications",
        show: true,
      },
    ];

    if (hasRole(UserRole.Underwriter) || hasRole(UserRole.Admin)) {
      cards.push({
        title: "Decisions",
        description: "Review and make manual decisions",
        action: "View Decisions",
        path: "/decisions",
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
          show: true,
        },
        {
          title: "Affiliates",
          description: "Manage affiliate organizations",
          action: "Manage Affiliates",
          path: "/admin/affiliates",
          show: true,
        },
        {
          title: "Audit Logs",
          description: "View system audit trail",
          action: "View Logs",
          path: "/admin/audit",
          show: true,
        }
      );
    }

    return cards.filter((card) => card.show);
  };

  const cards = getDashboardCards();

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Welcome, {user?.firstName}!
      </Typography>
      <Typography variant="body1" color="text.secondary" paragraph>
        Role: {user?.roles?.join(", ")}
        {user?.affiliateId && ` | Affiliate ID: ${user.affiliateId}`}
      </Typography>

      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3, mt: 2 }}>
        {cards.map((card) => (
          <Box key={card.title} sx={{ minWidth: 300, flex: "1 1 300px" }}>
            <Card>
              <CardContent>
                <Typography variant="h6" component="h2" gutterBottom>
                  {card.title}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {card.description}
                </Typography>
              </CardContent>
              <CardActions>
                <Button size="small" href={card.path}>
                  {card.action}
                </Button>
              </CardActions>
            </Card>
          </Box>
        ))}
      </Box>
    </Box>
  );
};
