package com.example.models;

import java.time.OffsetDateTime;

public class ErrorResponse {
    public final ErrorBody error;

    public ErrorResponse(int errorCode, String errorMessage, Object errorDescription) {
        this.error = new ErrorBody(errorCode, OffsetDateTime.now().toString(), errorMessage, errorDescription);
    }

    public static class ErrorBody {
        public int errorCode;
        public String errorDateTime;
        public String errorMessage;
        public Object errorDescription;

        public ErrorBody(int errorCode, String errorDateTime, String errorMessage, Object errorDescription) {
            this.errorCode = errorCode;
            this.errorDateTime = errorDateTime;
            this.errorMessage = errorMessage;
            this.errorDescription = errorDescription;
        }
    }
}
