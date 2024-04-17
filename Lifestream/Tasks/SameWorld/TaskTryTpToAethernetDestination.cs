﻿using ECommons.Automation.NeoTaskManager.Tasks;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld;

internal static class TaskTryTpToAethernetDestination
{
    public static void Enqueue(string targetName)
    {
        if (P.Config.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        if (P.ActiveAetheryte != null)
        {
            P.TaskManager.Enqueue(Process);
        }
        else
        {
            P.TaskManager.Enqueue(() =>
            {
                if (P.ActiveAetheryte == null && Utils.GetReachableWorldChangeAetheryte() != null)
                {
                    P.TaskManager.InsertMulti(
                        new FrameDelayTask(10),
                        new(WorldChange.TargetReachableAetheryte),
                        new(WorldChange.LockOn),
                        new(WorldChange.EnableAutomove),
                        new(WorldChange.WaitUntilWorldChangeAetheryteExists),
                        new(WorldChange.DisableAutomove)
                        );
                }
            }, "ConditionalLockonTask");
            P.TaskManager.Enqueue(WorldChange.WaitUntilWorldChangeAetheryteExists);
            P.TaskManager.EnqueueDelay(10, true);
            P.TaskManager.Enqueue(Process);
        }

        void Process()
        {
            var master = Utils.GetMaster();
            {
                if (P.ActiveAetheryte != master)
                {
                    var name = master.Name;
                    if (name.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName) || P.Config.Renames.TryGetValue(master.ID, out var value) && value.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName))
                    {
                        TaskRemoveAfkStatus.Enqueue();
                        TaskAethernetTeleport.Enqueue(master);
                        return;
                    }
                }
            }

            foreach (var x in P.DataStore.Aetherytes[master])
            {
                if (P.ActiveAetheryte != x)
                {
                    var name = x.Name;
                    if (name.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName) || P.Config.Renames.TryGetValue(x.ID, out var value) && value.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName))
                    {
                        TaskRemoveAfkStatus.Enqueue();
                        TaskAethernetTeleport.Enqueue(x);
                        return;
                    }
                }
            }

            if (P.ActiveAetheryte.Value.ID == 70 && P.Config.Firmament)
            {
                var name = "Firmament";
                if (name.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName))
                {
                    TaskRemoveAfkStatus.Enqueue();
                    TaskFirmanentTeleport.Enqueue();
                    return;
                }
            }
            Notify.Error($"No destination {targetName} found");
            return;
        }
    }
}
