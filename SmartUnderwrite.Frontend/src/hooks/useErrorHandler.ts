import { useState, useCallback } from "react";
import { ApiError } from "@/types/api";

interface ErrorState {
  message: string;
  fieldErrors: Record<string, string>;
}

export const useErrorHandler = () => {
  const [error, setError] = useState<ErrorState>({
    message: "",
    fieldErrors: {},
  });

  const handleError = useCallback((err: unknown) => {
    if (err && typeof err === "object" && "message" in err) {
      const apiError = err as ApiError;
      const fieldErrors: Record<string, string> = {};

      if (apiError.errors) {
        Object.entries(apiError.errors).forEach(([field, messages]) => {
          fieldErrors[field.toLowerCase()] = messages[0];
        });
      }

      setError({
        message: apiError.message,
        fieldErrors,
      });
    } else {
      setError({
        message: "An unexpected error occurred",
        fieldErrors: {},
      });
    }
  }, []);

  const clearError = useCallback(() => {
    setError({
      message: "",
      fieldErrors: {},
    });
  }, []);

  const clearFieldError = useCallback((field: string) => {
    setError((prev) => ({
      ...prev,
      fieldErrors: {
        ...prev.fieldErrors,
        [field]: "",
      },
    }));
  }, []);

  return {
    error: error.message,
    fieldErrors: error.fieldErrors,
    handleError,
    clearError,
    clearFieldError,
  };
};
