using System.Text.Json;
using CabinetMap.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE RecordFiles ADD COLUMN AttachmentsJson TEXT DEFAULT '[]';");
        }
        catch { }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Folders ADD COLUMN AttachmentsJson TEXT DEFAULT '[]';");
        }
        catch { }

        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""Users"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Users"" PRIMARY KEY AUTOINCREMENT,
                    ""FullName"" TEXT NOT NULL,
                    ""Email"" TEXT NOT NULL,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""Designation"" TEXT NOT NULL,
                    ""Gender"" TEXT NOT NULL DEFAULT 'Male',
                    ""Role"" TEXT NULL DEFAULT 'User',
                    ""ResetCode"" TEXT NULL,
                    ""ResetCodeExpiresAt"" TEXT NULL,
                    ""CreatedAt"" TEXT NOT NULL,
                    ""LastLoginAt"" TEXT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");
                CREATE TABLE IF NOT EXISTS ""ActivityLogs"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_ActivityLogs"" PRIMARY KEY AUTOINCREMENT,
                    ""UserId"" INTEGER NULL,
                    ""UserName"" TEXT NULL,
                    ""UserEmail"" TEXT NULL,
                    ""UserDesignation"" TEXT NULL,
                    ""UserGender"" TEXT NULL,
                    ""ActionType"" TEXT NOT NULL,
                    ""EntityType"" TEXT NULL,
                    ""EntityId"" INTEGER NULL,
                    ""EntityTitle"" TEXT NULL,
                    ""Details"" TEXT NOT NULL,
                    ""Timestamp"" TEXT NOT NULL
                );
            ");
        }
        catch { }

        // Seed Admin User if none exists
        if (!context.Users.Any())
        {
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
            var adminUser = new User
            {
                FullName = "System Admin",
                Email = "admin@cabinetmap.com",
                Designation = "Senior HR Administrator",
                Gender = "Male",
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };
            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin123!");
            context.Users.Add(adminUser);
            context.SaveChanges();

            // Seed initial sample audit activity logs
            context.ActivityLogs.AddRange(
                new ActivityLog
                {
                    UserId = adminUser.Id,
                    UserName = adminUser.FullName,
                    UserEmail = adminUser.Email,
                    UserDesignation = adminUser.Designation,
                    UserGender = adminUser.Gender,
                    ActionType = "REGISTER",
                    EntityType = "User",
                    EntityId = adminUser.Id,
                    EntityTitle = adminUser.FullName,
                    Details = "System Admin initialized the CabinetMap archive database.",
                    Timestamp = DateTime.UtcNow.AddHours(-2)
                },
                new ActivityLog
                {
                    UserId = adminUser.Id,
                    UserName = adminUser.FullName,
                    UserEmail = adminUser.Email,
                    UserDesignation = adminUser.Designation,
                    UserGender = adminUser.Gender,
                    ActionType = "CREATE",
                    EntityType = "Cabinet",
                    EntityId = 1,
                    EntityTitle = "Cabinet 1 (W1)",
                    Details = "Initialized Wall 1 and Wall 2 cabinet structures.",
                    Timestamp = DateTime.UtcNow.AddHours(-1)
                }
            );
            context.SaveChanges();
        }

        // Check if already seeded
        if (context.Cabinets.Any())
        {
            try
            {
                context.Database.ExecuteSqlRaw("UPDATE Cabinets SET Description = '' WHERE Description LIKE '%Wall 1%';");
                context.Database.ExecuteSqlRaw("DELETE FROM Shelves WHERE Section = 'Lower' AND ShelfCode = 'L3';");
            }
            catch { }

            var existingBll = context.DocumentTypes.FirstOrDefault(d => d.Name == "BLL");
            if (existingBll != null && existingBll.FieldsJson.Contains("location"))
            {
                existingBll.FieldsJson = JsonSerializer.Serialize(new[]
                {
                    new { key = "employeeName", label = "Employee Name", type = "text", required = true },
                    new { key = "staffId", label = "Staff ID", type = "text", required = true },
                    new { key = "designation", label = "Designation", type = "text", required = true },
                    new { key = "department", label = "Department", type = "text", required = true }
                });
                context.SaveChanges();
            }
            return;
        }

        // 1. Seed Document Types
        var telType = new DocumentType
        {
            Name = "TEL",
            Description = "Transcom Electronics Limited - Employee Record",
            IsBuiltIn = true,
            FieldsJson = JsonSerializer.Serialize(new[]
            {
                new { key = "employeeName", label = "Employee Name", type = "text", required = true },
                new { key = "employeeNo", label = "Employee No.", type = "text", required = true },
                new { key = "designation", label = "Designation", type = "text", required = true },
                new { key = "department", label = "Department", type = "text", required = true },
                new { key = "joiningDate", label = "Joining Date", type = "date", required = false }
            })
        };

        var bllType = new DocumentType
        {
            Name = "BLL",
            Description = "Bangladesh Lamps Limited - Employee Record",
            IsBuiltIn = true,
            FieldsJson = JsonSerializer.Serialize(new[]
            {
                new { key = "employeeName", label = "Employee Name", type = "text", required = true },
                new { key = "staffId", label = "Staff ID", type = "text", required = true },
                new { key = "designation", label = "Designation", type = "text", required = true },
                new { key = "department", label = "Department", type = "text", required = true }
            })
        };

        var adminGeneralType = new DocumentType
        {
            Name = "Admin / General",
            Description = "General Administrative, Legal & Audit Documentation",
            IsBuiltIn = true,
            FieldsJson = JsonSerializer.Serialize(new[]
            {
                new { key = "subject", label = "Subject / Title", type = "text", required = true },
                new { key = "category", label = "Category", type = "text", required = true },
                new { key = "fiscalYear", label = "Fiscal Year", type = "text", required = false },
                new { key = "issuedBy", label = "Issued By", type = "text", required = false }
            })
        };

        context.DocumentTypes.AddRange(telType, bllType, adminGeneralType);
        context.SaveChanges();

        // 2. Seed 6 Cabinets on Wall 1 and 2 Cabinets on Wall 2
        var cabinets = new List<Cabinet>();
        for (int c = 1; c <= 8; c++)
        {
            var cabinet = new Cabinet
            {
                CabinetNumber = c,
                Name = c <= 6 ? $"Cabinet {c}" : $"Cabinet {c - 6} (W2)",
                Description = c <= 6 ? "" : $"Wall 2 - 3-Door Unit {c - 6}"
            };

            if (c <= 6)
            {
                // Wall 1: Standard 4 Upper, 2 Lower
                for (int u = 1; u <= 4; u++)
                {
                    cabinet.Shelves.Add(new Shelf
                    {
                        Section = "Upper",
                        ShelfCode = $"U{u}",
                        OrderIndex = u
                    });
                }
                for (int l = 1; l <= 2; l++)
                {
                    cabinet.Shelves.Add(new Shelf
                    {
                        Section = "Lower",
                        ShelfCode = $"L{l}",
                        OrderIndex = l
                    });
                }
            }
            else
            {
                // Wall 2: 3-Door modular units with separate 2-Door and 1-Door shelves
                // 2-Door Module Shelves (U1..U4, L1..L2)
                for (int u = 1; u <= 4; u++)
                {
                    cabinet.Shelves.Add(new Shelf
                    {
                        Section = "Upper",
                        ShelfCode = $"U{u} (2-Door)",
                        OrderIndex = u
                    });
                }
                for (int l = 1; l <= 2; l++)
                {
                    cabinet.Shelves.Add(new Shelf
                    {
                        Section = "Lower",
                        ShelfCode = $"L{l} (2-Door)",
                        OrderIndex = l
                    });
                }

                // 1-Door Module Shelves (U1..U4, L1..L2)
                for (int u = 1; u <= 4; u++)
                {
                    cabinet.Shelves.Add(new Shelf
                    {
                        Section = "Upper",
                        ShelfCode = $"U{u} (1-Door)",
                        OrderIndex = u
                    });
                }
                for (int l = 1; l <= 2; l++)
                {
                    cabinet.Shelves.Add(new Shelf
                    {
                        Section = "Lower",
                        ShelfCode = $"L{l} (1-Door)",
                        OrderIndex = l
                    });
                }
            }

            cabinets.Add(cabinet);
        }

        context.Cabinets.AddRange(cabinets);
        context.SaveChanges();

        // 3. Seed Realistic Sample Data across Cabinets
        // Cabinet 1: TEL Active Personnel
        var cab1 = cabinets[0];
        var cab1U1 = cab1.Shelves.First(s => s.ShelfCode == "U1");
        var cab1U2 = cab1.Shelves.First(s => s.ShelfCode == "U2");
        var cab1L1 = cab1.Shelves.First(s => s.ShelfCode == "L1");

        // Magazines on Cab 1, Shelf U1
        var magTelSales = new Magazine
        {
            Name = "TEL Sales & Marketing Dossiers",
            Code = "MAG-TEL-SALES",
            ColorHex = "#2563EB", // Blue
            ShelfId = cab1U1.Id,
            OrderIndex = 1
        };

        var magTelFinance = new Magazine
        {
            Name = "TEL Finance & Accounts Team",
            Code = "MAG-TEL-FIN",
            ColorHex = "#7C3AED", // Purple
            ShelfId = cab1U1.Id,
            OrderIndex = 2
        };

        var folderTelAudit = new Folder
        {
            Name = "TEL Compliance & Regulatory Binder",
            Code = "FLD-TEL-REG-2025",
            ColorHex = "#D97706", // Amber
            ShelfId = cab1U1.Id,
            OrderIndex = 3
        };

        context.Magazines.AddRange(magTelSales, magTelFinance);
        context.Folders.Add(folderTelAudit);
        context.SaveChanges();

        // Files inside magTelSales
        var file1 = new RecordFile
        {
            Code = "TEL-EMP-1049",
            Title = "Rakibul Hasan - TEL Service Record",
            DocumentTypeId = telType.Id,
            MagazineId = magTelSales.Id,
            OrderIndex = 1,
            MetadataJson = JsonSerializer.Serialize(new
            {
                employeeName = "Rakibul Hasan",
                employeeNo = "TEL-1049",
                designation = "Senior Sales Executive",
                department = "Consumer Electronics",
                joiningDate = "2021-03-15"
            })
        };

        var file2 = new RecordFile
        {
            Code = "TEL-EMP-1082",
            Title = "Nusrat Jahan - TEL Service Record",
            DocumentTypeId = telType.Id,
            MagazineId = magTelSales.Id,
            OrderIndex = 2,
            MetadataJson = JsonSerializer.Serialize(new
            {
                employeeName = "Nusrat Jahan",
                employeeNo = "TEL-1082",
                designation = "Regional Territory Manager",
                department = "Institutional Sales",
                joiningDate = "2020-07-01"
            })
        };

        var file3 = new RecordFile
        {
            Code = "TEL-EMP-2015",
            Title = "Tanvir Ahmed - TEL Service Record",
            DocumentTypeId = telType.Id,
            MagazineId = magTelFinance.Id,
            OrderIndex = 1,
            MetadataJson = JsonSerializer.Serialize(new
            {
                employeeName = "Tanvir Ahmed",
                employeeNo = "TEL-2015",
                designation = "Deputy Manager - Payroll",
                department = "Finance & Accounts",
                joiningDate = "2019-11-10"
            })
        };

        // Standalone file directly on Shelf U2
        var fileStandalone1 = new RecordFile
        {
            Code = "TEL-DIR-EXEC-01",
            Title = "Managing Director Office Directives",
            DocumentTypeId = adminGeneralType.Id,
            ShelfId = cab1U2.Id,
            OrderIndex = 1,
            MetadataJson = JsonSerializer.Serialize(new
            {
                subject = "Board Resolutions & Executive Directives",
                category = "Executive",
                fiscalYear = "2024-2025",
                issuedBy = "Board of Directors"
            })
        };

        context.RecordFiles.AddRange(file1, file2, file3, fileStandalone1);

        // Cabinet 2: BLL Personnel & Plant Operations
        var cab2 = cabinets[1];
        var cab2U1 = cab2.Shelves.First(s => s.ShelfCode == "U1");
        var cab2L1 = cab2.Shelves.First(s => s.ShelfCode == "L1");

        var magBllFactory = new Magazine
        {
            Name = "BLL Mohakhali Factory Operations",
            Code = "MAG-BLL-MHK",
            ColorHex = "#059669", // Emerald Green
            ShelfId = cab2U1.Id,
            OrderIndex = 1
        };

        var folderBllSafety = new Folder
        {
            Name = "BLL Quality Assurance & Environmental Certifications",
            Code = "FLD-BLL-QA-ISO",
            ColorHex = "#DC2626", // Red
            ShelfId = cab2U1.Id,
            OrderIndex = 2
        };

        context.Magazines.Add(magBllFactory);
        context.Folders.Add(folderBllSafety);
        context.SaveChanges();

        var bllFile1 = new RecordFile
        {
            Code = "BLL-EMP-0512",
            Title = "Shahadat Hossain - BLL Dossier",
            DocumentTypeId = bllType.Id,
            MagazineId = magBllFactory.Id,
            OrderIndex = 1,
            MetadataJson = JsonSerializer.Serialize(new
            {
                employeeName = "Shahadat Hossain",
                staffId = "BLL-ST-0512",
                designation = "Lead Production Engineer",
                department = "LED Assembly Plant"
            })
        };

        var bllFile2 = new RecordFile
        {
            Code = "BLL-EMP-0789",
            Title = "Farhana Chowdhury - BLL Dossier",
            DocumentTypeId = bllType.Id,
            MagazineId = magBllFactory.Id,
            OrderIndex = 2,
            MetadataJson = JsonSerializer.Serialize(new
            {
                employeeName = "Farhana Chowdhury",
                staffId = "BLL-ST-0789",
                designation = "Procurement Specialist",
                department = "Supply Chain & Logistics"
            })
        };

        context.RecordFiles.AddRange(bllFile1, bllFile2);

        // Cabinet 3: Central HR & Administration
        var cab3 = cabinets[2];
        var cab3U3 = cab3.Shelves.First(s => s.ShelfCode == "U3");
        var folderHRPolicies = new Folder
        {
            Name = "Group HR Master Policies & Employment Manuals",
            Code = "FLD-GRP-HR-POL",
            ColorHex = "#4F46E5", // Indigo
            ShelfId = cab3U3.Id,
            OrderIndex = 1
        };

        // Cabinet 4: Legal, Leases & Tenancy Contracts
        var cab4 = cabinets[3];
        var cab4L2 = cab4.Shelves.First(s => s.ShelfCode == "L2");
        var magLegal = new Magazine
        {
            Name = "Commercial Leases & Factory Deeds",
            Code = "MAG-LEG-DEED",
            ColorHex = "#9333EA", // Violet
            ShelfId = cab4L2.Id,
            OrderIndex = 1
        };

        // Cabinet 5: Tax, Vat & Revenue Audits
        var cab5 = cabinets[4];
        var cab5U2 = cab5.Shelves.First(s => s.ShelfCode == "U2");
        var folderTax = new Folder
        {
            Name = "NBR Tax Assessment Reports (2020-2025)",
            Code = "FLD-TAX-NBR",
            ColorHex = "#0D9488", // Teal
            ShelfId = cab5U2.Id,
            OrderIndex = 1
        };

        // Cabinet 6: Archival & Inactive Files
        var cab6 = cabinets[5];
        var cab6L3 = cab6.Shelves.First(s => s.ShelfCode == "L3");
        var folderArchive = new Folder
        {
            Name = "Archived Personnel Records (Pre-2015)",
            Code = "FLD-ARCH-HIST",
            ColorHex = "#64748B", // Slate Grey
            ShelfId = cab6L3.Id,
            OrderIndex = 1
        };

        context.Folders.AddRange(folderHRPolicies, folderTax, folderArchive);
        context.Magazines.Add(magLegal);

        context.SaveChanges();
    }
}
