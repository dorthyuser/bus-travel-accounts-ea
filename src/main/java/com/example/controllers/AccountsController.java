package com.example.controllers;

import com.example.services.AccountsService;
import com.example.models.ErrorResponse;
import io.micronaut.http.HttpHeaders;
import io.micronaut.http.HttpRequest;
import io.micronaut.http.HttpResponse;
import io.micronaut.http.MediaType;
import io.micronaut.http.annotation.*;
import jakarta.inject.Inject;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;
import java.util.Optional;
import java.util.UUID;

@Controller("/accounts")
public class AccountsController {
    private static final Logger LOG = LoggerFactory.getLogger(AccountsController.class);

    @Inject
    AccountsService accountsService;

    // GET /accounts?email=...
    @Get(produces = MediaType.APPLICATION_JSON)
    public HttpResponse<?> getAccounts(@QueryValue Optional<String> email, @Header("CUSTOMER_CORRELATION_ID") Optional<String> customerCorrelationId, @Header("client_id") Optional<String> clientId, HttpRequest<?> request) {
        String corrId = customerCorrelationId.map(String::trim).orElse("CUSTOMER_CORRELATION_ID_NOT_FOUND");
        String xCorr = request.getHeaders().get("X-CORRELATION-ID");
        if (xCorr == null) xCorr = UUID.randomUUID().toString();

        LOG.info("START - Request received for GET /accounts, X_CORRELATION_ID={} CUSTOMER_CORRELATION_ID={}", xCorr, corrId);

        try {
            HttpResponse<String> resp = accountsService.getAccounts(email.orElse(null), corrId, xCorr, clientId.orElse(null));
            LOG.debug("AFTER REQUEST - status={}", resp.getStatus().getCode());
            if (resp.getStatus().getCode() == 200) {
                return HttpResponse.ok().body(resp.getBody().orElse("{}"));
            } else {
                return HttpResponse.status(resp.getStatus()).body(resp.getBody().orElse("{}"));
            }
        } catch (Exception e) {
            LOG.error("Error calling backend for GET /accounts", e);
            ErrorResponse error = new ErrorResponse(500, "INTERNAL SERVER ERROR", e.getMessage());
            return HttpResponse.status(500).body(error);
        }
    }

    // GET /accounts/{id}
    @Get(uri = "/{id}", produces = MediaType.APPLICATION_JSON)
    public HttpResponse<?> getAccountById(@PathVariable String id, @Header("CUSTOMER_CORRELATION_ID") Optional<String> customerCorrelationId, HttpRequest<?> request) {
        String corrId = customerCorrelationId.map(String::trim).orElse("CUSTOMER_CORRELATION_ID_NOT_FOUND");
        String xCorr = request.getHeaders().get("X-CORRELATION-ID");
        if (xCorr == null) xCorr = UUID.randomUUID().toString();

        LOG.info("START - Request received for GET /accounts/{} X_CORRELATION_ID={}", id, xCorr);
        try {
            HttpResponse<String> resp = accountsService.getAccountDetails(id, corrId, xCorr);
            LOG.debug("AFTER REQUEST - status={}", resp.getStatus().getCode());
            if (resp.getStatus().getCode() == 200) {
                return HttpResponse.ok().body(resp.getBody().orElse("{}"));
            } else {
                return HttpResponse.status(resp.getStatus()).body(resp.getBody().orElse("{}"));
            }
        } catch (Exception e) {
            LOG.error("Error calling backend for GET /accounts/{}", id, e);
            ErrorResponse error = new ErrorResponse(500, "INTERNAL SERVER ERROR", e.getMessage());
            return HttpResponse.status(500).body(error);
        }
    }

    // POST /accounts
    @Post(consumes = MediaType.APPLICATION_JSON, produces = MediaType.APPLICATION_JSON)
    public HttpResponse<?> createAccount(@Body Map<String, Object> body, @Header("CUSTOMER_CORRELATION_ID") Optional<String> customerCorrelationId, HttpRequest<?> request) {
        String corrId = customerCorrelationId.map(String::trim).orElse("CUSTOMER_CORRELATION_ID_NOT_FOUND");
        String xCorr = request.getHeaders().get("X-CORRELATION-ID");
        if (xCorr == null) xCorr = UUID.randomUUID().toString();

        LOG.info("START - Request received for POST /accounts X_CORRELATION_ID={}", xCorr);
        try {
            HttpResponse<String> resp = accountsService.createAccount(body, corrId, xCorr);
            LOG.debug("AFTER REQUEST - status={}", resp.getStatus().getCode());
            if (resp.getStatus().getCode() == 201) {
                return HttpResponse.status(201).body(resp.getBody().orElse("{}"));
            } else {
                return HttpResponse.status(resp.getStatus()).body(resp.getBody().orElse("{}"));
            }
        } catch (Exception e) {
            LOG.error("Error calling backend for POST /accounts", e);
            ErrorResponse error = new ErrorResponse(500, "INTERNAL SERVER ERROR", e.getMessage());
            return HttpResponse.status(500).body(error);
        }
    }

    // PUT /accounts/{id}
    @Put(uri = "/{id}", consumes = MediaType.APPLICATION_JSON, produces = MediaType.APPLICATION_JSON)
    public HttpResponse<?> updateAccount(@PathVariable String id, @Body Map<String, Object> body, @Header("CUSTOMER_CORRELATION_ID") Optional<String> customerCorrelationId, HttpRequest<?> request) {
        String corrId = customerCorrelationId.map(String::trim).orElse("CUSTOMER_CORRELATION_ID_NOT_FOUND");
        String xCorr = request.getHeaders().get("X-CORRELATION-ID");
        if (xCorr == null) xCorr = UUID.randomUUID().toString();

        LOG.info("START - Request received for PUT /accounts/{} X_CORRELATION_ID={}", id, xCorr);
        try {
            HttpResponse<String> resp = accountsService.updateAccount(id, body, corrId, xCorr);
            LOG.debug("AFTER REQUEST - status={}", resp.getStatus().getCode());
            if (resp.getStatus().getCode() == 200) {
                return HttpResponse.ok().body(resp.getBody().orElse("{}"));
            } else {
                return HttpResponse.status(resp.getStatus()).body(resp.getBody().orElse("{}"));
            }
        } catch (Exception e) {
            LOG.error("Error calling backend for PUT /accounts/{}", id, e);
            ErrorResponse error = new ErrorResponse(500, "INTERNAL SERVER ERROR", e.getMessage());
            return HttpResponse.status(500).body(error);
        }
    }
}
