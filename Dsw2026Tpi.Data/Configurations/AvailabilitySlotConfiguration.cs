using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
{
    public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
    {
        builder.ToTable("AVAILABILITYSLOTS");

        builder.HasKey(aslt => aslt.Id);

        // Relación N a 1 con AvailabilityRule
        builder.HasOne(aslt => aslt.AvailabilityRule)
            .WithMany(ar => ar.AvailabilitySlots)
            .HasForeignKey(aslt => aslt.AvailabilityRuleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Campo SlotDate configurado como date
        builder.Property(aslt => aslt.SlotDate)
            .HasColumnType("date")
            .IsRequired();

        // Campos StartTime y EndTime configurados como time
        builder.Property(aslt => aslt.StartTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(aslt => aslt.EndTime)
            .HasColumnType("time")
            .IsRequired();

        // Configuración de Status
        builder.Property(aslt => aslt.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("AVAILABLE");

        // Configuración del campo Deleted
        builder.Property(aslt => aslt.Deleted)
            .HasDefaultValue(false);

        // Global Query Filter para soft delete
        builder.HasQueryFilter(aslt => !aslt.Deleted);
    }
}
