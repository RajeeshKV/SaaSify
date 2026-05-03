using Domain.Entities;

public class UpdateProjectCommand
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class UpdateProjectCommandHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProjectCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Project> Handle(UpdateProjectCommand request)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(request.Id);
        if (project == null)
            throw new Exception("Project not found");

        project.Name = request.Name;
        await _unitOfWork.Projects.UpdateAsync(project);
        await _unitOfWork.SaveChangesAsync();

        return project;
    }
}
