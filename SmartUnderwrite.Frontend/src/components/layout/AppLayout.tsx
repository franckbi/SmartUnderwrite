import React, { useState } from "react";
import {
  AppBar,
  Box,
  CssBaseline,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  Menu,
  MenuItem,
  Avatar,
  Divider,
  Chip,
  useTheme,
  alpha,
  Fade,
  Slide,
} from "@mui/material";
import {
  Menu as MenuIcon,
  Dashboard,
  Assignment,
  Gavel,
  People,
  Settings,
  AccountCircle,
  Logout,
  AdminPanelSettings,
  Business,
  History,
} from "@mui/icons-material";
import { useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "@/contexts/AuthContext";
import { UserRole } from "@/types/auth";

const drawerWidth = 240;

interface AppLayoutProps {
  children: React.ReactNode;
}

interface NavigationItem {
  text: string;
  icon: React.ReactElement;
  path: string;
  roles?: string[];
}

const navigationItems: NavigationItem[] = [
  {
    text: "Dashboard",
    icon: <Dashboard />,
    path: "/dashboard",
  },
  {
    text: "Applications",
    icon: <Assignment />,
    path: "/applications",
  },
  {
    text: "Decisions",
    icon: <Gavel />,
    path: "/decisions",
    roles: [UserRole.Underwriter, UserRole.Admin],
  },
  {
    text: "Rules",
    icon: <Settings />,
    path: "/admin/rules",
    roles: [UserRole.Admin],
  },
  {
    text: "Affiliates",
    icon: <Business />,
    path: "/admin/affiliates",
    roles: [UserRole.Admin],
  },
  {
    text: "Audit Logs",
    icon: <History />,
    path: "/admin/audit",
    roles: [UserRole.Admin],
  },
];

export const AppLayout: React.FC<AppLayoutProps> = ({ children }) => {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const { user, logout, hasAnyRole } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const theme = useTheme();

  const handleDrawerToggle = () => {
    setMobileOpen(!mobileOpen);
  };

  const handleProfileMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleProfileMenuClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = async () => {
    handleProfileMenuClose();
    await logout();
    navigate("/login");
  };

  const handleNavigation = (path: string) => {
    navigate(path);
    setMobileOpen(false);
  };

  const filteredNavigationItems = navigationItems.filter((item) => {
    if (!item.roles) return true;
    return hasAnyRole(item.roles);
  });

  const drawer = (
    <Box sx={{ height: "100%", display: "flex", flexDirection: "column" }}>
      <Toolbar
        sx={{
          background: `linear-gradient(135deg, ${theme.palette.primary.main} 0%, ${theme.palette.primary.dark} 100%)`,
          color: "white",
          minHeight: 80,
        }}
      >
        <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
          <Avatar
            sx={{
              bgcolor: alpha(theme.palette.common.white, 0.2),
              color: "white",
              fontWeight: "bold",
            }}
          >
            SU
          </Avatar>
          <Box>
            <Typography
              variant="h6"
              noWrap
              component="div"
              sx={{ fontWeight: 600 }}
            >
              SmartUnderwrite
            </Typography>
            <Typography variant="caption" sx={{ opacity: 0.8 }}>
              Loan Processing Platform
            </Typography>
          </Box>
        </Box>
      </Toolbar>

      <Box sx={{ flex: 1, overflow: "auto" }}>
        <List sx={{ pt: 2 }}>
          {filteredNavigationItems.map((item, index) => (
            <Fade in timeout={300 + index * 100} key={item.text}>
              <ListItem disablePadding sx={{ mb: 0.5, px: 2 }}>
                <ListItemButton
                  selected={location.pathname === item.path}
                  onClick={() => handleNavigation(item.path)}
                  sx={{
                    borderRadius: 2,
                    transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
                    "&.Mui-selected": {
                      bgcolor: alpha(theme.palette.primary.main, 0.1),
                      color: theme.palette.primary.main,
                      "&:hover": {
                        bgcolor: alpha(theme.palette.primary.main, 0.15),
                      },
                      "& .MuiListItemIcon-root": {
                        color: theme.palette.primary.main,
                      },
                    },
                    "&:hover": {
                      bgcolor: alpha(theme.palette.action.hover, 0.5),
                      transform: "translateX(4px)",
                    },
                  }}
                >
                  <ListItemIcon
                    sx={{
                      minWidth: 40,
                      transition: "transform 0.2s ease",
                      ".Mui-selected &": {
                        transform: "scale(1.1)",
                      },
                    }}
                  >
                    {item.icon}
                  </ListItemIcon>
                  <ListItemText
                    primary={item.text}
                    primaryTypographyProps={{
                      fontWeight: location.pathname === item.path ? 600 : 400,
                    }}
                  />
                </ListItemButton>
              </ListItem>
            </Fade>
          ))}
        </List>
      </Box>

      {/* User Info at Bottom */}
      <Box
        sx={{
          p: 2,
          borderTop: `1px solid ${alpha(theme.palette.divider, 0.1)}`,
        }}
      >
        <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 1 }}>
          <Avatar
            sx={{
              width: 32,
              height: 32,
              bgcolor: theme.palette.secondary.main,
              fontSize: "0.875rem",
            }}
          >
            {user?.firstName?.charAt(0)}
            {user?.lastName?.charAt(0)}
          </Avatar>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography variant="body2" sx={{ fontWeight: 500 }} noWrap>
              {user?.firstName} {user?.lastName}
            </Typography>
            <Typography variant="caption" color="text.secondary" noWrap>
              {user?.roles?.join(", ")}
            </Typography>
          </Box>
        </Box>
        {user?.affiliateId && (
          <Chip
            label={`Affiliate: ${user.affiliateId}`}
            size="small"
            variant="outlined"
            sx={{ fontSize: "0.75rem" }}
          />
        )}
      </Box>
    </Box>
  );

  return (
    <Box sx={{ display: "flex" }}>
      <CssBaseline />
      <AppBar
        position="fixed"
        elevation={0}
        sx={{
          width: { sm: `calc(100% - ${drawerWidth}px)` },
          ml: { sm: `${drawerWidth}px` },
          bgcolor: alpha(theme.palette.background.paper, 0.8),
          backdropFilter: "blur(20px)",
          borderBottom: `1px solid ${alpha(theme.palette.divider, 0.1)}`,
          color: theme.palette.text.primary,
        }}
      >
        <Toolbar sx={{ minHeight: 70 }}>
          <IconButton
            color="inherit"
            aria-label="open drawer"
            edge="start"
            onClick={handleDrawerToggle}
            sx={{
              mr: 2,
              display: { sm: "none" },
              transition: "transform 0.2s ease",
              "&:hover": {
                transform: "scale(1.1)",
              },
            }}
          >
            <MenuIcon />
          </IconButton>

          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="h5" component="div" sx={{ fontWeight: 600 }}>
              {navigationItems.find((item) => item.path === location.pathname)
                ?.text || "Dashboard"}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {new Date().toLocaleDateString("en-US", {
                weekday: "long",
                year: "numeric",
                month: "long",
                day: "numeric",
              })}
            </Typography>
          </Box>

          <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
            <Box
              sx={{ textAlign: "right", display: { xs: "none", sm: "block" } }}
            >
              <Typography variant="body2" sx={{ fontWeight: 500 }}>
                {user?.firstName} {user?.lastName}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {user?.roles?.join(", ")}
              </Typography>
            </Box>
            <IconButton
              size="large"
              aria-label="account of current user"
              aria-controls="profile-menu"
              aria-haspopup="true"
              onClick={handleProfileMenuOpen}
              sx={{
                transition: "all 0.2s ease",
                "&:hover": {
                  transform: "scale(1.05)",
                  bgcolor: alpha(theme.palette.primary.main, 0.1),
                },
              }}
            >
              <Avatar
                sx={{
                  width: 40,
                  height: 40,
                  bgcolor: theme.palette.primary.main,
                  fontWeight: 600,
                }}
              >
                {user?.firstName?.charAt(0)}
                {user?.lastName?.charAt(0)}
              </Avatar>
            </IconButton>
          </Box>
        </Toolbar>
      </AppBar>
      <Box
        component="nav"
        sx={{ width: { sm: drawerWidth }, flexShrink: { sm: 0 } }}
        aria-label="navigation"
      >
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={handleDrawerToggle}
          ModalProps={{
            keepMounted: true,
          }}
          sx={{
            display: { xs: "block", sm: "none" },
            "& .MuiDrawer-paper": {
              boxSizing: "border-box",
              width: drawerWidth,
            },
          }}
        >
          {drawer}
        </Drawer>
        <Drawer
          variant="permanent"
          sx={{
            display: { xs: "none", sm: "block" },
            "& .MuiDrawer-paper": {
              boxSizing: "border-box",
              width: drawerWidth,
            },
          }}
          open
        >
          {drawer}
        </Drawer>
      </Box>
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 3,
          width: { sm: `calc(100% - ${drawerWidth}px)` },
        }}
      >
        <Toolbar />
        {children}
      </Box>
      <Menu
        id="profile-menu"
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleProfileMenuClose}
        anchorOrigin={{
          vertical: "bottom",
          horizontal: "right",
        }}
        transformOrigin={{
          vertical: "top",
          horizontal: "right",
        }}
      >
        <MenuItem onClick={handleLogout}>
          <ListItemIcon>
            <Logout fontSize="small" />
          </ListItemIcon>
          Logout
        </MenuItem>
      </Menu>
    </Box>
  );
};
