import React, { useState, useRef } from "react";
import {
  Box,
  Button,
  Typography,
  LinearProgress,
  Alert,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  IconButton,
  Paper,
} from "@mui/material";
import {
  CloudUpload as UploadIcon,
  AttachFile as FileIcon,
  Delete as DeleteIcon,
} from "@mui/icons-material";
import { DocumentUploadRequest } from "@/types/application";
import { applicationService } from "@/services/applicationService";
import { useErrorHandler } from "@/hooks/useErrorHandler";

interface DocumentUploadProps {
  applicationId: number;
  onUploadComplete?: () => void;
}

interface UploadingFile {
  file: File;
  progress: number;
  error?: string;
}

export const DocumentUpload: React.FC<DocumentUploadProps> = ({
  applicationId,
  onUploadComplete,
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { error, handleError, clearError } = useErrorHandler();
  const [uploadingFiles, setUploadingFiles] = useState<UploadingFile[]>([]);

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files || []);
    if (files.length === 0) return;

    // Add files to uploading list
    const newUploadingFiles = files.map((file) => ({
      file,
      progress: 0,
    }));

    setUploadingFiles((prev) => [...prev, ...newUploadingFiles]);

    // Upload each file
    files.forEach((file, index) => {
      uploadFile(file, uploadingFiles.length + index);
    });

    // Clear the input
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const uploadFile = async (file: File, index: number) => {
    try {
      clearError();

      // Update progress to show upload starting
      setUploadingFiles((prev) =>
        prev.map((item, i) => (i === index ? { ...item, progress: 10 } : item))
      );

      const request: DocumentUploadRequest = { file };

      // Simulate progress updates (in a real implementation, you'd use XMLHttpRequest for progress)
      const progressInterval = setInterval(() => {
        setUploadingFiles((prev) =>
          prev.map((item, i) =>
            i === index && item.progress < 90
              ? { ...item, progress: item.progress + 10 }
              : item
          )
        );
      }, 200);

      await applicationService.uploadDocument(applicationId, request);

      clearInterval(progressInterval);

      // Complete the upload
      setUploadingFiles((prev) =>
        prev.map((item, i) => (i === index ? { ...item, progress: 100 } : item))
      );

      // Remove from uploading list after a delay
      setTimeout(() => {
        setUploadingFiles((prev) => prev.filter((_, i) => i !== index));
        if (onUploadComplete) {
          onUploadComplete();
        }
      }, 1000);
    } catch (err) {
      handleError(err);

      // Mark file as error
      setUploadingFiles((prev) =>
        prev.map((item, i) =>
          i === index ? { ...item, error: "Upload failed", progress: 0 } : item
        )
      );
    }
  };

  const handleRemoveFile = (index: number) => {
    setUploadingFiles((prev) => prev.filter((_, i) => i !== index));
  };

  const handleUploadClick = () => {
    fileInputRef.current?.click();
  };

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return "0 Bytes";
    const k = 1024;
    const sizes = ["Bytes", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
  };

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Upload Documents
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Paper
        sx={{
          p: 3,
          border: "2px dashed",
          borderColor: "grey.300",
          textAlign: "center",
          cursor: "pointer",
          "&:hover": {
            borderColor: "primary.main",
            bgcolor: "action.hover",
          },
        }}
        onClick={handleUploadClick}
      >
        <UploadIcon sx={{ fontSize: 48, color: "grey.400", mb: 1 }} />
        <Typography variant="h6" gutterBottom>
          Click to upload documents
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Supported formats: PDF, DOC, DOCX, JPG, PNG (Max 10MB per file)
        </Typography>
        <Button variant="contained" sx={{ mt: 2 }} startIcon={<UploadIcon />}>
          Choose Files
        </Button>
      </Paper>

      <input
        ref={fileInputRef}
        type="file"
        multiple
        accept=".pdf,.doc,.docx,.jpg,.jpeg,.png"
        style={{ display: "none" }}
        onChange={handleFileSelect}
      />

      {uploadingFiles.length > 0 && (
        <Box sx={{ mt: 3 }}>
          <Typography variant="subtitle1" gutterBottom>
            Uploading Files
          </Typography>
          <List>
            {uploadingFiles.map((item, index) => (
              <ListItem
                key={index}
                secondaryAction={
                  <IconButton
                    edge="end"
                    onClick={() => handleRemoveFile(index)}
                    disabled={item.progress > 0 && item.progress < 100}
                  >
                    <DeleteIcon />
                  </IconButton>
                }
              >
                <ListItemIcon>
                  <FileIcon />
                </ListItemIcon>
                <ListItemText
                  primary={item.file.name}
                  secondary={
                    <Box>
                      <Typography variant="body2">
                        {formatFileSize(item.file.size)}
                      </Typography>
                      {item.error ? (
                        <Typography variant="body2" color="error">
                          {item.error}
                        </Typography>
                      ) : (
                        <Box sx={{ mt: 1 }}>
                          <LinearProgress
                            variant="determinate"
                            value={item.progress}
                            sx={{ height: 6, borderRadius: 3 }}
                          />
                          <Typography variant="body2" sx={{ mt: 0.5 }}>
                            {item.progress}%
                          </Typography>
                        </Box>
                      )}
                    </Box>
                  }
                />
              </ListItem>
            ))}
          </List>
        </Box>
      )}
    </Box>
  );
};
