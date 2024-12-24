﻿using ECommons.Configuration;
using ECommons.Events;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using Lifestream.Data;
using Lifestream.Tasks.Shortcuts;
using Lumina.Excel.GeneratedSheets;
using Path = System.IO.Path;

namespace Lifestream.Systems.Legacy;

internal class DataStore
{
    internal const string FileName = "StaticData.json";
    internal uint[] Territories;
    internal Dictionary<TinyAetheryte, List<TinyAetheryte>> Aetherytes = [];
    internal string[] Worlds = Array.Empty<string>();
    internal string[] DCWorlds = Array.Empty<string>();
    internal Dictionary<TaskISShortcut.IslandNPC, string[]> IslandNPCs = [];
    internal StaticData StaticData;

    internal TinyAetheryte GetMaster(TinyAetheryte aetheryte)
    {
        foreach(var x in Aetherytes.Keys)
        {
            if(x.Group == aetheryte.Group) return x;
        }
        return default;
    }

    internal DataStore()
    {
        var terr = new List<uint>();
        StaticData = EzConfig.LoadConfiguration<StaticData>(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, FileName), false);
        Svc.Data.GetExcelSheet<Aetheryte>().Each(x =>
        {
            if(x.AethernetGroup != 0)
            {
                if(x.IsAetheryte)
                {
                    Aetherytes[GetTinyAetheryte(x)] = [];
                    terr.Add(x.Territory.Value.RowId);
                }
            }
        });
        Svc.Data.GetExcelSheet<Aetheryte>().Each(x =>
        {
            if(x.AethernetGroup != 0)
            {
                if(!x.IsAetheryte)
                {
                    var a = GetTinyAetheryte(x);
                    Aetherytes[GetMaster(a)].Add(a);
                    terr.Add(x.Territory.Value.RowId);
                }
            }
        });
        foreach(var x in Aetherytes.Keys.ToArray())
        {
            Aetherytes[x] = [.. Aetherytes[x].OrderBy(x => GetAetheryteSortOrder(x.ID))];
        }
        Territories = [.. terr];
        if(ProperOnLogin.PlayerPresent)
        {
            BuildWorlds();
        }

        foreach(TaskISShortcut.IslandNPC npc in Enum.GetValues(typeof(TaskISShortcut.IslandNPC)))
        {
            var row = Svc.Data.GetExcelSheet<ENpcResident>().GetRow((uint)npc);
            IslandNPCs.Add(npc, [row.Singular, row.Title]);
        }
    }

    internal uint GetAetheryteSortOrder(uint id)
    {
        var ret = 10000u;
        if(StaticData.SortOrder.TryGetValue(id, out var x))
        {
            ret += x;
        }
        if(P.Config.Favorites.Contains(id))
        {
            ret -= 10000u;
        }
        return ret;
    }

    internal void BuildWorlds()
    {
        BuildWorlds(Svc.ClientState.LocalPlayer.CurrentWorld.GameData.DataCenter.Value.RowId);
        if(Player.Available)
        {
            if(P.AutoRetainerApi?.Ready == true && P.Config.UseAutoRetainerAccounts)
            {
                var data = P.AutoRetainerApi.GetOfflineCharacterData(Player.CID);
                if(data != null)
                {
                    P.Config.ServiceAccounts[Player.NameWithWorld] = data.ServiceAccount;
                }
            }
            else if(!P.Config.ServiceAccounts.ContainsKey(Player.NameWithWorld))
            {
                P.Config.ServiceAccounts[Player.NameWithWorld] = -1;
            }
        }
    }

    internal void BuildWorlds(uint dc)
    {
        // korea
        if (Svc.ClientState.LocalPlayer.CurrentWorld.GameData.Region == 3)
        {
            Worlds = ["카벙클", "초코보", "모그리", "톤베리", "펜리르"];
            DCWorlds = [];

            return;
        }

        Worlds = [.. Svc.Data.GetExcelSheet<World>().Where(x => x.DataCenter.Value.RowId == dc && x.IsPublic()).Select(x => x.Name.ToString()).Order()];
        PluginLog.Debug($"Built worlds: {Worlds.Print()}");
        DCWorlds = Svc.Data.GetExcelSheet<World>().Where(x => x.DataCenter.Value.RowId != dc && x.IsPublic() && (x.DataCenter.Value.Region == Player.Object.HomeWorld.GameData.DataCenter.Value.Region || x.DataCenter.Value.Region == 4)).Select(x => x.Name.ToString()).ToArray();
        PluginLog.Debug($"Built DCworlds: {DCWorlds.Print()}");
    }

    internal TinyAetheryte GetTinyAetheryte(Aetheryte aetheryte)
    {
        var AethersX = 0f;
        var AethersY = 0f;
        if(StaticData.CustomPositions.TryGetValue(aetheryte.RowId, out var pos))
        {
            AethersX = pos.X;
            AethersY = pos.Z;
        }
        else
        {
            var map = Svc.Data.GetExcelSheet<Map>().FirstOrDefault(m => m.TerritoryType.Row == aetheryte.Territory.Value.RowId);
            var scale = map.SizeFactor;
            var mapMarker = Svc.Data.GetExcelSheet<MapMarker>().FirstOrDefault(m => m.DataType == (aetheryte.IsAetheryte ? 3 : 4) && m.DataKey == (aetheryte.IsAetheryte ? aetheryte.RowId : aetheryte.AethernetName.Value.RowId));
            if(mapMarker != null)
            {
                AethersX = Utils.ConvertMapMarkerToRawPosition(mapMarker.X, scale);
                AethersY = Utils.ConvertMapMarkerToRawPosition(mapMarker.Y, scale);
            }
        }
        return new(new(AethersX, AethersY), aetheryte.Territory.Value.RowId, aetheryte.RowId, aetheryte.AethernetGroup);
    }
}
