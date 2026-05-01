using Domain.DTOs;
using Domain.Entities;

public class UpdateManyProjectCommand
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class UpdateManyProjectCommandHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateManyProjectCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProjectDTO>> Handle(List<UpdateManyProjectCommand> request)
    {
        var results = new List<ProjectDTO>();
        foreach (var command in request){
            var project = await _unitOfWork.Projects.GetByIdAsync(command.Id);
            if (project == null)
            {
                var error = $"Project with Id {command.Id} not found";
                results.Add(ToDTO(new Project { Id = command.Id }, error));
                continue;
            }

            project.Name = command.Name;
            _unitOfWork.Projects.Update(project);

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
