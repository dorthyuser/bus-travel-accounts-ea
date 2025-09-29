package com.example.handlers;

import com.example.models.ErrorResponse;
import io.micronaut.core.type.Argument;
import io.micronaut.http.HttpRequest;
import io.micronaut.http.HttpResponse;
import io.micronaut.http.annotation.Produces;
import io.micronaut.http.server.exceptions.ExceptionHandler;
import jakarta.inject.Singleton;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

@Produces("application/json")
@Singleton
public class GlobalExceptionHandler implements ExceptionHandler<Throwable, HttpResponse<?>> {
    private static final Logger LOG = LoggerFactory.getLogger(GlobalExceptionHandler.class);

    @Override
    public HttpResponse<?> handle(HttpRequest request, Throwable exception) {
        LOG.error("Unhandled exception while processing request {} {}", request.getMethod(), request.getPath(), exception);
        ErrorResponse err = new ErrorResponse(500, "INTERNAL SERVER ERROR", exception.getMessage());
        return HttpResponse.serverError().body(err);
    }
}
