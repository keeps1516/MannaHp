using System.Net.Http.Json;
using MannaHp.Shared.DTOs;

namespace MannaHp.Client.Services;

public class MenuService
{
    private readonly HttpClient _http;

    private List<CategoryDto>? _categories;
    private List<MenuItemDto>? _menuItems;

    public MenuService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        _categories ??= await _http.GetFromJsonAsync<List<CategoryDto>>("api/categories") ?? [];
        return _categories;
    }

    public async Task<List<MenuItemDto>> GetMenuItemsAsync()
    {
        _menuItems ??= await _http.GetFromJsonAsync<List<MenuItemDto>>("api/menu-items") ?? [];
        return _menuItems;
    }

    public void InvalidateCache()
    {
        _categories = null;
        _menuItems = null;
    }
}
