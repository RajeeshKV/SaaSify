using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly GetProjectByIdQueryHandler _getProjectByIdHandler;
    private readonly GetAllProjectsQueryHandler _getAllProjectsHandler;
    private readonly CreateProjectCommandHandler _createProjectHandler;
    private readonly UpdateProjectCommandHandler _updateProjectHandler;
    private readonly UpdateManyProjectCommandHandler _updateManyProjectHandler;
    private readonly DeleteProjectCommandHandler _deleteProjectHandler;
    private readonly DeleteManyProjectCommandHandler _deleteManyProjectHandler;

    public ProjectsController(
        GetProjectByIdQueryHandler getProjectByIdHandler,
        GetAllProjectsQueryHandler getAllProjectsHandler,
        CreateProjectCommandHandler createProjectHandler,
        UpdateProjectCommandHandler updateProjectHandler,
        UpdateManyProjectCommandHandler updateManyProjectHandler,
        DeleteProjectCommandHandler deleteProjectHandler,
        DeleteManyProjectCommandHandler deleteManyProjectHandler)
    {
        _getProjectByIdHandler = getProjectByIdHandler;
        _getAllProjectsHandler = getAllProjectsHandler;
        _createProjectHandler = createProjectHandler;
        _updateProjectHandler = updateProjectHandler;
        _updateManyProjectHandler = updateManyProjectHandler;
        _deleteProjectHandler = deleteProjectHandler;
        _deleteManyProjectHandler = deleteManyProjectHandler;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var query = new GetProjectByIdQuery { Id = id };
        var result = await _getProjectByIdHandler.Handle(query);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = new GetAllProjectsQuery();
        var result = await _getAllProjectsHandler.Handle(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectCommand command)
    {
        var result = await _createProjectHandler.Handle(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectCommand command)
    {
        command.Id = id;
        var result = await _updateProjectHandler.Handle(command);
        return Ok(result);
    }

    [HttpPut("many")]
    public async Task<IActionResult> UpdateMany([FromBody] List<UpdateManyProjectCommand> command)
    {
        var result = await _updateManyProjectHandler.Handle(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var command = new DeleteProjectCommand { Id = id };
        await _deleteProjectHandler.Handle(command);
        return NoContent();
    }

    [HttpDelete("many")]
    public async Task<IActionResult> DeleteMany([FromBody] List<DeleteManyProjectCommand> command)
    {
        await _deleteManyProjectHandler.Handle(command);
        return NoContent();
    }
}
