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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("ck_users_role", "role in ('Admin','Sale')");
            });

            builder.Property(u => u.Username)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(u => u.PasswordHash)
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(u => u.Role)
                   .HasMaxLength(20)
                   .HasDefaultValue("Sale")
                   .IsRequired();
        }
    }
}
