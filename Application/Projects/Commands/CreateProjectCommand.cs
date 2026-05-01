using Domain.Entities;

public class CreateProjectCommand
{
    public string Name { get; set; }
}

public class CreateProjectCommandHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public CreateProjectCommandHandler(IUnitOfWork unitOfWork, ITenantContext tenantContext)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Project> Handle(CreateProjectCommand request)
    {
        var project = new Project
        {
            Name = request.Name,
            TenantId = _tenantContext.TenantId
        };

        await _unitOfWork.Projects.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        return project;
    }
}
