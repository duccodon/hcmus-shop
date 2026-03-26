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
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.Property(o => o.Subtotal)
                   .HasColumnType("decimal(18,2)");

            builder.Property(o => o.DiscountAmount)
                   .HasColumnType("decimal(18,2)");

            builder.Property(o => o.FinalAmount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(o => o.Status)
                   .HasMaxLength(20)
                   .IsRequired();
        }
    }
}
