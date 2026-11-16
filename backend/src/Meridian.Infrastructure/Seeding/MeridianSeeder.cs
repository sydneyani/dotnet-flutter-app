using Meridian.Application.Abstractions;
using Meridian.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meridian.Infrastructure.Seeding;

public static class MeridianSeeder
{
    public const string DefaultPassword = "Meridian#demo1";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<MeridianDbContext>();
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var hasher = services.GetRequiredService<IPasswordHasher>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var password = configuration["Seed:DemoPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            password = DefaultPassword;
        }

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var hash = hasher.Hash(password);

        db.Users.AddRange(
            UserAccount.Create("maya.okonkwo@meridian.demo", "Maya Okonkwo", hash, UserRole.Technician, now.AddYears(-2), DemoIds.Maya),
            UserAccount.Create("luis.ortega@meridian.demo", "Luis Ortega", hash, UserRole.Technician, now.AddYears(-2), DemoIds.Luis),
            UserAccount.Create("priya.nair@meridian.demo", "Priya Nair", hash, UserRole.Supervisor, now.AddYears(-3), DemoIds.Priya),
            UserAccount.Create("sam.okada@meridian.demo", "Sam Okada", hash, UserRole.Admin, now.AddYears(-4), DemoIds.Sam));

        db.Sites.AddRange(
            Site.Establish("SUB-NR-04", "North Range Substation", "High Plains", "4180 Ridge Line Rd, Carbon County", 41.512, -107.438, DemoIds.NorthRange),
            Site.Establish("GEN-CB-01", "Cedar Bluff Generation Station", "River Basin", "900 Plant Levee Rd, Cedar Bluff", 38.774, -99.912, DemoIds.CedarBluff),
            Site.Establish("CMP-RM-02", "Red Mesa Compressor Station", "Colorado Plateau", "12 Mesa Access Rd, Red Mesa", 37.146, -108.772, DemoIds.RedMesa),
            Site.Establish("SWY-HP-07", "Harbor Point Switching Yard", "Gulf Coast", "55 Wharf Switch Rd, Harbor Point", 29.301, -94.784, DemoIds.HarborPoint));

        var transformer = Asset.Register(DemoIds.NorthRange, "TR-1044", "Main step-up transformer", AssetCategory.Transformer, AssetStatus.InService, AssetCriticality.Critical, "Helios Electric", "HE-230-MVA", "HE-TR-44821", new DateOnly(2014, 6, 2), "230/69 kV step-up bank feeding the North Range 69 kV bus.", today, now.AddYears(-4), DemoIds.Tr1044);
        var breaker = Asset.Register(DemoIds.NorthRange, "CB-220-14", "230 kV line breaker", AssetCategory.CircuitBreaker, AssetStatus.InService, AssetCriticality.High, "Helios Electric", "HE-SF6-245", "HE-CB-22014", new DateOnly(2016, 3, 18), "SF6 breaker on the Cedar Bluff tie line.", today, now.AddYears(-4), DemoIds.Cb220);
        var relay = Asset.Register(DemoIds.NorthRange, "REL-87T", "Transformer differential relay", AssetCategory.ProtectiveRelay, AssetStatus.InService, AssetCriticality.High, "Kestrel Protection", "K87-T", "KP-87T-1904", new DateOnly(2019, 11, 9), "87T differential protection for TR-1044.", today, now.AddYears(-3), DemoIds.Rel87);
        var switchgear = Asset.Register(DemoIds.NorthRange, "SWG-A12", "13.8 kV switchgear lineup A", AssetCategory.Switchgear, AssetStatus.InService, AssetCriticality.Medium, "Helios Electric", "HE-MV-A", "HE-SWG-A12", new DateOnly(2012, 1, 20), "Station service lineup. Infrared scan is on the preventive route.", today, now.AddYears(-4), DemoIds.SwgA12);
        var turbine = Asset.Register(DemoIds.CedarBluff, "GT-02", "Gas turbine unit 2", AssetCategory.Turbine, AssetStatus.InService, AssetCriticality.Critical, "Bracken Turbomachinery", "BT-45", "BT-GT-02-773", new DateOnly(2011, 8, 1), "45 MW simple-cycle turbine. Inlet filtration and combustion inspections are seasonal.", today, now.AddYears(-5), DemoIds.Gt02);
        var pump = Asset.Register(DemoIds.CedarBluff, "PMP-CW-3", "Circulating water pump 3", AssetCategory.Pump, AssetStatus.OutOfService, AssetCriticality.High, "Sable Pumps", "SP-CW-500", "SP-31990", new DateOnly(2015, 5, 12), "Out of service for a mechanical seal replacement.", today, now.AddYears(-4), DemoIds.PumpCw);
        var motor = Asset.Register(DemoIds.CedarBluff, "MOT-CW-3", "Circulating water motor 3", AssetCategory.Motor, AssetStatus.InService, AssetCriticality.Medium, "Sable Pumps", "SP-M-400", "SP-M-8841", new DateOnly(2015, 5, 12), "400 hp motor paired with PMP-CW-3. Vibration route is monthly.", today, now.AddYears(-4), DemoIds.MotorCw);
        var compressor = Asset.Register(DemoIds.RedMesa, "CMP-1", "Main gas compressor", AssetCategory.Compressor, AssetStatus.InService, AssetCriticality.High, "Bracken Turbomachinery", "BT-C-12", "BT-C-5510", new DateOnly(2017, 9, 30), "Two-stage compressor on the Red Mesa discharge header.", today, now.AddYears(-3), DemoIds.Comp1);
        var valve = Asset.Register(DemoIds.RedMesa, "VLV-SUCT-1", "Compressor suction valve", AssetCategory.Valve, AssetStatus.InService, AssetCriticality.Medium, "Northvalve", "NV-12-600", "NV-22018", new DateOnly(2018, 4, 4), "Suction isolation valve. A packing job was cancelled after the leak dried up.", today, now.AddYears(-3), DemoIds.Valve1);
        var meter = Asset.Register(DemoIds.HarborPoint, "MTR-YARD", "Yard revenue meter", AssetCategory.Instrument, AssetStatus.Retired, AssetCriticality.Low, "Kestrel Protection", "KM-200", "KM-00912", new DateOnly(2008, 2, 11), "Retired when the yard was re-metered in 2024. Kept for the nameplate record.", today, now.AddYears(-2), DemoIds.MeterYard);

        db.Assets.AddRange(transformer, breaker, relay, switchgear, turbine, pump, motor, compressor, valve, meter);
        await db.SaveChangesAsync(cancellationToken);

        var oilSample = Open(DemoIds.Wo1, "WO-00001", "Sample insulating oil on TR-1044", "Pull a dissolved-gas sample from the main tank and the tap changer compartment. Hold the sample out of direct sun.", transformer, WorkOrderType.Preventive, WorkOrderPriority.Medium, DemoIds.Maya, DemoIds.Maya, now.AddDays(5), now.AddDays(-2));
        oilSample.AddComment(DemoIds.Priya, "Lab courier stops at North Range on Thursday. Leave the bottles in the control house fridge.", now.AddDays(-1));

        var breakerTiming = Open(DemoIds.Wo2, "WO-00002", "Timing test on CB-220-14", "Travel and timing test after last month's slow close on the Cedar Bluff tie. Compare against the 2024 baseline.", breaker, WorkOrderType.Corrective, WorkOrderPriority.High, DemoIds.Maya, DemoIds.Maya, now.AddDays(1), now.AddDays(-3));
        breakerTiming.TransitionTo(WorkOrderStatus.InProgress, now.AddDays(-1));
        breakerTiming.RecordLabor(1.5m, now.AddHours(-4));
        breakerTiming.AddComment(DemoIds.Maya, "Close coil looks lazy. I am halfway through the travel trace.", now.AddHours(-3));

        var seal = Open(DemoIds.Wo3, "WO-00003", "Replace mechanical seal on PMP-CW-3", "Seal is spraying at the gland. Pump is isolated and tagged. Need the vendor kit before we can finish the fit-up.", pump, WorkOrderType.Corrective, WorkOrderPriority.Urgent, DemoIds.Luis, DemoIds.Luis, now.AddDays(-1), now.AddDays(-6));
        seal.TransitionTo(WorkOrderStatus.InProgress, now.AddDays(-5));
        seal.RecordLabor(2m, now.AddDays(-4));
        seal.TransitionTo(WorkOrderStatus.WaitingParts, now.AddDays(-2));
        seal.AddComment(DemoIds.Luis, "Seal kit is backordered. Vendor quoted a Thursday ship date.", now.AddDays(-1));

        var infrared = Open(DemoIds.Wo4, "WO-00004", "Infrared scan of switchgear SWG-A12", "Quarterly infrared pass on lineup A. Last scan was clean except a warm lug on the station service breaker.", switchgear, WorkOrderType.Inspection, WorkOrderPriority.Low, DemoIds.Priya, null, now.AddDays(14), now.AddDays(-1));

        var borescope = Open(DemoIds.Wo5, "WO-00005", "Borescope GT-02 combustion cans", "Seasonal borescope of all cans before the summer run. Record liner photos against the unit folder.", turbine, WorkOrderType.Preventive, WorkOrderPriority.Medium, DemoIds.Maya, DemoIds.Maya, now.AddDays(-1), now.AddDays(-6));
        borescope.TransitionTo(WorkOrderStatus.InProgress, now.AddDays(-5));
        borescope.TransitionTo(WorkOrderStatus.Completed, now.AddDays(-3), 4.5m);
        borescope.AddComment(DemoIds.Maya, "Can 3 has light coating, nothing that grounds the unit. Photos are in the unit folder.", now.AddDays(-3));

        var flange = Open(DemoIds.Wo6, "WO-00006", "Tighten CMP-1 stage-2 discharge flange", "Weep at the stage-2 discharge flange found on the morning round. Torque and recheck with detection fluid.", compressor, WorkOrderType.Corrective, WorkOrderPriority.High, DemoIds.Luis, DemoIds.Luis, now.AddDays(-8), now.AddDays(-12));
        flange.TransitionTo(WorkOrderStatus.InProgress, now.AddDays(-11));
        flange.TransitionTo(WorkOrderStatus.Completed, now.AddDays(-10), 1.5m);

        var packing = Open(DemoIds.Wo7, "WO-00007", "Repack suction valve VLV-SUCT-1", "Operator reported a damp packing gland. The leak dried up after the unit came off line, so this job was cancelled.", valve, WorkOrderType.Corrective, WorkOrderPriority.Low, DemoIds.Luis, DemoIds.Luis, now.AddDays(3), now.AddDays(-4));
        packing.TransitionTo(WorkOrderStatus.Cancelled, now.AddDays(-2));
        packing.AddComment(DemoIds.Priya, "Cancelled. Recheck the gland on the next start.", now.AddDays(-2));

        var trip = Open(DemoIds.Wo8, "WO-00008", "Investigate 87T alarm on REL-87T", "Control room took a momentary 87T differential alarm during the morning load pickup. Breaker did not trip. Need an event-file pull before the next peak.", relay, WorkOrderType.Emergency, WorkOrderPriority.Urgent, DemoIds.Priya, DemoIds.Maya, now.AddHours(-6), now.AddHours(-8));
        trip.AddComment(DemoIds.Priya, "Maya has the relay laptop. Do not reset targets until the event file is saved.", now.AddHours(-7));

        var vibration = Open(DemoIds.Wo9, "WO-00009", "Vibration route on MOT-CW-3", "Monthly route. Last month the drive-end horizontal was 3.1 mm/s. Watch for a step change while the sister pump is down.", motor, WorkOrderType.Inspection, WorkOrderPriority.Medium, DemoIds.Luis, DemoIds.Luis, now.AddDays(2), now.AddDays(-1));
        vibration.TransitionTo(WorkOrderStatus.InProgress, now.AddHours(-6));
        vibration.RecordLabor(0.5m, now.AddHours(-2));

        var filters = Open(DemoIds.Wo10, "WO-00010", "Replace inlet air filters on GT-02", "Differential pressure is climbing through the pre-filter stage. Filters are in the warehouse count but the frames are backordered.", turbine, WorkOrderType.Preventive, WorkOrderPriority.High, DemoIds.Maya, DemoIds.Maya, now.AddDays(7), now.AddDays(-8));
        filters.TransitionTo(WorkOrderStatus.InProgress, now.AddDays(-7));
        filters.RecordLabor(3m, now.AddDays(-6));
        filters.TransitionTo(WorkOrderStatus.WaitingParts, now.AddDays(-5));

        db.WorkOrders.AddRange(oilSample, breakerTiming, seal, infrared, borescope, flange, packing, trip, vibration, filters);
        await db.SaveChangesAsync(cancellationToken);

        var transformerInspection = Inspection.Record(transformer.Id, null, DemoIds.Maya, now.AddDays(-20), InspectionResult.Pass, "Dielectric screen and visual on the main tank. No new leaks at the radiator headers.", "Oil screen within the station limits.", today.AddDays(70), now, Guid.Parse("dddddddd-0000-4000-8000-000000000001"));
        var turbineInspection = Inspection.Record(turbine.Id, borescope.Id, DemoIds.Priya, now.AddDays(-6), InspectionResult.Conditional, "Walkdown after the borescope. Inlet plenum is clean. Vibration on bearing 2 is worth a shorter interval.", "Bearing 2 vibration 4.6 mm/s, up from 3.9.", today.AddDays(24), now, Guid.Parse("dddddddd-0000-4000-8000-000000000002"));
        var pumpInspection = Inspection.Record(pump.Id, seal.Id, DemoIds.Luis, now.AddDays(-2), InspectionResult.Fail, "Gland leak is active with the pump isolated. Do not return the pump to service on the current seal.", "Mechanical seal spraying at the gland. Out of service.", today.AddDays(2), now, Guid.Parse("dddddddd-0000-4000-8000-000000000003"));
        var breakerInspection = Inspection.Record(breaker.Id, null, DemoIds.Maya, now.AddDays(-40), InspectionResult.Pass, "Annual external inspection. Gas density alarm clear. Cabinet heater working.", null, today.AddDays(300), now, Guid.Parse("dddddddd-0000-4000-8000-000000000004"));
        var compressorInspection = Inspection.Record(compressor.Id, null, DemoIds.Luis, now.AddDays(-45), InspectionResult.Fail, "Stage-2 discharge temperature was high on the round before the flange job.", "Stage-2 discharge temperature above the station limit. Corrected under WO-00006.", today.AddDays(-10), now, Guid.Parse("dddddddd-0000-4000-8000-000000000005"));

        transformer.RecordInspection(transformerInspection.InspectedAt);
        turbine.RecordInspection(turbineInspection.InspectedAt);
        pump.RecordInspection(pumpInspection.InspectedAt);
        breaker.RecordInspection(breakerInspection.InspectedAt);
        compressor.RecordInspection(compressorInspection.InspectedAt);

        db.Inspections.AddRange(transformerInspection, turbineInspection, pumpInspection, breakerInspection, compressorInspection);

        var sequence = await db.NumberSequences.SingleAsync(item => item.Name == NumberSequence.WorkOrder, cancellationToken);
        while (sequence.NextValue < 11)
        {
            sequence.Take();
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static WorkOrder Open(
        Guid id,
        string number,
        string title,
        string description,
        Asset asset,
        WorkOrderType type,
        WorkOrderPriority priority,
        Guid createdBy,
        Guid? assignee,
        DateTimeOffset? dueAt,
        DateTimeOffset createdAt) =>
        WorkOrder.Open(number, title, description, asset.Id, asset.SiteId, type, priority, createdBy, assignee, dueAt, createdAt, id);
}
