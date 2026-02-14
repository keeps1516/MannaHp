using MannaHp.Shared.Entities;
using MannaHp.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace MannaHp.Server.Data;

public class MannaDbContext : DbContext
{
    public MannaDbContext(DbContextOptions<MannaDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemVariant> MenuItemVariants => Set<MenuItemVariant>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<MenuItemAvailableIngredient> MenuItemAvailableIngredients => Set<MenuItemAvailableIngredient>();

	public DbSet<Order> Orders => Set<Order>();
	public DbSet<OrderItem> OrderItems => Set<OrderItem>();
	public DbSet<OrderItemIngredient> OrderItemIngredients => Set<OrderItemIngredient>();
	public DbSet<AppSettings> AppSettings => Set<AppSettings>();


	protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Category ──
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.SortOrder).HasDefaultValue(0);
            e.Property(c => c.Active).HasDefaultValue(true);
            e.Property(c => c.CreatedAt).HasDefaultValueSql("now()");

            e.HasData(
                new Category { Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001"), Name = "Bowls", SortOrder = 1, Active = true },
                new Category { Id = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000002"), Name = "Traditional Drinks", SortOrder = 2, Active = true },
                new Category { Id = Guid.Parse("a1b2c3d4-0003-0000-0000-000000000003"), Name = "Seasonal Specials", SortOrder = 3, Active = true },
                new Category { Id = Guid.Parse("a1b2c3d4-0004-0000-0000-000000000004"), Name = "Sides & Drinks", SortOrder = 4, Active = true },
                new Category { Id = Guid.Parse("a1b2c3d4-0005-0000-0000-000000000005"), Name = "Add-Ons", SortOrder = 5, Active = true }
            );
        });

        // ── Ingredient ──
        modelBuilder.Entity<Ingredient>(e =>
        {
            e.ToTable("ingredients");
            e.HasKey(i => i.Id);
            e.Property(i => i.Name).HasMaxLength(100).IsRequired();
            e.Property(i => i.Unit).IsRequired();
            e.Property(i => i.CostPerUnit).HasPrecision(10, 4);
            e.Property(i => i.StockQuantity).HasPrecision(10, 2).HasDefaultValue(0m);
            e.Property(i => i.LowStockThreshold).HasPrecision(10, 2);
            e.Property(i => i.Active).HasDefaultValue(true);
            e.Property(i => i.CreatedAt).HasDefaultValueSql("now()");
        });

        // ── MenuItem ──
        modelBuilder.Entity<MenuItem>(e =>
        {
            e.ToTable("menu_items");
            e.HasKey(m => m.Id);
            e.Property(m => m.Categoryid).HasColumnName("category_id");
            e.Property(m => m.Name).HasMaxLength(100).IsRequired();
            e.Property(m => m.IsCustomizable).HasDefaultValue(false);
            e.Property(m => m.Active).HasDefaultValue(true);
            e.Property(m => m.SortOrder).HasDefaultValue(0);
            e.Property(m => m.CreatedAt).HasDefaultValueSql("now()");

            e.HasOne<Category>()
                .WithMany(c => c.MenuItems)
                .HasForeignKey(m => m.Categoryid);
        });

        // ── MenuItemVariant ──
        modelBuilder.Entity<MenuItemVariant>(e =>
        {
            e.ToTable("menu_item_variants");
            e.HasKey(v => v.Id);
            e.Property(v => v.Name).HasMaxLength(50).IsRequired();
            e.Property(v => v.Price).HasPrecision(10, 2).HasDefaultValue(0m);
            e.Property(v => v.Sortorder).HasColumnName("sort_order").HasDefaultValue(0);
            e.Property(v => v.Active).HasDefaultValue(true);

            e.HasOne(v => v.MenuItem)
                .WithMany(m => m.Variants)
                .HasForeignKey(v => v.MenuItemId);
        });

        // ── RecipeIngredient ──
        modelBuilder.Entity<RecipeIngredient>(e =>
        {
            e.ToTable("recipe_ingredients");
            e.HasKey(r => r.Id);
            e.Property(r => r.Quantity).HasPrecision(10, 4);

            e.HasOne(r => r.Variant)
                .WithMany(v => v.RecipeIngredients)
                .HasForeignKey(r => r.VariantId);

            e.HasOne(r => r.Ingredient)
                .WithMany()
                .HasForeignKey(r => r.IngredientId);
        });

        // ── MenuItemAvailableIngredient ──
        modelBuilder.Entity<MenuItemAvailableIngredient>(e =>
        {
            e.ToTable("menu_item_available_ingredients");
            e.HasKey(a => a.Id);
            e.Property(a => a.CustomerPrice).HasPrecision(10, 2);
            e.Property(a => a.QuantityUsed).HasPrecision(10, 4);
            e.Property(a => a.IsDefault).HasDefaultValue(false);
            e.Property(a => a.GroupName).HasMaxLength(50).IsRequired();
            e.Property(a => a.SortOrder).HasDefaultValue(0);
            e.Property(a => a.Active).HasDefaultValue(true);

            e.HasOne(a => a.MenuItem)
                .WithMany(m => m.AvailableIngredients)
                .HasForeignKey(a => a.MenuItemId);

            e.HasOne(a => a.Ingredient)
                .WithMany()
                .HasForeignKey(a => a.IngredientId);
        });

		// ── Order ──
		modelBuilder.HasSequence<int>("order_number_seq").StartsAt(1001).IncrementsBy(1);
		modelBuilder.Entity<Order>(e =>
		{
			e.ToTable("orders");
			e.HasKey(o => o.Id);
			e.Property(o => o.OrderNumber).HasDefaultValueSql("nextval('order_number_seq')");
			e.HasIndex(o => o.OrderNumber).IsUnique();
			e.Property(o => o.UserId).HasMaxLength(450);
			e.Property(o => o.Status).HasDefaultValue(OrderStatus.Received);
			e.Property(o => o.PaymentStatus).HasDefaultValue(PaymentStatus.Pending);
			e.Property(o => o.StripePaymentId).HasMaxLength(255);
			e.Property(o => o.CardBrand).HasMaxLength(20);
			e.Property(o => o.CardLast4).HasMaxLength(4);
			e.Property(o => o.Subtotal).HasPrecision(10, 2);
			e.Property(o => o.TaxRate).HasPrecision(5, 4);
			e.Property(o => o.Tax).HasPrecision(10, 2);
			e.Property(o => o.Total).HasPrecision(10, 2);
			e.Property(o => o.Printed).HasDefaultValue(false);
			e.Property(o => o.CreatedAt).HasDefaultValueSql("now()");
			e.Property(o => o.UpdatedAt).HasDefaultValueSql("now()");
		});

		// ── OrderItem ──
		modelBuilder.Entity<OrderItem>(e =>
		{
			e.ToTable("order_items");
			e.HasKey(oi => oi.Id);
			e.Property(oi => oi.Quantity).HasDefaultValue(1);
			e.Property(oi => oi.UnitPrice).HasPrecision(10, 2);
			e.Property(oi => oi.TotalPrice).HasPrecision(10, 2);

			e.HasOne(oi => oi.Order)
				.WithMany(o => o.Items)
				.HasForeignKey(oi => oi.OrderId);

			e.HasOne(oi => oi.MenuItem)
				.WithMany()
				.HasForeignKey(oi => oi.MenuItemId);

			e.HasOne(oi => oi.Variant)
				.WithMany()
				.HasForeignKey(oi => oi.VariantId);
		});

		// ── OrderItemIngredient ──
		modelBuilder.Entity<OrderItemIngredient>(e =>
		{
			e.ToTable("order_item_ingredients");
			e.HasKey(oii => oii.Id);
			e.Property(oii => oii.QuantityUsed).HasPrecision(10, 4);
			e.Property(oii => oii.PriceCharged).HasPrecision(10, 2);

			e.HasOne(oii => oii.OrderItem)
				.WithMany(oi => oi.Ingredients)
				.HasForeignKey(oii => oii.OrderItemId);

			e.HasOne(oii => oii.Ingredient)
				.WithMany()
				.HasForeignKey(oii => oii.IngredientId);
		});

		// ── AppSettings ──
		modelBuilder.Entity<AppSettings>(e =>
		{
			e.ToTable("app_settings");
			e.HasKey(s => s.Id);
			e.Property(s => s.Key).HasMaxLength(100).IsRequired();
			e.HasIndex(s => s.Key).IsUnique();
			e.Property(s => s.Value).HasMaxLength(500).IsRequired();

			e.HasData(
				new AppSettings { Id = Guid.Parse("e0000000-0001-0000-0000-000000000001"), Key = "StoreName", Value = "Manna + HP" },
				new AppSettings { Id = Guid.Parse("e0000000-0002-0000-0000-000000000002"), Key = "StoreAddress", Value = "317 S Main St" },
				new AppSettings { Id = Guid.Parse("e0000000-0003-0000-0000-000000000003"), Key = "StoreCity", Value = "Lindsay, OK 73052" },
				new AppSettings { Id = Guid.Parse("e0000000-0004-0000-0000-000000000004"), Key = "StorePhone", Value = "(405) 208-2271" },
				new AppSettings { Id = Guid.Parse("e0000000-0005-0000-0000-000000000005"), Key = "DefaultTaxRate", Value = "0.0825" },
				new AppSettings { Id = Guid.Parse("e0000000-0006-0000-0000-000000000006"), Key = "ReceiptFooter", Value = "Our pleasure to serve you!" }
			);
		});

		// ── Seed Data ──
		SeedData.Seed(modelBuilder);
	}
}
