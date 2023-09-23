﻿using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Impostor;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;
public static class Doppelganger
{
    private static readonly int Id = 194200;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static OptionItem KillCooldown;
    public static OptionItem MaxSteals;

    public static Dictionary<byte, string> DoppelVictim = new();
    public static Dictionary<byte, int> TotalSteals = new();


    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Bandit);
        MaxSteals = IntegerOptionItem.Create(Id + 10, "DoppelMaxSteals", new(1, 20, 1), 4, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doppelganger]);
        KillCooldown = FloatOptionItem.Create(Id + 11, "DoppelKillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doppelganger])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public static void Init()
    {
        playerIdList = new();
        DoppelVictim = new();
        TotalSteals = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
        TotalSteals.Add(playerId, 0);
        if (playerId == PlayerControl.LocalPlayer.PlayerId) DoppelVictim[playerId] = Main.nickName;
        else DoppelVictim[playerId] = Utils.GetPlayerById(playerId).Data.PlayerName;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC(byte playerId, bool isTargetList = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDoppelgangerStealLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(TotalSteals[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (TotalSteals.ContainsKey(PlayerId))
            TotalSteals[PlayerId] = Limit;
        else
            TotalSteals.Add(PlayerId, 0);
    }

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public static void RpcChangeSkin(PlayerControl pc, GameData.PlayerOutfit newOutfit)
    {
        var sender = CustomRpcSender.Create(name: $"Doppelganger.RpcChangeSkin({pc.Data.PlayerName})");
        if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) Main.nickName = newOutfit.PlayerName;
        else pc.RpcSetName(newOutfit.PlayerName);

        pc.SetColor(newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetColor)
            .Write(newOutfit.ColorId)
        .EndRpc();

        pc.SetHat(newOutfit.HatId, newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetHatStr)
            .Write(newOutfit.HatId)
        .EndRpc();

        pc.SetSkin(newOutfit.SkinId, newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetSkinStr)
            .Write(newOutfit.SkinId)
        .EndRpc();

        pc.SetVisor(newOutfit.VisorId, newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetVisorStr)
            .Write(newOutfit.VisorId)
        .EndRpc();

        pc.SetPet(newOutfit.PetId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetPetStr)
            .Write(newOutfit.PetId)
            .EndRpc();

        sender.SendMessage();
    }

    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || !IsEnable || Camouflage.IsCamouflage || Camouflager.IsActive) return;
        if (TotalSteals[killer.PlayerId] >= MaxSteals.GetInt())
        {
            TotalSteals[killer.PlayerId] = MaxSteals.GetInt();
            return;
        }
        if (target.Is(CustomRoles.Pestilence) ||
            target.Is(CustomRoles.Glitch) ||
            (target.Is(CustomRoles.Guardian) && target.AllTasksCompleted()) ||
            (target.Is(CustomRoles.Opportunist) && target.AllTasksCompleted() && Options.OppoImmuneToAttacksWhenTasksDone.GetBool()) ||
            (target.Is(CustomRoles.Veteran) && Main.VeteranInProtect.ContainsKey(target.PlayerId)) ||
            (target.Is(CustomRoles.Jinx) && Main.JinxSpellCount[target.PlayerId] > 0) ||
            (target.Is(CustomRoles.CursedWolf) && Main.CursedWolfSpellCount[target.PlayerId] > 0))  return;
        TotalSteals[killer.PlayerId]++;
        var killerSkin = new GameData.PlayerOutfit()
            .Set(killer.Data.PlayerName, killer.CurrentOutfit.ColorId, killer.CurrentOutfit.HatId, killer.CurrentOutfit.SkinId, killer.CurrentOutfit.VisorId, killer.CurrentOutfit.PetId);
        var targetSkin = new GameData.PlayerOutfit()
            .Set(target.Data.PlayerName, target.CurrentOutfit.ColorId, target.CurrentOutfit.HatId, target.CurrentOutfit.SkinId, target.CurrentOutfit.VisorId, target.CurrentOutfit.PetId);

        RpcChangeSkin(killer, targetSkin);
        RpcChangeSkin(target, killerSkin);
        DoppelVictim[target.PlayerId] = target.Data.PlayerName;
        SendRPC(killer.PlayerId);
        Utils.NotifyRoles();
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return;
    }

    public static string GetStealLimit(byte playerId) => Utils.ColorString(TotalSteals[playerId] < MaxSteals.GetInt() ? Utils.GetRoleColor(CustomRoles.Doppelganger).ShadeColor(0.25f) : Color.gray, TotalSteals.TryGetValue(playerId, out var stealLimit) ? $"({MaxSteals.GetInt() - stealLimit})" : "Invalid");
}
// StartEndGame()
// if (PlayerControl.LocalPlayer.Is(CustomRoles.NSerialKiller) || NSerialKiller.SKVictim.Contains(PlayerControl.LocalPlayer.PlayerId)) Main.nickName = NSerialKiller.HostOGName;

