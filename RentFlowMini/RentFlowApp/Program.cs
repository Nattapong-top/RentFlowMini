using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Forms;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;
using RentFlowApp.Infrastructure.Tenants;

namespace RentFlowApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        IRoomEditor roomRepository =
            new InMemoryRoomRepository();
        IBillRepository billRepository =
            new InMemoryBillRepository();
        ITenantRepository tenantRepository =
            new InMemoryTenantRepository();
        IMonthlyMeterReadingRepository meterReadingRepository =
            new InMemoryMonthlyMeterReadingRepository();

        var createRoomService =
            new CreateRoomService(roomRepository);
        var createMonthlyBillService =
            new CreateMonthlyBillService(billRepository);
        var createTenantService =
            new CreateTenantService(tenantRepository);
        var saveMeterReadingService =
            new SaveMonthlyMeterReadingService(meterReadingRepository);

        SeedInitialData(roomRepository, createRoomService, createTenantService, saveMeterReadingService);

        System.Windows.Forms.Application.Run(
            new RoomForm(
                createRoomService,
                roomRepository,
                createMonthlyBillService,
                createTenantService,
                tenantRepository,
                meterReadingRepository));
    }

    private sealed record SeedRoomData(
        string Number,
        decimal Rent,
        bool Exempt,
        string? FirstName,
        string? LastName,
        string? Phone,
        decimal PrevWater,
        decimal CurrWater,
        decimal PrevElec,
        decimal CurrElec);

    private static void SeedInitialData(
        IRoomEditor roomRepository,
        CreateRoomService createRoomService,
        CreateTenantService createTenantService,
        SaveMonthlyMeterReadingService saveMeterReadingService)
    {
        if (roomRepository.GetAll().Any())
        {
            return;
        }

        var currentPeriod = BillingPeriod.Create(DateTime.Now.ToString("yyyy-MM"));

        var seedData = new SeedRoomData[]
        {
            new("101", 3000m, false, "สมชาย", "ใจดี", "0811111111", 120m, 135m, 1000m, 1050m),
            new("102", 3000m, false, "สมหญิง", "รักสงบ", "0822222222", 80m, 92m, 850m, 890m),
            new("103", 3000m, true, "วิชัย", "มั่งคั่ง", "0833333333", 200m, 215m, 1500m, 1560m),
            new("104", 3000m, false, "นภา", "ฟ้าใส", "0844444444", 50m, 65m, 400m, 445m),
            new("105", 3000m, false, null, null, null, 10m, 10m, 100m, 100m),
            new("201", 3500m, false, "กิตติ", "องอาจ", "0855555555", 310m, 328m, 2100m, 2180m),
            new("202", 3500m, false, "ดารารัตน์", "สุขสำราญ", "0866666666", 150m, 164m, 1200m, 1255m),
            new("203", 3500m, false, "ประเสริฐ", "ดีเลิศ", "0877777777", 90m, 105m, 750m, 810m),
            new("204", 3500m, true, null, null, null, 0m, 0m, 50m, 50m),
            new("205", 3500m, false, null, null, null, 0m, 0m, 0m, 0m)
        };

        foreach (var data in seedData)
        {
            var room = createRoomService.Create(data.Number, data.Rent, data.Exempt);

            if (!string.IsNullOrWhiteSpace(data.FirstName) && !string.IsNullOrWhiteSpace(data.LastName))
            {
                createTenantService.Create(data.FirstName, data.LastName, data.Phone, room.Id);
            }

            try
            {
                saveMeterReadingService.Save(
                    room.Id,
                    currentPeriod,
                    MeterReading.Create(data.PrevWater),
                    MeterReading.Create(data.CurrWater),
                    MeterReading.Create(data.PrevElec),
                    MeterReading.Create(data.CurrElec));
            }
            catch
            {
                // ข้ามหากมีข้อผิดพลาดซ้ำซ้อนตอน seed
            }
        }
    }
}
