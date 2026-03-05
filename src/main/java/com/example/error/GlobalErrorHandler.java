package com.example.error;

import io.micronaut.http.HttpRequest;
import io.micronaut.http.HttpResponse;
import io.micronaut.http.annotation.Error;
import io.micronaut.http.server.exceptions.ExceptionHandler;
import jakarta.inject.Singleton;

@Singleton
public class GlobalErrorHandler implements ExceptionHandler<Exception, HttpResponse<?>> {

    @Override
    @Error(global = true)
    public HttpResponse<?> handle(HttpRequest request, Exception exception) {
        return HttpResponse.serverError().body(exception.getMessage());
    }
}