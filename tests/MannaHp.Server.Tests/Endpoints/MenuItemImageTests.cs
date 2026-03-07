using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class MenuItemImageTests
{
    private readonly MannaApiFactory _factory;

    // Known seed GUIDs
    private static readonly Guid MiLatte = Guid.Parse("c0000000-0009-0000-0000-000000000009");

    public MenuItemImageTests(MannaApiFactory factory)
    {
        _factory = factory;
    }

    private static MultipartFormDataContent CreateImageContent(
        byte[]? fileBytes = null, string fileName = "test.jpg", string contentType = "image/jpeg")
    {
        fileBytes ??= new byte[1024]; // 1 KB dummy file
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    // ── POST /api/menu-items/{id}/image ──

    [Fact]
    public async Task UploadImage_ValidJpeg_Returns200AndUpdatesImageUrl()
    {
        var client = await _factory.CreateOwnerClientAsync();
        var content = CreateImageContent();

        var response = await client.PostAsync($"/api/menu-items/{MiLatte}/image", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = await response.Content.ReadFromJsonAsync<MenuItemDto>();
        item!.ImageUrl.Should().NotBeNullOrEmpty();
        item.ImageUrl.Should().Contain("/uploads/menu/");
    }

    [Fact]
    public async Task UploadImage_RejectsFilesOver5MB()
    {
        var client = await _factory.CreateOwnerClientAsync();
        var largeFile = new byte[6 * 1024 * 1024]; // 6 MB
        var content = CreateImageContent(largeFile);

        var response = await client.PostAsync($"/api/menu-items/{MiLatte}/image", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadImage_RejectsNonImageFileTypes()
    {
        var client = await _factory.CreateOwnerClientAsync();
        var content = CreateImageContent(contentType: "application/pdf", fileName: "test.pdf");

        var response = await client.PostAsync($"/api/menu-items/{MiLatte}/image", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadImage_Returns404ForNonexistentMenuItem()
    {
        var client = await _factory.CreateOwnerClientAsync();
        var content = CreateImageContent();

        var response = await client.PostAsync($"/api/menu-items/{Guid.NewGuid()}/image", content);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadImage_RequiresOwnerAuthorization()
    {
        var anonClient = _factory.CreateClient();
        var content = CreateImageContent();

        var response = await anonClient.PostAsync($"/api/menu-items/{MiLatte}/image", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadImage_StaffCannotUpload()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var content = CreateImageContent();

        var response = await staffClient.PostAsync($"/api/menu-items/{MiLatte}/image", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── DELETE /api/menu-items/{id}/image ──

    [Fact]
    public async Task DeleteImage_ReturnsNoContentAndClearsImageUrl()
    {
        var client = await _factory.CreateOwnerClientAsync();

        // First upload an image
        var content = CreateImageContent();
        await client.PostAsync($"/api/menu-items/{MiLatte}/image", content);

        // Then delete it
        var deleteResponse = await client.DeleteAsync($"/api/menu-items/{MiLatte}/image");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the image URL is cleared
        var item = await client.GetFromJsonAsync<MenuItemDto>($"/api/menu-items/{MiLatte}");
        item!.ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task DeleteImage_RequiresOwnerAuthorization()
    {
        var anonClient = _factory.CreateClient();

        var response = await anonClient.DeleteAsync($"/api/menu-items/{MiLatte}/image");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteImage_Returns404ForNonexistentMenuItem()
    {
        var client = await _factory.CreateOwnerClientAsync();

        var response = await client.DeleteAsync($"/api/menu-items/{Guid.NewGuid()}/image");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
