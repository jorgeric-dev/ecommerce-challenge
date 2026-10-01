using ECommerce.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();

        builder.Property(product => product.Name)
            .HasMaxLength(ProductConstraints.NameMaxLength)
            .IsRequired();

        builder.Property(product => product.Sku)
            .HasMaxLength(ProductConstraints.SkuMaxLength)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasMaxLength(ProductConstraints.DescriptionMaxLength);

        builder.Property(product => product.Price)
            .HasPrecision(ProductConstraints.PricePrecision, ProductConstraints.PriceScale);

        builder.Property(product => product.WeightKg)
            .HasPrecision(ProductConstraints.WeightPrecision, ProductConstraints.WeightScale);

        builder.Property(product => product.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasOne(product => product.Category)
            .WithMany()
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(product => product.Sku)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(product => product.Name);
        builder.HasIndex(product => product.Price);
        builder.HasIndex(product => product.CreatedAtUtc);

        builder.HasQueryFilter(product => !product.IsDeleted);
    }
}
