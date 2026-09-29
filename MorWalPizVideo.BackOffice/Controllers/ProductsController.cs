using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Models.Constraints;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.BackOffice.Services;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers;

public class CreateProductRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
    
    public string[] CategoryIds { get; set; } = [];
}

public class UpdateProductRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
    
    public string[] CategoryIds { get; set; } = [];
}

public class BulkCreateProductRow
{
    public int RowNumber { get; set; }
    public string? InputKey { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string[] CategoryIds { get; set; } = [];
    public string[] CategoryNames { get; set; } = [];
}

public class BulkCreateProductsRequest
{
    public BulkCreateProductRow[] Items { get; set; } = [];
}

public class BulkCategoryAssignmentRow
{
    public string ProductId { get; set; } = string.Empty;
    public string? InputKey { get; set; }
    public string[] CategoryIds { get; set; } = [];
}

public class BulkCategoryAssignmentRequest
{
    public BulkCategoryAssignmentRow[] Items { get; set; } = [];
}

public class BulkProductOperationOutcome
{
    public int? RowNumber { get; init; }
    public string? InputKey { get; init; }
    public string? ProductId { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
}

public class BulkProductOperationResponse
{
    public BulkProductOperationOutcome[] Results { get; init; } = [];
}

[RequireChannelScope]
public class ProductsController : ApplicationControllerBase
{
    private readonly DataService _dataService;
    private readonly ICrossApiService _crossApiService;

    public ProductsController(DataService dataService, ICrossApiService crossApiService)
    {
        _dataService = dataService;
        _crossApiService = crossApiService;
    }

    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.ProductsView, AuthorizationPermissionKeys.ProductsManage)]
    public async Task<IActionResult> GetProducts()
    {
        var entities = await _dataService.GetProducts(HttpContext.GetChannelContext().ChannelId);
        return Ok(entities.Select(ContractUtils.Convert));
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ProductsView, AuthorizationPermissionKeys.ProductsManage)]
    public async Task<IActionResult> GetProduct(string id)
    {
        var entity = await _dataService.GetProductById(id, HttpContext.GetChannelContext().ChannelId);
        if (entity == null)
            return NotFound();
        return Ok(ContractUtils.Convert(entity));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.ProductsCreate, AuthorizationPermissionKeys.ProductsManage)]
    public async Task<IActionResult> CreateProduct(CreateProductRequest request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        // Fetch and validate categories
        CategoryRef[] categoryRefs = [];
        if (request.CategoryIds.Length > 0)
        {
            var categories = await _dataService.FetchProductCategories(request.CategoryIds, channelId);
            if (categories.Count != request.CategoryIds.Length)
                return BadRequest("One or more category IDs are invalid");
            
            categoryRefs = categories.Select(c => new CategoryRef(c.Id, c.Title)).ToArray();
        }

        var product = new Product(
            request.Title,
            request.Description,
            request.Url,
            categoryRefs
        );

        if (!await _dataService.SaveProduct(product, channelId))
            return Conflict("A product with this title already exists for the selected channel.");
        await InvalidateProductsCacheAsync();
        return NoContent();
    }

    [HttpPost("bulk")]
    [AllowUser(AuthorizationPermissionKeys.ProductsCreate, AuthorizationPermissionKeys.ProductsManage)]
    public async Task<IActionResult> CreateProductsBulk(BulkCreateProductsRequest request)
    {
        if (request.Items.Length == 0)
            return BadRequest("At least one product row is required.");
        if (request.Items.Length > 100)
            return BadRequest("A maximum of 100 product rows can be imported.");

        var channelId = HttpContext.GetChannelContext().ChannelId;
        var categories = await _dataService.FetchProductCategories(null, channelId);
        var categoriesById = categories.ToDictionary(category => category.Id, StringComparer.OrdinalIgnoreCase);
        var categoriesByName = categories
            .GroupBy(category => category.Title.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var outcomes = new List<BulkProductOperationOutcome>(request.Items.Length);
        var changed = false;

        foreach (var row in request.Items)
        {
            var error = ValidateProductRow(row, categoriesById, categoriesByName, out var categoryRefs);
            if (error != null)
            {
                outcomes.Add(new BulkProductOperationOutcome { RowNumber = row.RowNumber, InputKey = row.InputKey, Success = false, Error = error });
                continue;
            }

            var product = new Product(row.Title.Trim(), row.Description.Trim(), row.Url.Trim(), categoryRefs!);
            if (!await _dataService.SaveProduct(product, channelId))
            {
                outcomes.Add(new BulkProductOperationOutcome { RowNumber = row.RowNumber, InputKey = row.InputKey, Success = false, Error = "A product with this title already exists for the selected channel." });
                continue;
            }

            changed = true;
            outcomes.Add(new BulkProductOperationOutcome { RowNumber = row.RowNumber, InputKey = row.InputKey, ProductId = product.Id, Success = true });
        }

        if (changed)
            await InvalidateProductsCacheAsync();

        return Ok(new BulkProductOperationResponse { Results = outcomes.ToArray() });
    }

    [HttpPost("categories/bulk")]
    [AllowUser(AuthorizationPermissionKeys.ProductsUpdate, AuthorizationPermissionKeys.ProductsManage)]
    public async Task<IActionResult> AssignProductCategoriesBulk(BulkCategoryAssignmentRequest request)
    {
        if (request.Items.Length == 0)
            return BadRequest("At least one product is required.");

        var channelId = HttpContext.GetChannelContext().ChannelId;
        var categoryIds = request.Items.SelectMany(item => item.CategoryIds).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (categoryIds.Length == 0)
            return BadRequest("At least one category ID is required.");

        var categories = await _dataService.FetchProductCategories(categoryIds, channelId);
        var categoriesById = categories.ToDictionary(category => category.Id, StringComparer.OrdinalIgnoreCase);
        var products = await _dataService.GetProducts(channelId);
        var productsById = products.ToDictionary(product => product.Id, StringComparer.OrdinalIgnoreCase);
        var outcomes = new List<BulkProductOperationOutcome>(request.Items.Length);
        var changed = false;

        foreach (var row in request.Items)
        {
            var productId = row.ProductId.Trim();
            Product? product = null;
            var error = productId.Length == 0 ? "Product ID is required." : null;
            if (error == null && !productsById.TryGetValue(productId, out product))
                error = "Product was not found in the selected channel.";
            if (error == null && row.CategoryIds.Length == 0)
                error = "At least one category ID is required.";
            if (error == null && row.CategoryIds.Any(id => !categoriesById.ContainsKey(id)))
                error = "One or more category IDs are invalid for the selected channel.";

            if (error != null)
            {
                outcomes.Add(new BulkProductOperationOutcome { InputKey = row.InputKey, ProductId = productId, Success = false, Error = error });
                continue;
            }

            var additions = row.CategoryIds
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(id => categoriesById[id])
                .Select(category => new CategoryRef(category.Id, category.Title));
            var mergedCategories = product!.Categories
                .Concat(additions)
                .GroupBy(category => category.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();

            if (!await _dataService.UpdateProduct(product with { Categories = mergedCategories }, channelId))
            {
                outcomes.Add(new BulkProductOperationOutcome { InputKey = row.InputKey, ProductId = productId, Success = false, Error = "Product could not be updated." });
                continue;
            }

            changed = true;
            outcomes.Add(new BulkProductOperationOutcome { InputKey = row.InputKey, ProductId = productId, Success = true });
        }

        if (changed)
            await InvalidateProductsCacheAsync();

        return Ok(new BulkProductOperationResponse { Results = outcomes.ToArray() });
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ProductsUpdate, AuthorizationPermissionKeys.ProductsManage)]
    public async Task<IActionResult> UpdateProduct(BaseRequestId<UpdateProductRequest> request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var entity = await _dataService.GetProductById(request.Id, channelId);
        if (entity == null)
            return BadRequest("Product not found");

        // Fetch and validate categories
        CategoryRef[] categoryRefs = [];
        if (request.Body.CategoryIds.Length > 0)
        {
            var categories = await _dataService.FetchProductCategories(request.Body.CategoryIds, channelId);
            if (categories.Count != request.Body.CategoryIds.Length)
                return BadRequest("One or more category IDs are invalid");
            
            categoryRefs = categories.Select(c => new CategoryRef(c.Id, c.Title)).ToArray();
        }

        var updatedProduct = entity with
        {
            Title = request.Body.Title,
            Description = request.Body.Description,
            Url = request.Body.Url,
            Categories = categoryRefs
        };

        if (!await _dataService.UpdateProduct(updatedProduct, channelId))
            return Conflict("A product with this title already exists for the selected channel.");
        await InvalidateProductsCacheAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ProductsDelete, AuthorizationPermissionKeys.ProductsManage)]
    public async Task<IActionResult> DeleteProduct(BaseRequestId request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var entity = await _dataService.GetProductById(request.Id, channelId);
        if (entity == null)
        {
            return BadRequest("Product not found");
        }

        await _dataService.DeleteProduct(entity.Id, channelId);
        await InvalidateProductsCacheAsync();
        return NoContent();
    }

    private async Task InvalidateProductsCacheAsync()
    {
        await _crossApiService.ResetCache(CacheKeys.Products);
        await _crossApiService.PurgeCache(ApiTagCacheKeys.Products);
    }

    private static string? ValidateProductRow(
        BulkCreateProductRow row,
        IReadOnlyDictionary<string, ProductCategory> categoriesById,
        IReadOnlyDictionary<string, ProductCategory> categoriesByName,
        out CategoryRef[]? categoryRefs)
    {
        categoryRefs = null;
        if (string.IsNullOrWhiteSpace(row.Title)) return "Title is required.";
        if (string.IsNullOrWhiteSpace(row.Description)) return "Description is required.";
        if (string.IsNullOrWhiteSpace(row.Url) || !Uri.TryCreate(row.Url.Trim(), UriKind.Absolute, out var uri) || uri is null || string.IsNullOrWhiteSpace(uri.Scheme) || string.IsNullOrWhiteSpace(uri.Host)) return "URL must be an absolute URL.";

        var resolved = new List<CategoryRef>();
        foreach (var id in row.CategoryIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!categoriesById.TryGetValue(id.Trim(), out var category)) return "One or more category IDs are invalid for the selected channel.";
            resolved.Add(new CategoryRef(category.Id, category.Title));
        }
        foreach (var name in row.CategoryNames.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!categoriesByName.TryGetValue(name, out var category)) return $"Category name '{name}' is invalid for the selected channel.";
            resolved.Add(new CategoryRef(category.Id, category.Title));
        }

        categoryRefs = resolved.GroupBy(category => category.Id, StringComparer.OrdinalIgnoreCase).Select(group => group.First()).ToArray();
        return null;
    }
}
