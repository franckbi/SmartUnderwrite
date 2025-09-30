import React, { useState } from "react";
import { Box, Tabs, Tab, Typography } from "@mui/material";
import {
  Settings as SettingsIcon,
  Assessment as ReportsIcon,
  History as AuditIcon,
} from "@mui/icons-material";
import { RulesManagement } from "@/components/admin/RulesManagement";
import { AuditLogs } from "@/components/admin/AuditLogs";
import { Reports } from "@/components/admin/Reports";

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({
  children,
  value,
  index,
  ...other
}) => {
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`admin-tabpanel-${index}`}
      aria-labelledby={`admin-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
    </div>
  );
};

const a11yProps = (index: number) => {
  return {
    id: `admin-tab-${index}`,
    "aria-controls": `admin-tabpanel-${index}`,
  };
};

export const AdminPage: React.FC = () => {
  const [tabValue, setTabValue] = useState(0);

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Administration
      </Typography>

      <Box sx={{ borderBottom: 1, borderColor: "divider" }}>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          aria-label="admin tabs"
        >
          <Tab
            icon={<SettingsIcon />}
            label="Rules Management"
            {...a11yProps(0)}
          />
          <Tab icon={<ReportsIcon />} label="Reports" {...a11yProps(1)} />
          <Tab icon={<AuditIcon />} label="Audit Logs" {...a11yProps(2)} />
        </Tabs>
      </Box>

      <TabPanel value={tabValue} index={0}>
        <RulesManagement />
      </TabPanel>
      <TabPanel value={tabValue} index={1}>
        <Reports />
      </TabPanel>
      <TabPanel value={tabValue} index={2}>
        <AuditLogs />
      </TabPanel>
    </Box>
  );
};
