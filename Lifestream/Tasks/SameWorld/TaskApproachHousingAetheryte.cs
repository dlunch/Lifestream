﻿using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using Lifestream.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lifestream.Tasks.SameWorld;
public static class TaskApproachHousingAetheryte
{
    public static readonly (Vector3 Pos, float Distance) EmpyreumIMP = (new Vector3(20.540209f, -15.2f, 179.47063f), 29.78f);
    public static readonly (Vector3 Pos, float Distance) LavenderIMP = (new Vector3(3.1033986f, 2.8884888f, 191.80864f), 9.35f);
    public static readonly (Vector3 Pos, float Distance) ShiroIMP = (new Vector3(-103.11274f, 2.02f, 129.29942f), 8f);
    public static void Enqueue()
    {
        P.TaskManager.EnqueueMulti(
            P.Config.WaitForScreenReady?new(Utils.WaitForScreen):null,
            new(FaceIMP),
            new(MoveIMP),
            new(WaitUntilArrivesAtIMP),
            new(TargetNearestShard),
            new(WorldChange.LockOn),
            new(WorldChange.EnableAutomove),
            new(() => P.ResidentialAethernet.ActiveAetheryte != null, "Wait until residential aetheryte exists"),
            new(WorldChange.DisableAutomove)
            );
    }

    /*public static void MoveToIMP()
    {
        P.FollowPath.Stop();
        if (Svc.ClientState.TerritoryType == ResidentalAreas.Empyreum)
        {
            P.FollowPath.Waypoints.Add(EmpyreumIMP.Pos);
        }
        if (Svc.ClientState.TerritoryType == ResidentalAreas.The_Lavender_Beds)
        {
            P.FollowPath.Waypoints.Add(LavenderIMP.Pos);
        }
        if (Svc.ClientState.TerritoryType == ResidentalAreas.Shirogane)
        {
            P.FollowPath.Waypoints.Add(ShiroIMP.Pos);
        }
    }*/

    public static void FaceIMP()
    {
        if (Svc.ClientState.TerritoryType == ResidentalAreas.Empyreum)
        {
            P.Memory.FaceTarget(EmpyreumIMP.Pos);
        }
    }

    public static void MoveIMP()
    {
        if(Svc.ClientState.TerritoryType.EqualsAny(ResidentalAreas.Empyreum, ResidentalAreas.Shirogane, ResidentalAreas.The_Lavender_Beds))
        {
            WorldChange.EnableAutomove();
        }
    }

    public static bool WaitUntilArrivesAtIMP()
    {
        if (Svc.ClientState.TerritoryType == ResidentalAreas.Empyreum)
        {
            return Svc.Objects.Any(x => Utils.AethernetShards.Contains(x.DataId) && Vector3.Distance(Player.Object.Position, x.Position) < EmpyreumIMP.Distance);
        }
        if (Svc.ClientState.TerritoryType == ResidentalAreas.The_Lavender_Beds)
        {
            return Svc.Objects.Any(x => Utils.AethernetShards.Contains(x.DataId) && Vector3.Distance(Player.Object.Position, x.Position) < LavenderIMP.Distance);
        }
        if (Svc.ClientState.TerritoryType == ResidentalAreas.Shirogane)
        {
            return Player.Object.Position.Z < 128f;
        }
        return true;
    }

    //public static bool WaitUntilMovementStopped() => P.FollowPath.Waypoints.Count == 0;

    public static bool TargetNearestShard()
    {
        if (!Player.Interactable) return false;
        foreach(var x in Svc.Objects.OrderBy(z => Vector3.Distance(Player.Object.Position, z.Position)))
        {
            if(Utils.AethernetShards.Contains(x.DataId) && x.IsTargetable && x.ObjectKind == ObjectKind.EventObj)
            {
                if (EzThrottler.Throttle("TargetNearestShard"))
                {
                    Svc.Targets.SetTarget(x);
                    return true;
                }
            }
        }
        return false;
    }
}
