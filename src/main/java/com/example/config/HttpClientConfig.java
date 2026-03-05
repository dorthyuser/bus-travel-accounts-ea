package com.example.config;

import io.micronaut.context.annotation.Factory;
import io.micronaut.http.client.HttpClient;
import io.micronaut.http.client.annotation.Client;
import jakarta.inject.Singleton;

@Factory
public class HttpClientConfig {

    @Singleton
    @Client("${http.client.url}")
    HttpClient httpClient() {
        return HttpClient.create();
    }
}