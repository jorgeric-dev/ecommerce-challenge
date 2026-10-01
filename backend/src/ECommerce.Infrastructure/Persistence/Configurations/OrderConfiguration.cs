using ECommerce.Domain.Orders;
using ECommerce.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();

        builder.Property(order => order.OrderNumber)
            .HasMaxLength(OrderConstraints.OrderNumberMaxLength)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(order => order.CustomerName)
            .HasMaxLength(OrderConstraints.CustomerNameMaxLength)
            .IsRequired();

        builder.Property(order => order.CustomerEmail)
            .HasMaxLength(OrderConstraints.CustomerEmailMaxLength)
            .IsRequired();

        builder.Property(order => order.ShippingAddress)
            .HasMaxLength(OrderConstraints.ShippingAddressMaxLength)
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(OrderConstraints.StatusMaxLength)
            .IsUnicode(false);

        builder.Property(order => order.TotalAmount)
            .HasPrecision(ProductConstraints.PricePrecision, ProductConstraints.PriceScale);

        builder.Property(order => order.PaymentReference)
            .HasMaxLength(OrderConstraints.PaymentReferenceMaxLength)
            .IsUnicode(false);

        builder.Property(order => order.CardLast4)
            .HasMaxLength(OrderConstraints.CardLast4Length)
            .IsFixedLength()
            .IsUnicode(false);

        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(order => order.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(order => order.OrderNumber).IsUnique();
        builder.HasIndex(order => order.CustomerEmail);
        builder.HasIndex(order => order.CreatedAtUtc);
    }
}
