using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL bağlantı dizesi
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<NoteDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Veritabanını otomatik oluştur
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NoteDbContext>();
    db.Database.EnsureCreated();
}

// NOT: CRUD endpointleri

// Tüm notları getir
app.MapGet("/", async (NoteDbContext db) =>
    await db.Notes.ToListAsync());

// Yeni not ekle
app.MapPost("/add", async (NoteDbContext db, Note note) =>
{
    note = note with { CreatedAt = DateTime.UtcNow };
    db.Notes.Add(note);
    await db.SaveChangesAsync();
    return Results.Ok(note);
});

// Notu güncelle
app.MapPut("/update/{id}", async (NoteDbContext db, int id, Note input) =>
{
    var existing = await db.Notes.FindAsync(id);
    if (existing is null) return Results.NotFound();

    var updated = input with { Id = id, CreatedAt = existing.CreatedAt };
    db.Entry(existing).CurrentValues.SetValues(updated);
    await db.SaveChangesAsync();

    return Results.Ok(updated);
});

// Notu sil
app.MapDelete("/delete/{id}", async (NoteDbContext db, int id) =>
{
    var note = await db.Notes.FindAsync(id);
    if (note is null) return Results.NotFound();

    db.Notes.Remove(note);
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.Run();

// Model: Note
record Note(int Id, string Title, string Content, DateTime CreatedAt);

// DbContext
class NoteDbContext : DbContext
{
    public NoteDbContext(DbContextOptions<NoteDbContext> options) : base(options) { }

    public DbSet<Note> Notes => Set<Note>();
}
