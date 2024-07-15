using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(conn))
{
    throw new Exception("Connection string 'DefaultConnection' is not found.");
}

builder.Services.AddEntityFrameworkNpgsql().AddDbContext<ApiDbContext>(options =>
    options.UseNpgsql(conn));

builder.Services.AddTransient<UserService>();

var app = builder.Build();

app.MapGet("Hello", () => "welcome to minimal api");


app.MapGet("/users", (UserService users) => {
    return Results.Ok(users.GetAll());
});

app.MapGet("/users/{id}", (UserService users, int id) => {
    var user = users.GetById(id);

    if (user == null)
        return Results.NotFound("User does not exist");

    return Results.Ok(user);
});

app.MapPost("/users", (UserService users, User newUser) => {
    users.AddUser(newUser);

    return Results.Created($"/users/{newUser.Id}", newUser);
});

app.MapDelete("/users/{id}", (UserService users, int id) => {
    var result = users.DeleteUser(id);

    if (result)
        return Results.NoContent();
    return Results.BadRequest("User not found");
});

app.MapPut("/users/{id}", (UserService users, User updateUser, int id) => {
    var currentUserExist = users.GetById(id);

    if (currentUserExist != null)
    {
        users.UpdateUser(updateUser);
        return Results.Ok(users);
    }
    return Results.BadRequest("User not found");
});

app.Run();

class User
{
    [Column("id")]       
    public int Id { get; set; }

    [Column("mail")]
    public string Mail { get; set; } = string.Empty;

    [Column("password")]
    public string Password { get; set; } = string.Empty;
    
    [Column("username")]
    public string UserName { get; set; } = string.Empty;

    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("birthdate")]
    public DateTime Birthdate { get; set; }
    
    [Column("rights")]
    public int Rights { get; set; }

    [Column("creation_date")]
    public DateTime CreationDate { get; set; }

    [Column("deletion_date")]
    public DateTime? DeletionDate { get; set; }
}

class ApiDbContext : DbContext
{
    public virtual DbSet<User> Users { get; set; }

    public ApiDbContext(DbContextOptions<ApiDbContext> options)
        : base(options)
    {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("users");
        base.OnModelCreating(modelBuilder);
    }
}

class UserService
{
    private readonly ApiDbContext _dbContext;

    public UserService(ApiDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public IEnumerable<User> GetAll() => _dbContext.Users.ToList();

    public User? GetById(int id) => _dbContext.Users.FirstOrDefault(x => x.Id == id);

    public void AddUser(User user) 
    {
        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
    }
    
    public bool DeleteUser(int id) {
        var userForDeletion = _dbContext.Users.FirstOrDefault(x => x.Id == id);

        if (userForDeletion != null)
        {
            _dbContext.Users.Remove(userForDeletion);
            _dbContext.SaveChanges();
            return true;
        }
        return false;
    }

    public void UpdateUser(User userForUpdate)
    {
        var currentUser = _dbContext.Users.FirstOrDefault(x => x.Id == userForUpdate.Id);

        if (currentUser != null)
        {
            currentUser.FirstName = userForUpdate.FirstName;
            currentUser.LastName = userForUpdate.LastName;
            _dbContext.SaveChanges();
        }
    }
}