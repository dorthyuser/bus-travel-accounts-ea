package com.example.controller;

import io.micronaut.http.annotation.*;
import io.micronaut.http.HttpStatus;

@Controller("/accounts")
public class AccountsController {

    @Get
    public HttpStatus getAccounts() {
        return HttpStatus.OK;
    }

    @Get("/{id}")
    public HttpStatus getAccountDetails(String id) {
        return HttpStatus.OK;
    }

    @Post
    public HttpStatus createAccount() {
        return HttpStatus.CREATED;
    }

    @Put("/{id}")
    public HttpStatus updateAccount(String id) {
        return HttpStatus.OK;
    }
}