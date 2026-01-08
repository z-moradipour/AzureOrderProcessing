using System;

namespace OrderApi.Functions.Models;

public sealed record CreateOrderRequest(
    string OrderId,
    string CustomerId,
    decimal Amount
);
