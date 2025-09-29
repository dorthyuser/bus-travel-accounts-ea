package com.example.controllers;

import io.micronaut.http.HttpResponse;
import io.micronaut.http.annotation.Controller;
import io.micronaut.http.annotation.Get;

@Controller
public class HealthController {

    @Get("/alive")
    public HttpResponse<String> alive() {
        return HttpResponse.ok("UP");
    }

    @Get("/ready")
    public HttpResponse<String> ready() {
        // In Mule flow it references check-all-dependencies-are-alive - here we assume dependencies are OK.
        return HttpResponse.ok("UP");
    }
}
