using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<NoteDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NoteDbContext>();
    db.Database.EnsureCreated();
}




app.MapGet("/", async (NoteDbContext db) =>
    await db.Notes.ToListAsync());


app.MapPost("/add", async (NoteDbContext db, Note note) =>
{
    note = note with { CreatedAt = DateTime.UtcNow };
    db.Notes.Add(note);
    await db.SaveChangesAsync();
    return Results.Ok(note);
});


app.MapPut("/update/{id}", async (NoteDbContext db, int id, Note input) =>
{
    var existing = await db.Notes.FindAsync(id);
    if (existing is null) return Results.NotFound();

    var updated = input with { Id = id, CreatedAt = existing.CreatedAt };
    db.Entry(existing).CurrentValues.SetValues(updated);
    await db.SaveChangesAsync();

    return Results.Ok(updated);
});


app.MapDelete("/delete/{id}", async (NoteDbContext db, int id) =>
{
    var note = await db.Notes.FindAsync(id);
    if (note is null) return Results.NotFound();

    db.Notes.Remove(note);
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.Run();


record Note(int Id, string Title, string Content, DateTime CreatedAt);


class NoteDbContext : DbContext
{
    public NoteDbContext(DbContextOptions<NoteDbContext> options) : base(options) { }

    public DbSet<Note> Notes => Set<Note>();
}
