using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Modules.Scheduling.Contracts;
using Elsa.Runtime.Middleware;
using Elsa.Runtime.Models;
using Elsa.Runtime.Notifications;

namespace Elsa.Modules.Scheduling.Handlers;

/// <summary>
/// Updates scheduled jobs based on updated workflow triggers and bookmarks.
/// </summary>
public class ScheduleWorkflows : INotificationHandler<WorkflowTriggersIndexed>, INotificationHandler<WorkflowBookmarksIndexed>
{
    private readonly IWorkflowTriggerScheduler _workflowTriggerScheduler;
    private readonly IWorkflowBookmarkScheduler _workflowBookmarkScheduler;

    public ScheduleWorkflows(IWorkflowTriggerScheduler workflowTriggerScheduler, IWorkflowBookmarkScheduler workflowBookmarkScheduler)
    {
        _workflowTriggerScheduler = workflowTriggerScheduler;
        _workflowBookmarkScheduler = workflowBookmarkScheduler;
    }

    public async Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        await _workflowTriggerScheduler.UnscheduleTriggersAsync(notification.IndexedWorkflowTriggers.RemovedTriggers, cancellationToken);
        await _workflowTriggerScheduler.ScheduleTriggersAsync(notification.IndexedWorkflowTriggers.AddedTriggers, cancellationToken);
    }

    public async Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        var workflowInstanceId = notification.IndexedWorkflowBookmarks.WorkflowState.Id;
        await _workflowBookmarkScheduler.UnscheduleBookmarksAsync(workflowInstanceId, notification.IndexedWorkflowBookmarks.RemovedBookmarks, cancellationToken);
        await _workflowBookmarkScheduler.ScheduleBookmarksAsync(workflowInstanceId, notification.IndexedWorkflowBookmarks.AddedBookmarks, cancellationToken);
    }
}