using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("APPOINTMENTS");

        builder.HasKey(a => a.Id);

        // Relación 1 a N con Patient
        builder.HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Relación 1 a 1 con AvailabilitySlot (UNIQUE constraint)
        builder.HasOne(a => a.AvailabilitySlot)
            .WithMany()
            .HasForeignKey(a => a.AvailabilitySlotId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.AvailabilitySlotId)
            .IsUnique();

        // Configuración de propiedades
        builder.Property(a => a.Reason)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        // Tipos de fecha configurados como datetime2
        builder.Property(a => a.CreatedAt)
            .HasColumnType("datetime2");

        builder.Property(a => a.UpdatedAt)
            .HasColumnType("datetime2");

        builder.Property(a => a.CancelledAt)
            .HasColumnType("datetime2");

        builder.Property(a => a.AttendedAt)
            .HasColumnType("datetime2");
    }
}
