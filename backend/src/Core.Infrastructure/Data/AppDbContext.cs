using Microsoft.EntityFrameworkCore;
using MyIS.Core.Domain.Organization;
using MyIS.Core.Domain.Users;
using MyIS.Core.Domain.Requests.Entities;
 
namespace MyIS.Core.Infrastructure.Data;
 
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
 
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Employee> Employees => Set<Employee>();
 
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<RequestType> RequestTypes => Set<RequestType>();
    public DbSet<RequestStatus> RequestStatuses => Set<RequestStatus>();
    public DbSet<RequestLine> RequestLines => Set<RequestLine>();
    public DbSet<RequestTransition> RequestTransitions => Set<RequestTransition>();
    public DbSet<RequestHistory> RequestHistory => Set<RequestHistory>();
    public DbSet<RequestComment> RequestComments => Set<RequestComment>();
    public DbSet<RequestAttachment> RequestAttachments => Set<RequestAttachment>();
 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
 
        modelBuilder.HasDefaultSchema("core");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
