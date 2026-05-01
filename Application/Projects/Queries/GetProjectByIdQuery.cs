using Domain.Entities;

public class GetProjectByIdQuery
{
    public int Id { get; set; }
}

public class GetProjectByIdQueryHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProjectByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Project> Handle(GetProjectByIdQuery request)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(request.Id);
        return project;
    }
}
