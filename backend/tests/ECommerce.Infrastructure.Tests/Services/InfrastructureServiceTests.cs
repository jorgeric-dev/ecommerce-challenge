using ECommerce.Application.Abstractions.Payments;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Categories;
using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.Payments;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.ReadServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace ECommerce.Infrastructure.Tests.Services;

public sealed class InfrastructureServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task CategoryCatalog_ReturnsSeededCategoriesOrderedByName()
    {
        await using var context = _database.CreateContext();

        var categories = await new CategoryCatalog(context).GetAllAsync();

        categories.Count.ShouldBe(18);
        categories.Select(category => category.Name).ShouldBeInOrder();
        context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task CachedCategoryCatalog_HitsInnerCatalogOnlyOnce()
    {
        var inner = Substitute.For<ICategoryCatalog>();
        inner.GetAllAsync(Arg.Any<CancellationToken>()).Returns([new CategoryDto(1, "Books")]);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedCategoryCatalog(inner, cache, Options.Create(new CatalogCacheOptions()));

        var first = await cached.GetAllAsync();
        var second = await cached.GetAllAsync();

        second.ShouldBeSameAs(first);
        await inner.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CachedCategoryCatalog_ReloadsAfterExpiration()
    {
        var inner = Substitute.For<ICategoryCatalog>();
        inner.GetAllAsync(Arg.Any<CancellationToken>()).Returns([new CategoryDto(1, "Books")]);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedCategoryCatalog(inner, cache, Options.Create(new CatalogCacheOptions()));

        await cached.GetAllAsync();
        cache.Remove(CachedCategoryCatalog.CacheKey);
        await cached.GetAllAsync();

        await inner.Received(2).GetAllAsync(Arg.Any<CancellationToken>());
    }

    private static PaymentRequest Payment(string cardNumber, decimal amount = 10m) =>
        new("ORD-1", amount, "USD", "Jane", cardNumber, 12, 2030, "123");

    [Fact]
    public async Task FakePaymentGateway_ApprovesRegularCards()
    {
        var result = await new FakePaymentGateway().ChargeAsync(Payment("4242424242424242"));

        result.IsApproved.ShouldBeTrue();
        result.TransactionId.ShouldNotBeNull().ShouldStartWith("PAY-");
        result.CardLast4.ShouldBe("4242");
        result.FailureReason.ShouldBeNull();
    }

    [Theory]
    [InlineData(FakePaymentGateway.DeclinedCardNumber, "declined")]
    [InlineData(FakePaymentGateway.InsufficientFundsCardNumber, "insufficient funds")]
    public async Task FakePaymentGateway_DeclinesTestCards(string cardNumber, string reason)
    {
        var result = await new FakePaymentGateway().ChargeAsync(Payment(cardNumber));

        result.IsApproved.ShouldBeFalse();
        result.TransactionId.ShouldBeNull();
        result.FailureReason.ShouldNotBeNull().ShouldContain(reason);
    }

    [Fact]
    public async Task FakePaymentGateway_DeclinesNonPositiveAmounts()
    {
        var result = await new FakePaymentGateway().ChargeAsync(Payment("4242424242424242", 0m));

        result.IsApproved.ShouldBeFalse();
    }

    [Fact]
    public async Task FakePaymentGateway_HonoursCancellation()
    {
        await Should.ThrowAsync<OperationCanceledException>(() =>
            new FakePaymentGateway().ChargeAsync(Payment("4242424242424242"), new CancellationToken(canceled: true)));
    }
}
