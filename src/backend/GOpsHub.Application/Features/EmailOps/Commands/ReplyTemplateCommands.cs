using GOpsHub.Application.Common.CQRS;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;

namespace GOpsHub.Application.Features.EmailOps.Commands;

public record CreateReplyTemplateCommand(
    string TemplateName,
    string Category,
    string Content,
    string Language = "vi"
) : ICommand<ReplyTemplate>;

public class CreateReplyTemplateCommandHandler : ICommandHandler<CreateReplyTemplateCommand, ReplyTemplate>
{
    private readonly IRepository<ReplyTemplate> _templateRepo;

    public CreateReplyTemplateCommandHandler(IRepository<ReplyTemplate> templateRepo)
    {
        _templateRepo = templateRepo;
    }

    public async Task<ReplyTemplate> HandleAsync(CreateReplyTemplateCommand command, CancellationToken ct = default)
    {
        var template = new ReplyTemplate
        {
            TemplateName = command.TemplateName,
            Category = command.Category,
            Content = command.Content,
            Language = command.Language
        };

        return await _templateRepo.CreateAsync(template, ct);
    }
}
