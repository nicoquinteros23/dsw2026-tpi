using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dsw2026Tpi.Domain;

namespace Dsw2026Tpi.Data.Configurations
{
    public class PatientConfiguration : IEnityTypeConfiguration <Patient>
    {
        public void Configure (EntityTypeBuilder<Patient>)
        {
            builder.ToTable ("PATIENTS");
            builder.HasKey (x => x.Id);
            builder.Property (x => x.Dni)
            .IsRequired()
            .HasMaxLength (10);
            builder.HasIndex (x => x.Dni)
            .IsUnique();
            builder.Property ( x => x.FullName)
            .IsRequired ()
            .HasMaxLength (100);
            builder.Property ( x => x.Email)
            .IsRequired ()
            .HasMaxLength (255);
            builder.Property (x => x.Telefono)
            .HasMaxLength (20);
            builder.Property (x => x.Deleted)
            .HasDefaultValue (false);
        }

    }
}



