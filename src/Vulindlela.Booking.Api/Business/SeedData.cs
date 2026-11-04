using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Data;
using Vulindlela.Booking.Api.Data.Entities;
using Vulindlela.Shared;

namespace Vulindlela.Booking.Api.Business;

public static class SeedData
{
    public static async Task EnsureSeededAsync(BookingDbContext db)
    {
        if (await db.Branches.AnyAsync()) return;

        var branches = new[]
        {
            new Branch { Code = "JHB-CENTRAL-01", Name = "Johannesburg Central" },
            new Branch { Code = "JHB-SOWETO-02", Name = "Soweto" },
            new Branch { Code = "PTA-PRETORIA-03", Name = "Pretoria" }
        };
        db.Branches.AddRange(branches);
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;
        foreach (var branch in branches)
        {
            for (int day = 0; day < 14; day++)
            {
                var date = today.AddDays(day);
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                for (int hour = 8; hour <= 15; hour++)
                {
                    for (int half = 0; half < 2; half++)
                    {
                        var starts = date.AddHours(hour).AddMinutes(half * 30);
                        db.Slots.Add(new Slot { BranchId = branch.Id, StartsAtUtc = starts });
                    }
                }
            }
        }
        await db.SaveChangesAsync();
        
        AddUser(db, "thabo", "8001015009087", "Thabo Mokoena", "Citizen", "Passw0rd!");
        AddUser(db, "naledi", "9002025009088", "Naledi Dlamini", "Citizen", "Passw0rd!");
        AddUser(db, "sipho", "8503035009089", "Sipho Nkosi", "Citizen", "Passw0rd!");
        AddUser(db, "official", "7004045009090", "Official Mahlangu", "Official", "Passw0rd!");
        await db.SaveChangesAsync();
    }

    private static void AddUser(BookingDbContext db, string user, string idNumber, string name, string role, string password)
    {
        var (hash, salt) = PasswordHasher.Create(password);
        db.Citizens.Add(new Citizen
        {
            Username = user,
            IdNumber = idNumber,
            FullName = name,
            Role = role,
            PasswordHash = hash,
            PasswordSalt = salt
        });
    }
}