using hcmus_shop.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hcmus_shop.Database.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.ProductId);

            builder.Property(p => p.Sku)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(p => p.Name)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(p => p.ImportPrice)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(p => p.SellingPrice)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            // JSONB cho thông số kỹ thuật laptop
            builder.Property(p => p.Specifications)
                   .HasColumnType("jsonb");

            builder.HasOne(p => p.Category)
                   .WithMany(c => c.Products)
                   .HasForeignKey(p => p.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Brand)
                   .WithMany()
                   .HasForeignKey(p => p.BrandId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Series)
                   .WithMany(s => s.Products)
                   .HasForeignKey(p => p.SeriesId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
