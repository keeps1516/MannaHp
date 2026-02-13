using System.Net.Http.Json;
using MannaHp.Client.Models;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;

namespace MannaHp.Client.Services;

public class OrderService
{
    private readonly HttpClient _http;

    public OrderService(HttpClient http)
    {
        _http = http;
    }

    public async Task<OrderDto?> PlaceOrderAsync(IReadOnlyList<CartItem> items, PaymentMethod paymentMethod, string? notes = null)
    {
        var request = new CreateOrderRequest(
            PaymentMethod: paymentMethod,
            Notes: notes,
            Items: items.Select(ci => new CreateOrderItemRequest(
                MenuItemId: ci.MenuItem.Id,
                VariantId: ci.Variant?.Id,
                Quantity: ci.Quantity,
                Notes: ci.Notes,
                SelectedIngredientIds: ci.SelectedIngredients?.Select(i => i.Id).ToList()
            )).ToList()
        );

        var response = await _http.PostAsJsonAsync("api/orders", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>();
    }

    public async Task<OrderDto?> GetOrderAsync(Guid orderId)
    {
        return await _http.GetFromJsonAsync<OrderDto>($"api/orders/{orderId}");
    }
}
