using MannaHp.Shared.Entities;
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
    }
}
