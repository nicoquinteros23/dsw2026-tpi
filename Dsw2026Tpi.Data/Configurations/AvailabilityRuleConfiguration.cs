using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AvailabilityRuleConfiguration : IEntityTypeConfiguration<AvailabilityRule>
{
    public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
    {
        builder.ToTable("AVAILABILITYRULES");

        builder.HasKey(ar => ar.Id);

        // Relación N a 1 con Doctor
        builder.HasOne(ar => ar.Doctor)
            .WithMany()
            .HasForeignKey(ar => ar.DoctorId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Configuración de campos numéricos
        builder.Property(ar => ar.Month)
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(ar => ar.Year)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(ar => ar.DayOfWeek)
            .HasColumnType("tinyint")
            .IsRequired();

        // Campos de tiempo configurados como time
        builder.Property(ar => ar.StartTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(ar => ar.EndTime)
            .HasColumnType("time")
            .IsRequired();

        // Configuración del campo Deleted
        builder.Property(ar => ar.Deleted)
            .HasDefaultValue(false);

        // Índice Único Restrictivo (Unique Constraint)
        builder.HasIndex(ar => new { ar.DoctorId, ar.Year, ar.Month, ar.DayOfWeek, ar.StartTime, ar.EndTime })
            .IsUnique()
            .HasDatabaseName("UX_AvailabilityRule_DoctorYearMonthDayOfWeekTimes");

        // Global Query Filter para soft delete
        builder.HasQueryFilter(ar => !ar.Deleted);
    }
}
