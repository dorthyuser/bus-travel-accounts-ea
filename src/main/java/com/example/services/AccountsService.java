package com.example.services;

import io.micronaut.http.HttpHeaders;
import io.micronaut.http.HttpRequest;
import io.micronaut.http.HttpResponse;
import io.micronaut.http.MediaType;
import io.micronaut.http.client.HttpClient;
import io.micronaut.http.client.annotation.Client;
import jakarta.inject.Inject;
import jakarta.inject.Singleton;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

@Singleton
public class AccountsService {
    private static final Logger LOG = LoggerFactory.getLogger(AccountsService.class);

    @Inject
    @Client("${https.requester.sa.accounts.base-url}")
    HttpClient client;

    // Call GET /accounts?email=...
    public HttpResponse<String> getAccounts(String email, String customerCorrelationId, String xCorrelationId, String clientId) {
        String uri = "/accounts" + (email != null ? "?email=" + encode(email) : "");
        HttpRequest<?> req = HttpRequest.GET(uri)
                .accept(MediaType.APPLICATION_JSON)
                .header("CUSTOMER_CORRELATION_ID", customerCorrelationId)
                .header("X_CORRELATION_ID", xCorrelationId);
        if (clientId != null) req = req.header("client_id", clientId);
        // client_secret expected to be provided by configuration; we avoid leaking it here but include header if configured
        String clientSecret = System.getProperty("https.requester.sa.accounts.client_secret");
        if (clientSecret == null) clientSecret = System.getenv("SA_CLIENT_SECRET");
        if (clientSecret != null) req = req.header("client_secret", clientSecret);

        LOG.debug("Calling backend GET {}", uri);
        return client.toBlocking().exchange(req, String.class);
    }

    public HttpResponse<String> getAccountDetails(String id, String customerCorrelationId, String xCorrelationId) {
        String uri = "/accounts/" + encode(id);
        HttpRequest<?> req = HttpRequest.GET(uri)
                .accept(MediaType.APPLICATION_JSON)
                .header("CUSTOMER_CORRELATION_ID", customerCorrelationId)
                .header("X_CORRELATION_ID", xCorrelationId);

        String clientSecret = System.getProperty("https.requester.sa.accounts.client_secret");
        if (clientSecret == null) clientSecret = System.getenv("SA_CLIENT_SECRET");
        if (clientSecret != null) req = req.header("client_secret", clientSecret);

        LOG.debug("Calling backend GET {}", uri);
        return client.toBlocking().exchange(req, String.class);
    }

    public HttpResponse<String> createAccount(Map<String, Object> body, String customerCorrelationId, String xCorrelationId) {
        String uri = "/accounts";
        HttpRequest<Map<String, Object>> req = HttpRequest.POST(uri, body)
                .contentType(MediaType.APPLICATION_JSON)
                .accept(MediaType.APPLICATION_JSON)
                .header("CUSTOMER_CORRELATION_ID", customerCorrelationId)
                .header("X_CORRELATION_ID", xCorrelationId);
        String clientSecret = System.getProperty("https.requester.sa.accounts.client_secret");
        if (clientSecret == null) clientSecret = System.getenv("SA_CLIENT_SECRET");
        if (clientSecret != null) req = req.header("client_secret", clientSecret);

        LOG.debug("Calling backend POST {}", uri);
        return client.toBlocking().exchange(req, String.class);
    }

    public HttpResponse<String> updateAccount(String id, Map<String, Object> body, String customerCorrelationId, String xCorrelationId) {
        String uri = "/accounts/" + encode(id);
        HttpRequest<Map<String, Object>> req = HttpRequest.PUT(uri, body)
                .contentType(MediaType.APPLICATION_JSON)
                .accept(MediaType.APPLICATION_JSON)
                .header("CUSTOMER_CORRELATION_ID", customerCorrelationId)
                .header("X_CORRELATION_ID", xCorrelationId);
        String clientSecret = System.getProperty("https.requester.sa.accounts.client_secret");
        if (clientSecret == null) clientSecret = System.getenv("SA_CLIENT_SECRET");
        if (clientSecret != null) req = req.header("client_secret", clientSecret);

        LOG.debug("Calling backend PUT {}", uri);
        return client.toBlocking().exchange(req, String.class);
    }

    private String encode(String s) {
        try {
            return java.net.URLEncoder.encode(s, java.nio.charset.StandardCharsets.UTF_8);
        } catch (Exception e) {
            return s;
        }
    }
}
