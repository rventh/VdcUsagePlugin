// Copyright © Veeam Software Group GmbH.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyPlugin.Service.Registration;

internal static class TaskExtensions
{
    public static Task IgnoreFaultOrCancellation(
        this Task task,
        TaskContinuationOptions options = TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously,
        TaskScheduler? scheduler = null)
    {
        if (task == null) throw new ArgumentNullException(nameof(task), "task != null");

        if (scheduler == null) scheduler = TaskScheduler.Current ?? TaskScheduler.Default;

        if (task.IsCompleted)
        {
            var _ = task.Exception; // observe exception
            return Task.CompletedTask;
        }

        return task.ContinueWith(completedTask =>
        {
            var _ = completedTask.Exception; // observe exception
        }, CancellationToken.None, options, scheduler);
    }
}
