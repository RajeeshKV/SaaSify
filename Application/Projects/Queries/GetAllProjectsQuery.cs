using Application.Common.Pagination;
using Domain.Entities;

public class GetAllProjectsQuery : PaginationParameters
{
    public GetAllProjectsQuery()
    {
    }

    public GetAllProjectsQuery(int pageNumber, int pageSize) : base(pageNumber, pageSize)
    {
    }
}

public class GetAllProjectsQueryHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllProjectsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginatedResponse<Project>> Handle(GetAllProjectsQuery request)
    {
        var projects = await _unitOfWork.Projects.GetAllAsync();
        var totalItems = projects.Count();
        
        var paginatedProjects = projects
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PaginatedResponse<Project>(
            paginatedProjects,
            request.PageNumber,
            request.PageSize,
            totalItems
        );
    }
}
