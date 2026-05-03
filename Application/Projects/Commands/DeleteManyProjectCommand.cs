using Domain.DTOs;
using Domain.Entities;

public class DeleteManyProjectCommand
{
    public int Id { get; set; }
}

public class DeleteManyProjectCommandHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteManyProjectCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProjectDTO>> Handle(List<DeleteManyProjectCommand> command)
    {
        var results = new List<ProjectDTO>();
        foreach (var request in command)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(request.Id);
            if (project == null)
            {
                var error = $"Project with Id {request.Id} not found";
                results.Add(ToDTO(new Project { Id = request.Id }, error));
                continue;
            }

            await _unitOfWork.Projects.DeleteAsync(project);
            results.Add(ToDTO(project));
        }
        
        return results;
    }
    
    private ProjectDTO ToDTO(Project project, string error = null)
    {
        return new ProjectDTO
        {
            Id = project.Id,
            Name = project.Name,
            Error = error
        };
    }
}
