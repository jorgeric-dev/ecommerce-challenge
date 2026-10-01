using ECommerce.Domain.Orders;
using ECommerce.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property(item => item.ProductName)
            .HasMaxLength(ProductConstraints.NameMaxLength)
            .IsRequired();

        builder.Property(item => item.Sku)
            .HasMaxLength(ProductConstraints.SkuMaxLength)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasPrecision(ProductConstraints.PricePrecision, ProductConstraints.PriceScale);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
