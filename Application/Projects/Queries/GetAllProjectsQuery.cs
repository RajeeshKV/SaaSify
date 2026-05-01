using Domain.Entities;

public class GetAllProjectsQuery
{
}

public class GetAllProjectsQueryHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllProjectsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Project>> Handle(GetAllProjectsQuery request)
    {
        var projects = await _unitOfWork.Projects.GetAllAsync();
        return projects;
    }
}
