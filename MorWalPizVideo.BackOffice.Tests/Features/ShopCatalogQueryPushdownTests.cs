using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.ServerAPI.Controllers;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public class ShopCatalogQueryPushdownTests
{
    [Fact]
    public async Task GetProductsByCategory_UsesRepositoryPushdown()
    {
        var productRepository = new RecordingDigitalProductRepository();
        productRepository.ProductsByCategory =
        [
            new DigitalProduct("Product 1", "Desc", "img", "blob", ["cat-a"], 9.99m, true) { Id = "p1" }
        ];

        var categoryRepository = new StubDigitalProductCategoryRepository();
        var controller = new ShopCatalogController(
            dataService: null!,
            memoryCache: null!,
            productRepository: productRepository,
            categoryRepository: categoryRepository);

        var result = await controller.GetProductsByCategory("cat-a");

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, productRepository.GetByCategoryCalls);
        Assert.Equal("cat-a", productRepository.LastCategoryId);
        Assert.Equal(2000, productRepository.LastCategoryLimit);
        Assert.False(productRepository.GetItemsCalled);
    }

    [Fact]
    public async Task GetProductsByCategory_ClampsLimit()
    {
        var productRepository = new RecordingDigitalProductRepository();
        var categoryRepository = new StubDigitalProductCategoryRepository();
        var controller = new ShopCatalogController(
            dataService: null!,
            memoryCache: null!,
            productRepository: productRepository,
            categoryRepository: categoryRepository);

        await controller.GetProductsByCategory("cat-a", limit: int.MaxValue);
        Assert.Equal(2000, productRepository.LastCategoryLimit);

        await controller.GetProductsByCategory("cat-a", limit: -10);
        Assert.Equal(1, productRepository.LastCategoryLimit);
    }

    [Fact]
    public async Task GetProducts_UsesBoundedRepositoryQuery()
    {
        var productRepository = new RecordingDigitalProductRepository
        {
            PublicCatalog =
            [
                new DigitalProduct("Product 1", "Desc", "img", "blob", ["cat-a"], 9.99m, true) { Id = "p1" }
            ]
        };

        var categoryRepository = new StubDigitalProductCategoryRepository();
        var controller = new ShopCatalogController(
            dataService: null!,
            memoryCache: null!,
            productRepository: productRepository,
            categoryRepository: categoryRepository);

        var result = await controller.GetProducts(skip: 5, take: 25);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, productRepository.GetPublicCatalogCalls);
        Assert.Equal(5, productRepository.LastPublicCatalogSkip);
        Assert.Equal(25, productRepository.LastPublicCatalogTake);
        Assert.False(productRepository.GetItemsCalled);
    }

    [Fact]
    public async Task GetProducts_ClampsSkipAndTake()
    {
        var productRepository = new RecordingDigitalProductRepository();
        var categoryRepository = new StubDigitalProductCategoryRepository();
        var controller = new ShopCatalogController(
            dataService: null!,
            memoryCache: null!,
            productRepository: productRepository,
            categoryRepository: categoryRepository);

        await controller.GetProducts(skip: -5, take: int.MaxValue);

        Assert.Equal(0, productRepository.LastPublicCatalogSkip);
        Assert.Equal(500, productRepository.LastPublicCatalogTake);
    }

    [Fact]
    public async Task GetCategories_UsesBoundedRepositoryQuery()
    {
        var productRepository = new RecordingDigitalProductRepository();
        var categoryRepository = new StubDigitalProductCategoryRepository
        {
            OrderedCategories =
            [
                new DigitalProductCategory("Category", "Desc", 1) { Id = "c1" }
            ]
        };

        var controller = new ShopCatalogController(
            dataService: null!,
            memoryCache: null!,
            productRepository: productRepository,
            categoryRepository: categoryRepository);

        var result = await controller.GetCategories(skip: 2, take: 10);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, categoryRepository.GetOrderedCalls);
        Assert.Equal(2, categoryRepository.LastSkip);
        Assert.Equal(10, categoryRepository.LastTake);
        Assert.False(categoryRepository.GetItemsCalled);
    }

    [Fact]
    public async Task GetCategories_ClampsSkipAndTake()
    {
        var productRepository = new RecordingDigitalProductRepository();
        var categoryRepository = new StubDigitalProductCategoryRepository();
        var controller = new ShopCatalogController(
            dataService: null!,
            memoryCache: null!,
            productRepository: productRepository,
            categoryRepository: categoryRepository);

        await controller.GetCategories(skip: -2, take: int.MaxValue);

        Assert.Equal(0, categoryRepository.LastSkip);
        Assert.Equal(500, categoryRepository.LastTake);
    }

    [Fact]
    public void ShopCatalogController_PreservesAnonymousAndCacheTags()
    {
        var allowAnonymous = typeof(ShopCatalogController).GetCustomAttribute<AllowAnonymousAttribute>();
        Assert.NotNull(allowAnonymous);

        var method = typeof(ShopCatalogController).GetMethod(nameof(ShopCatalogController.GetProductsByCategory));
        Assert.NotNull(method);

        var outputCache = method!.GetCustomAttribute<OutputCacheAttribute>();
        Assert.NotNull(outputCache);
        Assert.NotNull(outputCache!.Tags);

        Assert.Contains(CacheKeys.DigitalProducts, outputCache.Tags!);
        Assert.Contains(CacheKeys.DigitalProductCategories, outputCache.Tags!);
    }

    private sealed class RecordingDigitalProductRepository : IDigitalProductRepository
    {
        public int GetByCategoryCalls { get; private set; }
        public string? LastCategoryId { get; private set; }
        public int GetPublicCatalogCalls { get; private set; }
        public int LastPublicCatalogSkip { get; private set; }
        public int LastPublicCatalogTake { get; private set; }
        public int LastCategoryLimit { get; private set; }
        public bool GetItemsCalled { get; private set; }
        public IList<DigitalProduct> ProductsByCategory { get; set; } = [];
        public IList<DigitalProduct> PublicCatalog { get; set; } = [];

        public Task<IList<DigitalProduct>> GetPublicCatalogAsync(int skip, int take)
        {
            GetPublicCatalogCalls++;
            LastPublicCatalogSkip = skip;
            LastPublicCatalogTake = take;
            return Task.FromResult(PublicCatalog);
        }

        public Task<IList<DigitalProduct>> GetByCategoryIdAsync(string categoryId, int limit = 500)
        {
            GetByCategoryCalls++;
            LastCategoryId = categoryId;
            LastCategoryLimit = limit;
            return Task.FromResult(ProductsByCategory);
        }

        public Task<DigitalProduct> GetItemAsync(string id) => throw new NotSupportedException();

        public Task<IList<DigitalProduct>> GetItemsAsync()
        {
            GetItemsCalled = true;
            return Task.FromResult<IList<DigitalProduct>>([]);
        }

        public Task<IList<DigitalProduct>> GetItemsAsync(System.Linq.Expressions.Expression<Func<DigitalProduct, bool>> predicate)
            => throw new NotSupportedException();

        public Task<DigitalProduct> AddItemAsync(DigitalProduct item) => throw new NotSupportedException();

        public Task UpdateItemAsync(DigitalProduct item) => throw new NotSupportedException();

        public Task DeleteItemAsync(string id) => throw new NotSupportedException();
    }

    private sealed class StubDigitalProductCategoryRepository : IDigitalProductCategoryRepository
    {
        public int GetOrderedCalls { get; private set; }
        public int LastSkip { get; private set; }
        public int LastTake { get; private set; }
        public bool GetItemsCalled { get; private set; }
        public IList<DigitalProductCategory> OrderedCategories { get; set; } = [];

        public Task<IList<DigitalProductCategory>> GetOrderedAsync(int skip, int take)
        {
            GetOrderedCalls++;
            LastSkip = skip;
            LastTake = take;
            return Task.FromResult(OrderedCategories);
        }

        public Task<DigitalProductCategory> GetItemAsync(string id) => throw new NotSupportedException();

        public Task<IList<DigitalProductCategory>> GetItemsAsync()
        {
            GetItemsCalled = true;
            return Task.FromResult<IList<DigitalProductCategory>>([]);
        }

        public Task<IList<DigitalProductCategory>> GetItemsAsync(System.Linq.Expressions.Expression<Func<DigitalProductCategory, bool>> predicate)
            => throw new NotSupportedException();

        public Task<DigitalProductCategory> AddItemAsync(DigitalProductCategory item) => throw new NotSupportedException();

        public Task UpdateItemAsync(DigitalProductCategory item) => throw new NotSupportedException();

        public Task DeleteItemAsync(string id) => throw new NotSupportedException();
    }
}
