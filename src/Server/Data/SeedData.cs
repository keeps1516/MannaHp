using MannaHp.Shared.Entities;
using MannaHp.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace MannaHp.Server.Data;

public static class SeedData
{
    // ── Category IDs ──
    private static readonly Guid CatBowls = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    private static readonly Guid CatDrinks = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000002");
    private static readonly Guid CatSeasonal = Guid.Parse("a1b2c3d4-0003-0000-0000-000000000003");
    private static readonly Guid CatSides = Guid.Parse("a1b2c3d4-0004-0000-0000-000000000004");
    private static readonly Guid CatAddons = Guid.Parse("a1b2c3d4-0005-0000-0000-000000000005");

    // ── Ingredient IDs ──
    private static readonly Guid IngJRice = Guid.Parse("b0000000-0001-0000-0000-000000000001");
    private static readonly Guid IngBeans = Guid.Parse("b0000000-0002-0000-0000-000000000002");
    private static readonly Guid IngGBeef = Guid.Parse("b0000000-0003-0000-0000-000000000003");
    private static readonly Guid IngChicken = Guid.Parse("b0000000-0004-0000-0000-000000000004");
    private static readonly Guid IngSausQueso = Guid.Parse("b0000000-0005-0000-0000-000000000005");
    private static readonly Guid IngLettuce = Guid.Parse("b0000000-0006-0000-0000-000000000006");
    private static readonly Guid IngTomatoes = Guid.Parse("b0000000-0007-0000-0000-000000000007");
    private static readonly Guid IngJalapenos = Guid.Parse("b0000000-0008-0000-0000-000000000008");
    private static readonly Guid IngSalsa = Guid.Parse("b0000000-0009-0000-0000-000000000009");
    private static readonly Guid IngShrCheese = Guid.Parse("b0000000-000a-0000-0000-000000000010");
    private static readonly Guid IngChips = Guid.Parse("b0000000-000b-0000-0000-000000000011");
    private static readonly Guid IngLgQueso = Guid.Parse("b0000000-000c-0000-0000-000000000012");
    private static readonly Guid IngCoffee = Guid.Parse("b0000000-000d-0000-0000-000000000013");
    private static readonly Guid IngEspresso = Guid.Parse("b0000000-000e-0000-0000-000000000014");
    private static readonly Guid IngMilk = Guid.Parse("b0000000-000f-0000-0000-000000000015");
    private static readonly Guid IngAltMilk = Guid.Parse("b0000000-0010-0000-0000-000000000016");
    private static readonly Guid IngChocolate = Guid.Parse("b0000000-0011-0000-0000-000000000017");
    private static readonly Guid IngWhtChoc = Guid.Parse("b0000000-0012-0000-0000-000000000018");
    private static readonly Guid IngCaramel = Guid.Parse("b0000000-0013-0000-0000-000000000019");
    private static readonly Guid IngFlvSyrup = Guid.Parse("b0000000-0014-0000-0000-000000000020");
    private static readonly Guid IngChai = Guid.Parse("b0000000-0015-0000-0000-000000000021");
    private static readonly Guid IngSmoothie = Guid.Parse("b0000000-0016-0000-0000-000000000022");
    private static readonly Guid IngHotChocMix = Guid.Parse("b0000000-0017-0000-0000-000000000023");
    private static readonly Guid IngTea = Guid.Parse("b0000000-0018-0000-0000-000000000024");
    private static readonly Guid IngWhipCream = Guid.Parse("b0000000-0019-0000-0000-000000000025");
    private static readonly Guid IngIceCream = Guid.Parse("b0000000-001a-0000-0000-000000000026");
    private static readonly Guid IngCider = Guid.Parse("b0000000-001b-0000-0000-000000000027");
    private static readonly Guid IngCinnamon = Guid.Parse("b0000000-001c-0000-0000-000000000028");
    private static readonly Guid IngPumpkin = Guid.Parse("b0000000-001d-0000-0000-000000000029");
    private static readonly Guid IngMaple = Guid.Parse("b0000000-001e-0000-0000-000000000030");
    private static readonly Guid IngMarshmallow = Guid.Parse("b0000000-001f-0000-0000-000000000031");
    private static readonly Guid IngPeppermint = Guid.Parse("b0000000-0020-0000-0000-000000000032");
    private static readonly Guid IngGingerbread = Guid.Parse("b0000000-0021-0000-0000-000000000033");
    private static readonly Guid IngIce = Guid.Parse("b0000000-0022-0000-0000-000000000034");

    // ── MenuItem IDs ──
    private static readonly Guid MiBowl = Guid.Parse("c0000000-0001-0000-0000-000000000001");
    private static readonly Guid MiDrip = Guid.Parse("c0000000-0002-0000-0000-000000000002");
    private static readonly Guid MiAuLait = Guid.Parse("c0000000-0003-0000-0000-000000000003");
    private static readonly Guid MiEspresso = Guid.Parse("c0000000-0004-0000-0000-000000000004");
    private static readonly Guid MiAmericano = Guid.Parse("c0000000-0005-0000-0000-000000000005");
    private static readonly Guid MiCaraMac = Guid.Parse("c0000000-0006-0000-0000-000000000006");
    private static readonly Guid MiCortado = Guid.Parse("c0000000-0007-0000-0000-000000000007");
    private static readonly Guid MiCappuccino = Guid.Parse("c0000000-0008-0000-0000-000000000008");
    private static readonly Guid MiLatte = Guid.Parse("c0000000-0009-0000-0000-000000000009");
    private static readonly Guid MiMocha = Guid.Parse("c0000000-000a-0000-0000-000000000010");
    private static readonly Guid MiWhtMocha = Guid.Parse("c0000000-000b-0000-0000-000000000011");
    private static readonly Guid MiFlvLatte = Guid.Parse("c0000000-000c-0000-0000-000000000012");
    private static readonly Guid MiCaraLatte = Guid.Parse("c0000000-000d-0000-0000-000000000013");
    private static readonly Guid MiIcedCoffee = Guid.Parse("c0000000-000e-0000-0000-000000000014");
    private static readonly Guid MiColdBrew = Guid.Parse("c0000000-000f-0000-0000-000000000015");
    private static readonly Guid MiIcedLatte = Guid.Parse("c0000000-0010-0000-0000-000000000016");
    private static readonly Guid MiAffogato = Guid.Parse("c0000000-0011-0000-0000-000000000017");
    private static readonly Guid MiBlendMocha = Guid.Parse("c0000000-0012-0000-0000-000000000018");
    private static readonly Guid MiBlendCaramel = Guid.Parse("c0000000-0013-0000-0000-000000000019");
    private static readonly Guid MiSmoothie = Guid.Parse("c0000000-0014-0000-0000-000000000020");
    private static readonly Guid MiHotChoc = Guid.Parse("c0000000-0015-0000-0000-000000000021");
    private static readonly Guid MiSteamer = Guid.Parse("c0000000-0016-0000-0000-000000000022");
    private static readonly Guid MiTea = Guid.Parse("c0000000-0017-0000-0000-000000000023");
    private static readonly Guid MiChaiLatte = Guid.Parse("c0000000-0018-0000-0000-000000000024");
    private static readonly Guid MiPumpkin = Guid.Parse("c0000000-0019-0000-0000-000000000025");
    private static readonly Guid MiMaple = Guid.Parse("c0000000-001a-0000-0000-000000000026");
    private static readonly Guid MiMarshmallow = Guid.Parse("c0000000-001b-0000-0000-000000000027");
    private static readonly Guid MiPepMocha = Guid.Parse("c0000000-001c-0000-0000-000000000028");
    private static readonly Guid MiGingerbread = Guid.Parse("c0000000-001d-0000-0000-000000000029");
    private static readonly Guid MiAppleCider = Guid.Parse("c0000000-001e-0000-0000-000000000030");
    private static readonly Guid MiChips = Guid.Parse("c0000000-001f-0000-0000-000000000031");
    private static readonly Guid MiChipsQueso = Guid.Parse("c0000000-0020-0000-0000-000000000032");
    private static readonly Guid MiFlvShot = Guid.Parse("c0000000-0021-0000-0000-000000000033");
    private static readonly Guid MiEspShot = Guid.Parse("c0000000-0022-0000-0000-000000000034");
    private static readonly Guid MiWhipCream = Guid.Parse("c0000000-0023-0000-0000-000000000035");
    private static readonly Guid MiAltMilk = Guid.Parse("c0000000-0024-0000-0000-000000000036");

    // ── Variant IDs (d0 prefix) ──
    private static readonly Guid VBowlReg = Guid.Parse("d0000000-0001-0000-0000-000000000001");
    private static readonly Guid VDrip12 = Guid.Parse("d0000000-0002-0000-0000-000000000002");
    private static readonly Guid VDrip16 = Guid.Parse("d0000000-0003-0000-0000-000000000003");
    private static readonly Guid VDripPot = Guid.Parse("d0000000-0004-0000-0000-000000000004");
    private static readonly Guid VAuLait12 = Guid.Parse("d0000000-0005-0000-0000-000000000005");
    private static readonly Guid VAuLait16 = Guid.Parse("d0000000-0006-0000-0000-000000000006");
    private static readonly Guid VEspSingle = Guid.Parse("d0000000-0007-0000-0000-000000000007");
    private static readonly Guid VEspDouble = Guid.Parse("d0000000-0008-0000-0000-000000000008");
    private static readonly Guid VAmericano12 = Guid.Parse("d0000000-0009-0000-0000-000000000009");
    private static readonly Guid VAmericano16 = Guid.Parse("d0000000-000a-0000-0000-000000000010");
    private static readonly Guid VCaraMac12 = Guid.Parse("d0000000-000b-0000-0000-000000000011");
    private static readonly Guid VCaraMac16 = Guid.Parse("d0000000-000c-0000-0000-000000000012");
    private static readonly Guid VCortado12 = Guid.Parse("d0000000-000d-0000-0000-000000000013");
    private static readonly Guid VCortado8 = Guid.Parse("d0000000-000e-0000-0000-000000000014");
    private static readonly Guid VCappuccino6 = Guid.Parse("d0000000-000f-0000-0000-000000000015");
    private static readonly Guid VCappuccino8 = Guid.Parse("d0000000-0010-0000-0000-000000000016");
    private static readonly Guid VLatte12 = Guid.Parse("d0000000-0011-0000-0000-000000000017");
    private static readonly Guid VLatte16 = Guid.Parse("d0000000-0012-0000-0000-000000000018");
    private static readonly Guid VMocha12 = Guid.Parse("d0000000-0013-0000-0000-000000000019");
    private static readonly Guid VMocha16 = Guid.Parse("d0000000-0014-0000-0000-000000000020");
    private static readonly Guid VWhtMocha12 = Guid.Parse("d0000000-0015-0000-0000-000000000021");
    private static readonly Guid VWhtMocha16 = Guid.Parse("d0000000-0016-0000-0000-000000000022");
    private static readonly Guid VFlvLatte12 = Guid.Parse("d0000000-0017-0000-0000-000000000023");
    private static readonly Guid VFlvLatte16 = Guid.Parse("d0000000-0018-0000-0000-000000000024");
    private static readonly Guid VCaraLatte12 = Guid.Parse("d0000000-0019-0000-0000-000000000025");
    private static readonly Guid VCaraLatte16 = Guid.Parse("d0000000-001a-0000-0000-000000000026");
    private static readonly Guid VIcedCoffee12 = Guid.Parse("d0000000-001b-0000-0000-000000000027");
    private static readonly Guid VIcedCoffee16 = Guid.Parse("d0000000-001c-0000-0000-000000000028");
    private static readonly Guid VColdBrew12 = Guid.Parse("d0000000-001d-0000-0000-000000000029");
    private static readonly Guid VColdBrew16 = Guid.Parse("d0000000-001e-0000-0000-000000000030");
    private static readonly Guid VIcedLatte12 = Guid.Parse("d0000000-001f-0000-0000-000000000031");
    private static readonly Guid VIcedLatte16 = Guid.Parse("d0000000-0020-0000-0000-000000000032");
    private static readonly Guid VAffogato8 = Guid.Parse("d0000000-0021-0000-0000-000000000033");
    private static readonly Guid VBlendMocha16 = Guid.Parse("d0000000-0022-0000-0000-000000000034");
    private static readonly Guid VBlendMocha24 = Guid.Parse("d0000000-0023-0000-0000-000000000035");
    private static readonly Guid VBlendCaramel16 = Guid.Parse("d0000000-0024-0000-0000-000000000036");
    private static readonly Guid VBlendCaramel24 = Guid.Parse("d0000000-0025-0000-0000-000000000037");
    private static readonly Guid VSmoothie16 = Guid.Parse("d0000000-0026-0000-0000-000000000038");
    private static readonly Guid VSmoothie24 = Guid.Parse("d0000000-0027-0000-0000-000000000039");
    private static readonly Guid VHotChoc4 = Guid.Parse("d0000000-0028-0000-0000-000000000040");
    private static readonly Guid VHotChoc12 = Guid.Parse("d0000000-0029-0000-0000-000000000041");
    private static readonly Guid VHotChoc16 = Guid.Parse("d0000000-002a-0000-0000-000000000042");
    private static readonly Guid VSteamer12 = Guid.Parse("d0000000-002b-0000-0000-000000000043");
    private static readonly Guid VSteamer16 = Guid.Parse("d0000000-002c-0000-0000-000000000044");
    private static readonly Guid VTea12 = Guid.Parse("d0000000-002d-0000-0000-000000000045");
    private static readonly Guid VTea16 = Guid.Parse("d0000000-002e-0000-0000-000000000046");
    private static readonly Guid VTeaPot = Guid.Parse("d0000000-002f-0000-0000-000000000047");
    private static readonly Guid VChaiLatte12 = Guid.Parse("d0000000-0030-0000-0000-000000000048");
    private static readonly Guid VChaiLatte16 = Guid.Parse("d0000000-0031-0000-0000-000000000049");
    private static readonly Guid VPumpkin12 = Guid.Parse("d0000000-0032-0000-0000-000000000050");
    private static readonly Guid VPumpkin16 = Guid.Parse("d0000000-0033-0000-0000-000000000051");
    private static readonly Guid VMaple12 = Guid.Parse("d0000000-0034-0000-0000-000000000052");
    private static readonly Guid VMaple16 = Guid.Parse("d0000000-0035-0000-0000-000000000053");
    private static readonly Guid VMarshmallow12 = Guid.Parse("d0000000-0036-0000-0000-000000000054");
    private static readonly Guid VMarshmallow16 = Guid.Parse("d0000000-0037-0000-0000-000000000055");
    private static readonly Guid VPepMocha12 = Guid.Parse("d0000000-0038-0000-0000-000000000056");
    private static readonly Guid VPepMocha16 = Guid.Parse("d0000000-0039-0000-0000-000000000057");
    private static readonly Guid VGingerbread12 = Guid.Parse("d0000000-003a-0000-0000-000000000058");
    private static readonly Guid VGingerbread16 = Guid.Parse("d0000000-003b-0000-0000-000000000059");
    private static readonly Guid VAppleCider12 = Guid.Parse("d0000000-003c-0000-0000-000000000060");
    private static readonly Guid VAppleCider16 = Guid.Parse("d0000000-003d-0000-0000-000000000061");
    private static readonly Guid VChips = Guid.Parse("d0000000-003e-0000-0000-000000000062");
    private static readonly Guid VChipsQueso = Guid.Parse("d0000000-003f-0000-0000-000000000063");
    private static readonly Guid VFlvShot = Guid.Parse("d0000000-0040-0000-0000-000000000064");
    private static readonly Guid VEspShot = Guid.Parse("d0000000-0041-0000-0000-000000000065");
    private static readonly Guid VWhipCream = Guid.Parse("d0000000-0042-0000-0000-000000000066");
    private static readonly Guid VAltMilk = Guid.Parse("d0000000-0043-0000-0000-000000000067");

    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedIngredients(modelBuilder);
        SeedMenuItems(modelBuilder);
        SeedVariants(modelBuilder);
        SeedAvailableIngredients(modelBuilder);
        SeedRecipeIngredients(modelBuilder);
    }

    private static void SeedIngredients(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>().HasData(
            Ing(IngJRice, "Jasmine Rice", UnitOfMeasure.Oz, 0.0400m, 500, 80),
            Ing(IngBeans, "Beans", UnitOfMeasure.Oz, 0.0350m, 400, 64),
            Ing(IngGBeef, "Ground Beef", UnitOfMeasure.Oz, 0.3750m, 300, 48),
            Ing(IngChicken, "Chicken", UnitOfMeasure.Oz, 0.3125m, 400, 64),
            Ing(IngSausQueso, "Sausage Queso", UnitOfMeasure.Oz, 0.3500m, 200, 32),
            Ing(IngLettuce, "Lettuce", UnitOfMeasure.Oz, 0.0625m, 200, 32),
            Ing(IngTomatoes, "Tomatoes", UnitOfMeasure.Oz, 0.1250m, 200, 32),
            Ing(IngJalapenos, "Fresh Jalapenos", UnitOfMeasure.Oz, 0.0750m, 150, 24),
            Ing(IngSalsa, "Fresh Salsa", UnitOfMeasure.Oz, 0.1000m, 200, 32),
            Ing(IngShrCheese, "Shredded Cheese", UnitOfMeasure.Oz, 0.2500m, 200, 32),
            Ing(IngChips, "Tortilla Chips", UnitOfMeasure.Oz, 0.0800m, 300, 48),
            Ing(IngLgQueso, "Large Queso", UnitOfMeasure.Oz, 0.1500m, 200, 32),
            Ing(IngCoffee, "Coffee (brewed)", UnitOfMeasure.Oz, 0.0500m, 600, 96),
            Ing(IngEspresso, "Espresso", UnitOfMeasure.Shot, 0.3000m, 500, 80),
            Ing(IngMilk, "Whole Milk", UnitOfMeasure.Oz, 0.0350m, 500, 80),
            Ing(IngAltMilk, "Alternative Milk", UnitOfMeasure.Oz, 0.0625m, 300, 48),
            Ing(IngChocolate, "Chocolate Syrup", UnitOfMeasure.Oz, 0.1500m, 200, 32),
            Ing(IngWhtChoc, "White Choc Syrup", UnitOfMeasure.Oz, 0.1500m, 150, 24),
            Ing(IngCaramel, "Caramel Syrup", UnitOfMeasure.Oz, 0.1500m, 200, 32),
            Ing(IngFlvSyrup, "Flavored Syrup", UnitOfMeasure.Oz, 0.1200m, 200, 32),
            Ing(IngChai, "Chai Concentrate", UnitOfMeasure.Oz, 0.1000m, 200, 32),
            Ing(IngSmoothie, "Smoothie Base", UnitOfMeasure.Oz, 0.0800m, 200, 32),
            Ing(IngHotChocMix, "Hot Chocolate Mix", UnitOfMeasure.Oz, 0.1200m, 200, 32),
            Ing(IngTea, "Tea (sachet)", UnitOfMeasure.Each, 0.2000m, 300, 50),
            Ing(IngWhipCream, "Whipped Cream", UnitOfMeasure.Oz, 0.1000m, 150, 24),
            Ing(IngIceCream, "Vanilla Ice Cream", UnitOfMeasure.Oz, 0.1500m, 100, 16),
            Ing(IngCider, "Apple Cider", UnitOfMeasure.Oz, 0.0800m, 200, 32),
            Ing(IngCinnamon, "Cinnamon", UnitOfMeasure.Tsp, 0.0500m, 100, 16),
            Ing(IngPumpkin, "Pumpkin Spice Syrup", UnitOfMeasure.Oz, 0.1500m, 100, 16),
            Ing(IngMaple, "Maple Brown Sugar Syrup", UnitOfMeasure.Oz, 0.1500m, 100, 16),
            Ing(IngMarshmallow, "Marshmallow Syrup", UnitOfMeasure.Oz, 0.1500m, 100, 16),
            Ing(IngPeppermint, "Peppermint Syrup", UnitOfMeasure.Oz, 0.1500m, 100, 16),
            Ing(IngGingerbread, "Gingerbread Syrup", UnitOfMeasure.Oz, 0.1500m, 100, 16),
            Ing(IngIce, "Ice", UnitOfMeasure.Oz, 0.0100m, 9999, 0)
        );
    }

    private static void SeedMenuItems(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuItem>().HasData(
            Mi(MiBowl, CatBowls, "Burrito Bowl", "Build your own bowl", true, 1),
            Mi(MiDrip, CatDrinks, "Drip Coffee", "Fresh batch brew", false, 1),
            Mi(MiAuLait, CatDrinks, "Café au Lait", "½ drip + ½ steamed milk", false, 2),
            Mi(MiEspresso, CatDrinks, "Espresso", "Shot of espresso", false, 3),
            Mi(MiAmericano, CatDrinks, "Americano", "Espresso + hot water", false, 4),
            Mi(MiCaraMac, CatDrinks, "Caramel Macchiato", "Layered caramel espresso drink", false, 5),
            Mi(MiCortado, CatDrinks, "Cortado", "1:1 espresso : steamed milk", false, 6),
            Mi(MiCappuccino, CatDrinks, "Cappuccino", "Espresso + airy foam", false, 7),
            Mi(MiLatte, CatDrinks, "Latte", "Espresso + steamed milk", false, 8),
            Mi(MiMocha, CatDrinks, "Mocha", "Latte + chocolate syrup", false, 9),
            Mi(MiWhtMocha, CatDrinks, "White Mocha", "Latte + white chocolate syrup", false, 10),
            Mi(MiFlvLatte, CatDrinks, "Flavored Latte", "Latte + flavored syrup", false, 11),
            Mi(MiCaraLatte, CatDrinks, "Caramel Latte", "Latte + caramel syrup", false, 12),
            Mi(MiIcedCoffee, CatDrinks, "Iced Coffee", "Chilled drip over ice", false, 13),
            Mi(MiColdBrew, CatDrinks, "Cold Brew", "16-18 hr brew, over ice", false, 14),
            Mi(MiIcedLatte, CatDrinks, "Iced Latte", "Espresso + milk over ice", false, 15),
            Mi(MiAffogato, CatDrinks, "Affogato", "Espresso over vanilla ice cream", false, 16),
            Mi(MiBlendMocha, CatDrinks, "Blended Mocha", "Espresso, milk, chocolate, ice", false, 17),
            Mi(MiBlendCaramel, CatDrinks, "Blended Caramel", "Espresso, milk, caramel, ice", false, 18),
            Mi(MiSmoothie, CatDrinks, "Smoothie", "Smoothie base + fruit flavor", false, 19),
            Mi(MiHotChoc, CatDrinks, "Hot Chocolate", "Steamed milk + chocolate", false, 20),
            Mi(MiSteamer, CatDrinks, "Steamer", "Steamed milk + syrup", false, 21),
            Mi(MiTea, CatDrinks, "Tea", "Premium sachet or pot", false, 22),
            Mi(MiChaiLatte, CatDrinks, "Chai Latte", "Chai + steamed milk", false, 23),
            Mi(MiPumpkin, CatSeasonal, "Pumpkin Spice Latte", "Latte + pumpkin spice syrup + whipped cream", false, 1),
            Mi(MiMaple, CatSeasonal, "Maple Brown Sugar Latte", "Latte + maple brown sugar syrup + whipped cream", false, 2),
            Mi(MiMarshmallow, CatSeasonal, "Toasted Marshmallow Mocha", "Mocha + toasted marshmallow syrup", false, 3),
            Mi(MiPepMocha, CatSeasonal, "Peppermint Mocha", "Mocha + peppermint syrup + whipped cream", false, 4),
            Mi(MiGingerbread, CatSeasonal, "Gingerbread Latte", "Latte + gingerbread syrup", false, 5),
            Mi(MiAppleCider, CatSeasonal, "Hot Apple Cider", "Steamed cider + cinnamon", false, 6),
            Mi(MiChips, CatSides, "Side of Chips", null, false, 1),
            Mi(MiChipsQueso, CatSides, "Chips & Salsa with 13oz Large Queso", null, false, 2),
            Mi(MiFlvShot, CatAddons, "Flavor Shot", null, false, 1),
            Mi(MiEspShot, CatAddons, "Espresso Shot", null, false, 2),
            Mi(MiWhipCream, CatAddons, "Whipped Cream", null, false, 3),
            Mi(MiAltMilk, CatAddons, "Alternative Milk", null, false, 4)
        );
    }

    private static void SeedVariants(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuItemVariant>().HasData(
            Var(VBowlReg, MiBowl, "Regular", 0.00m, 1),
            Var(VDrip12, MiDrip, "12oz", 2.25m, 1), Var(VDrip16, MiDrip, "16oz", 2.75m, 2), Var(VDripPot, MiDrip, "Pot", 4.00m, 3),
            Var(VAuLait12, MiAuLait, "12oz", 3.50m, 1), Var(VAuLait16, MiAuLait, "16oz", 4.00m, 2),
            Var(VEspSingle, MiEspresso, "Single", 2.00m, 1), Var(VEspDouble, MiEspresso, "Double", 3.00m, 2),
            Var(VAmericano12, MiAmericano, "12oz", 3.25m, 1), Var(VAmericano16, MiAmericano, "16oz", 3.75m, 2),
            Var(VCaraMac12, MiCaraMac, "12oz", 5.50m, 1), Var(VCaraMac16, MiCaraMac, "16oz", 6.00m, 2),
            Var(VCortado12, MiCortado, "12oz", 2.25m, 1), Var(VCortado8, MiCortado, "8oz", 2.75m, 2),
            Var(VCappuccino6, MiCappuccino, "6oz", 4.25m, 1), Var(VCappuccino8, MiCappuccino, "8oz", 4.50m, 2),
            Var(VLatte12, MiLatte, "12oz", 4.75m, 1), Var(VLatte16, MiLatte, "16oz", 5.25m, 2),
            Var(VMocha12, MiMocha, "12oz", 5.25m, 1), Var(VMocha16, MiMocha, "16oz", 5.75m, 2),
            Var(VWhtMocha12, MiWhtMocha, "12oz", 5.25m, 1), Var(VWhtMocha16, MiWhtMocha, "16oz", 5.75m, 2),
            Var(VFlvLatte12, MiFlvLatte, "12oz", 5.00m, 1), Var(VFlvLatte16, MiFlvLatte, "16oz", 5.50m, 2),
            Var(VCaraLatte12, MiCaraLatte, "12oz", 5.25m, 1), Var(VCaraLatte16, MiCaraLatte, "16oz", 5.75m, 2),
            Var(VIcedCoffee12, MiIcedCoffee, "12oz", 2.25m, 1), Var(VIcedCoffee16, MiIcedCoffee, "16oz", 2.75m, 2),
            Var(VColdBrew12, MiColdBrew, "12oz", 4.25m, 1), Var(VColdBrew16, MiColdBrew, "16oz", 4.75m, 2),
            Var(VIcedLatte12, MiIcedLatte, "12oz", 5.25m, 1), Var(VIcedLatte16, MiIcedLatte, "16oz", 5.75m, 2),
            Var(VAffogato8, MiAffogato, "8oz", 6.50m, 1),
            Var(VBlendMocha16, MiBlendMocha, "16oz", 5.75m, 1), Var(VBlendMocha24, MiBlendMocha, "24oz", 6.50m, 2),
            Var(VBlendCaramel16, MiBlendCaramel, "16oz", 5.75m, 1), Var(VBlendCaramel24, MiBlendCaramel, "24oz", 6.50m, 2),
            Var(VSmoothie16, MiSmoothie, "16oz", 5.50m, 1), Var(VSmoothie24, MiSmoothie, "24oz", 6.25m, 2),
            Var(VHotChoc4, MiHotChoc, "4oz", 1.50m, 1), Var(VHotChoc12, MiHotChoc, "12oz", 3.50m, 2), Var(VHotChoc16, MiHotChoc, "16oz", 4.00m, 3),
            Var(VSteamer12, MiSteamer, "12oz", 3.25m, 1), Var(VSteamer16, MiSteamer, "16oz", 3.75m, 2),
            Var(VTea12, MiTea, "12oz", 2.25m, 1), Var(VTea16, MiTea, "16oz", 2.75m, 2), Var(VTeaPot, MiTea, "Pot", 4.00m, 3),
            Var(VChaiLatte12, MiChaiLatte, "12oz", 4.75m, 1), Var(VChaiLatte16, MiChaiLatte, "16oz", 5.25m, 2),
            Var(VPumpkin12, MiPumpkin, "12oz", 5.75m, 1), Var(VPumpkin16, MiPumpkin, "16oz", 6.25m, 2),
            Var(VMaple12, MiMaple, "12oz", 5.75m, 1), Var(VMaple16, MiMaple, "16oz", 6.25m, 2),
            Var(VMarshmallow12, MiMarshmallow, "12oz", 5.75m, 1), Var(VMarshmallow16, MiMarshmallow, "16oz", 6.25m, 2),
            Var(VPepMocha12, MiPepMocha, "12oz", 5.75m, 1), Var(VPepMocha16, MiPepMocha, "16oz", 6.25m, 2),
            Var(VGingerbread12, MiGingerbread, "12oz", 5.75m, 1), Var(VGingerbread16, MiGingerbread, "16oz", 6.25m, 2),
            Var(VAppleCider12, MiAppleCider, "12oz", 4.50m, 1), Var(VAppleCider16, MiAppleCider, "16oz", 5.00m, 2),
            Var(VChips, MiChips, "Regular", 1.50m, 1),
            Var(VChipsQueso, MiChipsQueso, "Regular", 6.00m, 1),
            Var(VFlvShot, MiFlvShot, "Regular", 0.75m, 1),
            Var(VEspShot, MiEspShot, "Regular", 1.00m, 1),
            Var(VWhipCream, MiWhipCream, "Regular", 0.50m, 1),
            Var(VAltMilk, MiAltMilk, "Regular", 0.75m, 1)
        );
    }

    private static void SeedAvailableIngredients(ModelBuilder modelBuilder)
    {
        var id = 1;
        modelBuilder.Entity<MenuItemAvailableIngredient>().HasData(
            // ── Bowl ingredients ──
            Avail(id++, MiBowl, IngJRice, 3.00m, 10.0m, "Bases", 1),
            Avail(id++, MiBowl, IngBeans, 2.00m, 5.0m, "Bases", 2),
            Avail(id++, MiBowl, IngGBeef, 3.00m, 6.0m, "Proteins", 1),
            Avail(id++, MiBowl, IngChicken, 3.00m, 8.0m, "Proteins", 2),
            Avail(id++, MiBowl, IngSausQueso, 3.00m, 6.0m, "Proteins", 3),
            Avail(id++, MiBowl, IngLettuce, 0.50m, 2.0m, "Fresh Toppings", 1),
            Avail(id++, MiBowl, IngTomatoes, 0.50m, 2.0m, "Fresh Toppings", 2),
            Avail(id++, MiBowl, IngJalapenos, 0.25m, 1.0m, "Fresh Toppings", 3),
            Avail(id++, MiBowl, IngSalsa, 0.50m, 2.0m, "Fresh Toppings", 4),
            Avail(id++, MiBowl, IngShrCheese, 0.50m, 1.5m, "Fresh Toppings", 5),

            // ── Drink add-ons ──
            // Latte
            Avail(id++, MiLatte, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiLatte, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiLatte, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Cappuccino
            Avail(id++, MiCappuccino, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiCappuccino, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiCappuccino, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Americano
            Avail(id++, MiAmericano, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiAmericano, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            // Mocha
            Avail(id++, MiMocha, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiMocha, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiMocha, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // White Mocha
            Avail(id++, MiWhtMocha, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiWhtMocha, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiWhtMocha, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Caramel Macchiato
            Avail(id++, MiCaraMac, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiCaraMac, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiCaraMac, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Flavored Latte
            Avail(id++, MiFlvLatte, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiFlvLatte, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiFlvLatte, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Caramel Latte
            Avail(id++, MiCaraLatte, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiCaraLatte, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiCaraLatte, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Cortado
            Avail(id++, MiCortado, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiCortado, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Café au Lait
            Avail(id++, MiAuLait, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiAuLait, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Iced Latte
            Avail(id++, MiIcedLatte, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiIcedLatte, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiIcedLatte, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Cold Brew
            Avail(id++, MiColdBrew, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiColdBrew, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiColdBrew, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Iced Coffee
            Avail(id++, MiIcedCoffee, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiIcedCoffee, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            // Blended Mocha
            Avail(id++, MiBlendMocha, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiBlendMocha, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            // Blended Caramel
            Avail(id++, MiBlendCaramel, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiBlendCaramel, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            // Chai Latte
            Avail(id++, MiChaiLatte, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiChaiLatte, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiChaiLatte, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Hot Chocolate
            Avail(id++, MiHotChoc, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiHotChoc, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiHotChoc, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Steamer
            Avail(id++, MiSteamer, IngWhipCream, 0.50m, 1.5m, "Extras", 1),
            Avail(id++, MiSteamer, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Drip Coffee
            Avail(id++, MiDrip, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiDrip, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            // Seasonal — Pumpkin Spice Latte
            Avail(id++, MiPumpkin, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiPumpkin, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiPumpkin, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Seasonal — Maple Brown Sugar Latte
            Avail(id++, MiMaple, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiMaple, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiMaple, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Seasonal — Toasted Marshmallow Mocha
            Avail(id++, MiMarshmallow, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiMarshmallow, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiMarshmallow, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Seasonal — Peppermint Mocha
            Avail(id++, MiPepMocha, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiPepMocha, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiPepMocha, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1),
            // Seasonal — Gingerbread Latte
            Avail(id++, MiGingerbread, IngEspresso, 1.00m, 1.0m, "Extras", 1),
            Avail(id++, MiGingerbread, IngWhipCream, 0.50m, 1.5m, "Extras", 2),
            Avail(id++, MiGingerbread, IngAltMilk, 0.75m, 2.0m, "Milk Substitute", 1)
        );
    }

    private static void SeedRecipeIngredients(ModelBuilder modelBuilder)
    {
        var id = 1;
        modelBuilder.Entity<RecipeIngredient>().HasData(
            // Drip Coffee
            Rcp(id++, VDrip12, IngCoffee, 12.0m), Rcp(id++, VDrip16, IngCoffee, 16.0m), Rcp(id++, VDripPot, IngCoffee, 48.0m),
            // Café au Lait
            Rcp(id++, VAuLait12, IngCoffee, 6.0m), Rcp(id++, VAuLait12, IngMilk, 6.0m),
            Rcp(id++, VAuLait16, IngCoffee, 8.0m), Rcp(id++, VAuLait16, IngMilk, 8.0m),
            // Espresso
            Rcp(id++, VEspSingle, IngEspresso, 1.0m), Rcp(id++, VEspDouble, IngEspresso, 2.0m),
            // Americano
            Rcp(id++, VAmericano12, IngEspresso, 2.0m), Rcp(id++, VAmericano16, IngEspresso, 3.0m),
            // Latte
            Rcp(id++, VLatte12, IngEspresso, 2.0m), Rcp(id++, VLatte12, IngMilk, 10.0m),
            Rcp(id++, VLatte16, IngEspresso, 2.0m), Rcp(id++, VLatte16, IngMilk, 14.0m),
            // Mocha
            Rcp(id++, VMocha12, IngEspresso, 2.0m), Rcp(id++, VMocha12, IngMilk, 8.0m), Rcp(id++, VMocha12, IngChocolate, 2.0m),
            Rcp(id++, VMocha16, IngEspresso, 2.0m), Rcp(id++, VMocha16, IngMilk, 12.0m), Rcp(id++, VMocha16, IngChocolate, 2.0m),
            // White Mocha
            Rcp(id++, VWhtMocha12, IngEspresso, 2.0m), Rcp(id++, VWhtMocha12, IngMilk, 8.0m), Rcp(id++, VWhtMocha12, IngWhtChoc, 2.0m),
            Rcp(id++, VWhtMocha16, IngEspresso, 2.0m), Rcp(id++, VWhtMocha16, IngMilk, 12.0m), Rcp(id++, VWhtMocha16, IngWhtChoc, 2.0m),
            // Caramel Macchiato
            Rcp(id++, VCaraMac12, IngEspresso, 2.0m), Rcp(id++, VCaraMac12, IngMilk, 8.0m), Rcp(id++, VCaraMac12, IngCaramel, 2.0m),
            Rcp(id++, VCaraMac16, IngEspresso, 2.0m), Rcp(id++, VCaraMac16, IngMilk, 12.0m), Rcp(id++, VCaraMac16, IngCaramel, 2.0m),
            // Caramel Latte
            Rcp(id++, VCaraLatte12, IngEspresso, 2.0m), Rcp(id++, VCaraLatte12, IngMilk, 8.0m), Rcp(id++, VCaraLatte12, IngCaramel, 2.0m),
            Rcp(id++, VCaraLatte16, IngEspresso, 2.0m), Rcp(id++, VCaraLatte16, IngMilk, 12.0m), Rcp(id++, VCaraLatte16, IngCaramel, 2.0m),
            // Flavored Latte
            Rcp(id++, VFlvLatte12, IngEspresso, 2.0m), Rcp(id++, VFlvLatte12, IngMilk, 8.0m), Rcp(id++, VFlvLatte12, IngFlvSyrup, 1.0m),
            Rcp(id++, VFlvLatte16, IngEspresso, 2.0m), Rcp(id++, VFlvLatte16, IngMilk, 12.0m), Rcp(id++, VFlvLatte16, IngFlvSyrup, 1.5m),
            // Iced Coffee
            Rcp(id++, VIcedCoffee12, IngCoffee, 8.0m), Rcp(id++, VIcedCoffee12, IngIce, 4.0m),
            Rcp(id++, VIcedCoffee16, IngCoffee, 12.0m), Rcp(id++, VIcedCoffee16, IngIce, 4.0m),
            // Cold Brew
            Rcp(id++, VColdBrew12, IngCoffee, 12.0m), Rcp(id++, VColdBrew12, IngIce, 4.0m),
            Rcp(id++, VColdBrew16, IngCoffee, 16.0m), Rcp(id++, VColdBrew16, IngIce, 4.0m),
            // Iced Latte
            Rcp(id++, VIcedLatte12, IngEspresso, 2.0m), Rcp(id++, VIcedLatte12, IngMilk, 8.0m), Rcp(id++, VIcedLatte12, IngIce, 4.0m),
            Rcp(id++, VIcedLatte16, IngEspresso, 2.0m), Rcp(id++, VIcedLatte16, IngMilk, 12.0m), Rcp(id++, VIcedLatte16, IngIce, 4.0m),
            // Affogato
            Rcp(id++, VAffogato8, IngEspresso, 2.0m), Rcp(id++, VAffogato8, IngIceCream, 4.0m),
            // Blended Mocha
            Rcp(id++, VBlendMocha16, IngEspresso, 2.0m), Rcp(id++, VBlendMocha16, IngMilk, 8.0m), Rcp(id++, VBlendMocha16, IngChocolate, 2.0m), Rcp(id++, VBlendMocha16, IngIce, 6.0m),
            Rcp(id++, VBlendMocha24, IngEspresso, 3.0m), Rcp(id++, VBlendMocha24, IngMilk, 12.0m), Rcp(id++, VBlendMocha24, IngChocolate, 3.0m), Rcp(id++, VBlendMocha24, IngIce, 8.0m),
            // Blended Caramel
            Rcp(id++, VBlendCaramel16, IngEspresso, 2.0m), Rcp(id++, VBlendCaramel16, IngMilk, 8.0m), Rcp(id++, VBlendCaramel16, IngCaramel, 2.0m), Rcp(id++, VBlendCaramel16, IngIce, 6.0m),
            Rcp(id++, VBlendCaramel24, IngEspresso, 3.0m), Rcp(id++, VBlendCaramel24, IngMilk, 12.0m), Rcp(id++, VBlendCaramel24, IngCaramel, 3.0m), Rcp(id++, VBlendCaramel24, IngIce, 8.0m),
            // Smoothie
            Rcp(id++, VSmoothie16, IngSmoothie, 12.0m), Rcp(id++, VSmoothie16, IngIce, 6.0m),
            Rcp(id++, VSmoothie24, IngSmoothie, 18.0m), Rcp(id++, VSmoothie24, IngIce, 8.0m),
            // Hot Chocolate
            Rcp(id++, VHotChoc4, IngMilk, 3.0m), Rcp(id++, VHotChoc4, IngHotChocMix, 1.0m),
            Rcp(id++, VHotChoc12, IngMilk, 10.0m), Rcp(id++, VHotChoc12, IngHotChocMix, 2.0m),
            Rcp(id++, VHotChoc16, IngMilk, 14.0m), Rcp(id++, VHotChoc16, IngHotChocMix, 2.0m),
            // Steamer
            Rcp(id++, VSteamer12, IngMilk, 10.0m), Rcp(id++, VSteamer12, IngFlvSyrup, 1.0m),
            Rcp(id++, VSteamer16, IngMilk, 14.0m), Rcp(id++, VSteamer16, IngFlvSyrup, 1.5m),
            // Tea
            Rcp(id++, VTea12, IngTea, 1.0m), Rcp(id++, VTea16, IngTea, 1.0m), Rcp(id++, VTeaPot, IngTea, 3.0m),
            // Chai Latte
            Rcp(id++, VChaiLatte12, IngChai, 6.0m), Rcp(id++, VChaiLatte12, IngMilk, 6.0m),
            Rcp(id++, VChaiLatte16, IngChai, 8.0m), Rcp(id++, VChaiLatte16, IngMilk, 8.0m),
            // Cortado
            Rcp(id++, VCortado12, IngEspresso, 2.0m), Rcp(id++, VCortado12, IngMilk, 2.0m),
            Rcp(id++, VCortado8, IngEspresso, 2.0m), Rcp(id++, VCortado8, IngMilk, 2.0m),
            // Cappuccino
            Rcp(id++, VCappuccino6, IngEspresso, 2.0m), Rcp(id++, VCappuccino6, IngMilk, 4.0m),
            Rcp(id++, VCappuccino8, IngEspresso, 2.0m), Rcp(id++, VCappuccino8, IngMilk, 6.0m),
            // Pumpkin Spice Latte
            Rcp(id++, VPumpkin12, IngEspresso, 2.0m), Rcp(id++, VPumpkin12, IngMilk, 8.0m), Rcp(id++, VPumpkin12, IngPumpkin, 1.5m), Rcp(id++, VPumpkin12, IngWhipCream, 1.0m),
            Rcp(id++, VPumpkin16, IngEspresso, 2.0m), Rcp(id++, VPumpkin16, IngMilk, 12.0m), Rcp(id++, VPumpkin16, IngPumpkin, 2.0m), Rcp(id++, VPumpkin16, IngWhipCream, 1.5m),
            // Maple Brown Sugar Latte
            Rcp(id++, VMaple12, IngEspresso, 2.0m), Rcp(id++, VMaple12, IngMilk, 8.0m), Rcp(id++, VMaple12, IngMaple, 1.5m), Rcp(id++, VMaple12, IngWhipCream, 1.0m),
            Rcp(id++, VMaple16, IngEspresso, 2.0m), Rcp(id++, VMaple16, IngMilk, 12.0m), Rcp(id++, VMaple16, IngMaple, 2.0m), Rcp(id++, VMaple16, IngWhipCream, 1.5m),
            // Toasted Marshmallow Mocha
            Rcp(id++, VMarshmallow12, IngEspresso, 2.0m), Rcp(id++, VMarshmallow12, IngMilk, 8.0m), Rcp(id++, VMarshmallow12, IngChocolate, 2.0m), Rcp(id++, VMarshmallow12, IngMarshmallow, 1.0m),
            Rcp(id++, VMarshmallow16, IngEspresso, 2.0m), Rcp(id++, VMarshmallow16, IngMilk, 12.0m), Rcp(id++, VMarshmallow16, IngChocolate, 2.0m), Rcp(id++, VMarshmallow16, IngMarshmallow, 1.5m),
            // Peppermint Mocha
            Rcp(id++, VPepMocha12, IngEspresso, 2.0m), Rcp(id++, VPepMocha12, IngMilk, 8.0m), Rcp(id++, VPepMocha12, IngChocolate, 2.0m), Rcp(id++, VPepMocha12, IngPeppermint, 1.0m), Rcp(id++, VPepMocha12, IngWhipCream, 1.0m),
            Rcp(id++, VPepMocha16, IngEspresso, 2.0m), Rcp(id++, VPepMocha16, IngMilk, 12.0m), Rcp(id++, VPepMocha16, IngChocolate, 2.0m), Rcp(id++, VPepMocha16, IngPeppermint, 1.5m), Rcp(id++, VPepMocha16, IngWhipCream, 1.5m),
            // Gingerbread Latte
            Rcp(id++, VGingerbread12, IngEspresso, 2.0m), Rcp(id++, VGingerbread12, IngMilk, 8.0m), Rcp(id++, VGingerbread12, IngGingerbread, 1.5m),
            Rcp(id++, VGingerbread16, IngEspresso, 2.0m), Rcp(id++, VGingerbread16, IngMilk, 12.0m), Rcp(id++, VGingerbread16, IngGingerbread, 2.0m),
            // Hot Apple Cider
            Rcp(id++, VAppleCider12, IngCider, 12.0m), Rcp(id++, VAppleCider12, IngCinnamon, 0.5m),
            Rcp(id++, VAppleCider16, IngCider, 16.0m), Rcp(id++, VAppleCider16, IngCinnamon, 0.5m),
            // Sides
            Rcp(id++, VChips, IngChips, 4.0m),
            Rcp(id++, VChipsQueso, IngChips, 4.0m), Rcp(id++, VChipsQueso, IngSalsa, 4.0m), Rcp(id++, VChipsQueso, IngLgQueso, 13.0m),
            // Add-Ons
            Rcp(id++, VFlvShot, IngFlvSyrup, 0.5m),
            Rcp(id++, VEspShot, IngEspresso, 1.0m),
            Rcp(id++, VWhipCream, IngWhipCream, 1.5m),
            Rcp(id++, VAltMilk, IngAltMilk, 2.0m)
        );
    }

    // ── Helper methods ──
    private static Ingredient Ing(Guid id, string name, UnitOfMeasure unit, decimal costPerUnit, decimal stock, decimal lowStock) => new()
    {
        Id = id, Name = name, Unit = unit, CostPerUnit = costPerUnit,
        StockQuantity = stock, LowStockThreshold = lowStock, Active = true
    };

    private static MenuItem Mi(Guid id, Guid categoryId, string name, string? desc, bool customizable, int sort) => new()
    {
        Id = id, Categoryid = categoryId, Name = name, Description = desc,
        IsCustomizable = customizable, Active = true, SortOrder = sort
    };

    private static MenuItemVariant Var(Guid id, Guid menuItemId, string name, decimal price, int sort) => new()
    {
        Id = id, MenuItemId = menuItemId, Name = name, Price = price, Sortorder = sort, Active = true
    };

    private static MenuItemAvailableIngredient Avail(int seq, Guid menuItemId, Guid ingredientId, decimal price, decimal qty, string group, int sort) => new()
    {
        Id = Guid.Parse($"e0000000-{seq:D4}-0000-0000-000000000000"),
        MenuItemId = menuItemId, IngredientId = ingredientId,
        CustomerPrice = price, QuantityUsed = qty, GroupName = group,
        SortOrder = sort, Active = true
    };

    private static RecipeIngredient Rcp(int seq, Guid variantId, Guid ingredientId, decimal qty) => new()
    {
        Id = Guid.Parse($"f0000000-{seq:D4}-0000-0000-000000000000"),
        VariantId = variantId, IngredientId = ingredientId, Quantity = qty
    };
}
