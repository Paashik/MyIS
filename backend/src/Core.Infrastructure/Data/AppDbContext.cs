using Microsoft.EntityFrameworkCore;
using MyIS.Core.Domain.Customers.Entities;
using MyIS.Core.Domain.Organization;
using MyIS.Core.Domain.Users;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Domain.Statuses.Entities;
using MyIS.Core.Infrastructure.Data.Entities.Integration;
 
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
    public DbSet<CustomerOrder> CustomerOrders => Set<CustomerOrder>();
 
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<RequestType> RequestTypes => Set<RequestType>();
    public DbSet<RequestStatus> RequestStatuses => Set<RequestStatus>();
    public DbSet<RequestLine> RequestLines => Set<RequestLine>();
    public DbSet<RequestTransition> RequestTransitions => Set<RequestTransition>();
    public DbSet<RequestHistory> RequestHistory => Set<RequestHistory>();
    public DbSet<RequestComment> RequestComments => Set<RequestComment>();
    public DbSet<RequestAttachment> RequestAttachments => Set<RequestAttachment>();

    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemGroup> ItemGroups => Set<ItemGroup>();
    public DbSet<ItemAttribute> ItemAttributes => Set<ItemAttribute>();
    public DbSet<ItemAttributeValue> ItemAttributeValues => Set<ItemAttributeValue>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<ItemSequence> ItemSequences => Set<ItemSequence>();
    public DbSet<Counterparty> Counterparties => Set<Counterparty>();
    public DbSet<CounterpartyRole> CounterpartyRoles => Set<CounterpartyRole>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<BodyType> BodyTypes => Set<BodyType>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<TechnicalParameter> TechnicalParameters => Set<TechnicalParameter>();
    public DbSet<ParameterSet> ParameterSets => Set<ParameterSet>();
    public DbSet<Symbol> Symbols => Set<Symbol>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<ExternalEntityLink> ExternalEntityLinks => Set<ExternalEntityLink>();

    public DbSet<Component2020Connection> Component2020Connections => Set<Component2020Connection>();
    public DbSet<Component2020SyncRun> Component2020SyncRuns => Set<Component2020SyncRun>();
    public DbSet<Component2020SyncError> Component2020SyncErrors => Set<Component2020SyncError>();
    public DbSet<Component2020SyncCursor> Component2020SyncCursors => Set<Component2020SyncCursor>();
    public DbSet<Component2020SyncSchedule> Component2020SyncSchedules => Set<Component2020SyncSchedule>();
 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
 
        modelBuilder.HasDefaultSchema("core");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
