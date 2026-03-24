using BookLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Data;

// =============================================================================
// EF CORE: LibraryDbContext
// =============================================================================
// DbContext is the heart of Entity Framework Core. Every database operation
// flows through it. It is responsible for:
//
//   1. CHANGE TRACKING: It keeps a snapshot of every entity you loaded and
//      detects what changed when you call SaveChangesAsync(). It then generates
//      the minimal SQL (INSERT / UPDATE / DELETE) to sync those changes.
//
//   2. QUERY TRANSLATION: LINQ expressions like .Where(), .Include(), .OrderBy()
//      are translated into SQL at runtime. The query is NOT executed in C# —
//      it runs in the database engine.
//
//   3. CONNECTION MANAGEMENT: DbContext opens and closes database connections
//      automatically. You never call connection.Open() yourself.
//
//   4. TRANSACTION COORDINATION: SaveChangesAsync() wraps all pending changes in
//      a single transaction. Either all changes commit or none do.
//
// LIFETIME — ALWAYS SCOPED:
//   DbContext is NOT thread-safe. It must be registered as SCOPED (one instance
//   per HTTP request) so that concurrent requests each get their own instance.
//   ASP.NET Core's AddDbContext() registers it as Scoped by default.
//   NEVER register DbContext as Singleton — you will get race conditions and
//   "second operation started" exceptions under concurrent load.
// =============================================================================

public class LibraryDbContext : DbContext
{
    // Constructor injection: ASP.NET Core's DI container creates the DbContextOptions
    // from your appsettings.json connection string and injects it here.
    // You never create LibraryDbContext with 'new' — let the DI container do it.
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    // ==========================================================================
    // DbSET PROPERTIES
    // ==========================================================================
    // Each DbSet<T> represents one database table. EF Core uses the property
    // name to determine the table name (by convention it pluralises it):
    //   Books   → "Books"   table
    //   Authors → "Authors" table
    //   Categories → "Categories" table
    //
    // You query through DbSets:
    //   var books = await _db.Books.Where(b => b.IsAvailable).ToListAsync();
    //
    // Set<T>() is equivalent to a named DbSet property — it's just more explicit.
    // ==========================================================================

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();

    // ==========================================================================
    // FLUENT API CONFIGURATION
    // ==========================================================================
    // OnModelCreating runs once at startup to build the "model" — EF Core's
    // internal map of your entities to database tables/columns.
    //
    // FLUENT API vs DATA ANNOTATIONS:
    //   Data Annotations ([Required], [MaxLength]) are applied on your model classes.
    //   Fluent API is applied here in OnModelCreating.
    //
    //   Fluent API WINS if both conflict. It's more powerful because it can
    //   configure things that have no annotation equivalent (e.g., table splitting,
    //   owned types, complex relationships). Many teams prefer Fluent API because
    //   it keeps model classes free of infrastructure concerns.
    // ==========================================================================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------------------------------------------------------
        // BOOK CONFIGURATION
        // ---------------------------------------------------------------
        modelBuilder.Entity<Book>(entity =>
        {
            // Fluent API: configure the Price column type for SQLite.
            // This is an example of something you CANNOT do with Data Annotations —
            // specifying the raw column type string.
            entity.Property(b => b.Price)
                  .HasColumnType("TEXT");

            // Create an index on Title for faster searches.
            // This adds a CREATE INDEX statement to the migration.
            entity.HasIndex(b => b.Title)
                  .HasDatabaseName("IX_Books_Title");

            // RELATIONSHIP CONFIGURATION (explicit, overriding conventions):
            // One Author has many Books. If the Author is deleted, what happens
            // to their Books? Options:
            //   - Cascade: delete all Books (dangerous — use carefully)
            //   - Restrict: block the Author deletion (safer — enforces business rules)
            //   - SetNull: set AuthorId to null (only if FK is nullable)
            //
            // Here we use Restrict so the LibraryService business rule
            // ("can't delete author with books") is enforced at the DB level too.
            entity.HasOne(b => b.Author)
                  .WithMany(a => a.Books)
                  .HasForeignKey(b => b.AuthorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Category)
                  .WithMany(c => c.Books)
                  .HasForeignKey(b => b.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------------------------------------------------------
        // SEED DATA
        // ---------------------------------------------------------------
        // HasData() tells EF Core to INSERT these rows when the migration is applied.
        // IDs MUST be explicit (e.g., Id = 1) — EF Core needs stable IDs to track
        // whether seed rows already exist across migrations. It cannot auto-generate
        // them because they must be deterministic.
        //
        // Seed data is great for development (always have data to work with) but
        // for production you often prefer a separate seeding script or a first-run
        // check in Program.cs.

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Fiction",        Description = "Novels, short stories, and other fictional works." },
            new Category { Id = 2, Name = "Non-Fiction",    Description = "Factual writing including biographies, history, and essays." },
            new Category { Id = 3, Name = "Science",        Description = "Natural sciences, technology, and mathematics." },
            new Category { Id = 4, Name = "Fantasy",        Description = "Speculative fiction with magical or supernatural elements." },
            new Category { Id = 5, Name = "Biography",      Description = "Life stories of real people." }
        );

        modelBuilder.Entity<Author>().HasData(
            new Author { Id = 1, FirstName = "Frank",      LastName = "Herbert",  BirthDate = new DateTime(1920, 10, 8),
                         Biography = "American science fiction author, best known for the Dune series." },
            new Author { Id = 2, FirstName = "J.R.R.",     LastName = "Tolkien",  BirthDate = new DateTime(1892, 1, 3),
                         Biography = "English author and philologist, creator of Middle-earth." },
            new Author { Id = 3, FirstName = "George",     LastName = "Orwell",   BirthDate = new DateTime(1903, 6, 25),
                         Biography = "English novelist and essayist known for 1984 and Animal Farm." }
        );

        modelBuilder.Entity<Book>().HasData(
            new Book { Id = 1, Title = "Dune",              AuthorId = 1, CategoryId = 3, ISBN = "9780441172719",
                       PublishedYear = 1965, PageCount = 412, Price = 14.99m, IsAvailable = true,
                       Description = "A sweeping tale of politics, religion, and ecology set on the desert planet Arrakis." },
            new Book { Id = 2, Title = "The Lord of the Rings", AuthorId = 2, CategoryId = 4, ISBN = "9780618640157",
                       PublishedYear = 1954, PageCount = 1178, Price = 24.99m, IsAvailable = true,
                       Description = "The epic quest to destroy the One Ring and defeat the Dark Lord Sauron." },
            new Book { Id = 3, Title = "1984",              AuthorId = 3, CategoryId = 1, ISBN = "9780451524935",
                       PublishedYear = 1949, PageCount = 328, Price = 12.99m, IsAvailable = true,
                       Description = "A chilling dystopian vision of a totalitarian society under constant surveillance." }
        );
    }
}
