using MannaHp.Shared.Entities;
using MannaHp.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace MannaHp.PrintAgent.Data;

public class PrintAgentDbContext : DbContext
{
	public PrintAgentDbContext(DbContextOptions<PrintAgentDbContext> options) : base(options) { }

	public DbSet<Order> Orders => Set<Order>();
	public DbSet<OrderItem> OrderItems => Set<OrderItem>();
	public DbSet<OrderItemIngredient> OrderItemIngredients => Set<OrderItemIngredient>();
	public DbSet<MenuItem> MenuItems => Set<MenuItem>();
	public DbSet<MenuItemVariant> MenuItemVariants => Set<MenuItemVariant>();
	public DbSet<Ingredient> Ingredients => Set<Ingredient>();
	public DbSet<AppSettings> AppSettings => Set<AppSettings>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Order>(e =>
		{
			e.ToTable("orders");
			e.Property(o => o.Subtotal).HasPrecision(10, 2);
			e.Property(o => o.TaxRate).HasPrecision(5, 4);
			e.Property(o => o.Tax).HasPrecision(10, 2);
			e.Property(o => o.Total).HasPrecision(10, 2);
		});

		modelBuilder.Entity<OrderItem>(e =>
		{
			e.ToTable("order_items");
			e.Property(oi => oi.UnitPrice).HasPrecision(10, 2);
			e.Property(oi => oi.TotalPrice).HasPrecision(10, 2);

			e.HasOne(oi => oi.Order).WithMany(o => o.Items).HasForeignKey(oi => oi.OrderId);
			e.HasOne(oi => oi.MenuItem).WithMany().HasForeignKey(oi => oi.MenuItemId);
			e.HasOne(oi => oi.Variant).WithMany().HasForeignKey(oi => oi.VariantId);
		});

		modelBuilder.Entity<OrderItemIngredient>(e =>
		{
			e.ToTable("order_item_ingredients");
			e.Property(oii => oii.QuantityUsed).HasPrecision(10, 4);
			e.Property(oii => oii.PriceCharged).HasPrecision(10, 2);

			e.HasOne(oii => oii.OrderItem).WithMany(oi => oi.Ingredients).HasForeignKey(oii => oii.OrderItemId);
			e.HasOne(oii => oii.Ingredient).WithMany().HasForeignKey(oii => oii.IngredientId);
		});

		modelBuilder.Entity<MenuItem>(e =>
		{
			e.ToTable("menu_items");
			e.Property(m => m.Categoryid).HasColumnName("category_id");
		});

		modelBuilder.Entity<MenuItemVariant>(e =>
		{
			e.ToTable("menu_item_variants");
			e.Property(v => v.Sortorder).HasColumnName("sort_order");
			e.HasOne(v => v.MenuItem).WithMany(m => m.Variants).HasForeignKey(v => v.MenuItemId);
		});

		modelBuilder.Entity<Ingredient>(e =>
		{
			e.ToTable("ingredients");
			e.Property(i => i.CostPerUnit).HasPrecision(10, 4);
			e.Property(i => i.StockQuantity).HasPrecision(10, 2);
			e.Property(i => i.LowStockThreshold).HasPrecision(10, 2);
		});

		modelBuilder.Entity<AppSettings>(e =>
		{
			e.ToTable("app_settings");
		});

		// Ignore entities we don't need
		modelBuilder.Entity<Category>().ToTable("categories");
		modelBuilder.Entity<RecipeIngredient>().ToTable("recipe_ingredients");
		modelBuilder.Entity<RecipeIngredient>(e =>
		{
			e.Property(r => r.Quantity).HasPrecision(10, 4);
			e.HasOne(r => r.Variant).WithMany(v => v.RecipeIngredients).HasForeignKey(r => r.VariantId);
			e.HasOne(r => r.Ingredient).WithMany().HasForeignKey(r => r.IngredientId);
		});
		modelBuilder.Entity<MenuItemAvailableIngredient>(e =>
		{
			e.ToTable("menu_item_available_ingredients");
			e.Property(a => a.CustomerPrice).HasPrecision(10, 2);
			e.Property(a => a.QuantityUsed).HasPrecision(10, 4);
			e.HasOne(a => a.MenuItem).WithMany(m => m.AvailableIngredients).HasForeignKey(a => a.MenuItemId);
			e.HasOne(a => a.Ingredient).WithMany().HasForeignKey(a => a.IngredientId);
		});
	}
}
