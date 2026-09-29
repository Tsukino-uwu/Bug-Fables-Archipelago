using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Color = UnityEngine.Color;

namespace BugFablesAP
{
    internal static partial class ItemSwap
    {
        // Turns a location pickup's add into |additemtoss,3,...| (crystal-berry kind: adds nothing, closes the box
        // the same way); the |flag,...| before it still marks the pickup taken, so the check is sent.
        private static class Pickups
        {
            [HarmonyPatch(typeof(MainManager), "SetText", typeof(string), typeof(int), typeof(float?), typeof(bool),
                typeof(bool), typeof(Vector3), typeof(Vector3), typeof(Vector2), typeof(Transform), typeof(NPCControl))]
            [HarmonyPrefix]
            public static void PickupPrefix(ref string text, NPCControl caller)
            {
                if (caller == null || caller.objecttype != NPCControl.ObjectTypes.Item || text == null
                    || caller.entity == null)
                {
                    return;
                }
                int kind = caller.entity.animid;
                string add = "|additemtoss," + kind + ",var,0|";
                if (kind < 0 || kind > 3 || !text.Contains(add))
                {
                    return;
                }
                long at = FindPickup(caller);
                if (at < 0)
                {
                    return;
                }
                bool respawning = connection.LocationPickups[at].Regional >= 0;
                if (respawning)
                {
                    // Once its check is done, a respawning pickup is the game's own again.
                    if (connection.IsDone(at))
                    {
                        log.LogInfo($"[swap] location {at}: respawning pickup on {MapName()}, check already done: vanilla item");
                        return;
                    }
                    // The game marks nothing LocationChecks could read later, so its check goes out now.
                    connection.QueueRespawnCheck(at, MainManager.instance.flagstring[ItemReceiver.SeedSlot]);
                }
                ScoutedItemInfo info = Describe(at, out string name, out Sprite sprite, out Color? color);
                Mark(caller.entity, null);
                string article = info != null && IsOurs(info) ? ArticleOf(info.ItemId, KindOf(info)) : null;
                // "You found |string,1| ...": the seed item's own article, none for a member or another player's item.
                bool other = ForOther(info, ref name);
                if (other || article == "")
                {
                    text = text.Replace(ArticleSlot, "");
                }
                // The pickup line ends its name in the game's red ("...|string,0||color,1|!"); after a name in the
                // Item colors that "!" looked stray, so it ends in black as the gift line does.
                if (other && QualityOfLife.ApColors)
                {
                    text = text.Replace(NameThenRed, NameThenBlack);
                }
                else if (article != null)
                {
                    MainManager.instance.flagstring[GameStrings.ItemArticle] = article;
                }
                MainManager.instance.flagstring[GameStrings.ItemName] = name;
                SpriteRenderer held = caller.entity.sprite;
                if (sprite != null && held != null)
                {
                    held.sprite = sprite;
                }
                Transform back = held == null ? null : held.transform.Find("back");
                SpriteRenderer backRenderer = back == null ? null : back.GetComponent<SpriteRenderer>();
                if (color.HasValue && backRenderer != null)
                {
                    backRenderer.material.color = color.Value;
                }
                // Replace the vanilla box at once: DestroyDescWindow would shrink it out while the new one grows.
                if (descWindowField != null && descWindowField.GetValue(caller) is DialogueAnim box && box != null)
                {
                    UnityEngine.Object.Destroy(box.gameObject);
                    descWindowField.SetValue(caller, null);
                }
                ShowOwnDescription(caller, info);
                text = text.Replace(add, "|additemtoss,3,var,0|");
                if (kind == 3)
                {
                    // The berry's pickup code already marked it taken and raised the count: undo the count, keep the
                    // mark.
                    MainManager.instance.flagvar[GameVars.CrystalBerries]--;
                    text = text.Replace(FirstBerryTutorial + "|break|", "").Replace(FirstBerryTutorial, "");
                    ShowAsSprite(caller.entity, sprite);
                }
                text = text.Replace(FirstMedalTutorial + "|break|", "").Replace(FirstMedalTutorial, "");
                if (!connection.IsDone(at))
                {
                    ShownInScene.Add(at); // a done check's item only comes back as a replay, which keeps its box
                }
                log.LogInfo($"[swap] location {at}: pickup (kind {kind}, id {caller.entity.animstate}, flag {caller.activationflag}) "
                    + $"on {MapName()} is a location; showing '{name}'" + (info == null ? " (not scouted yet)" : ""));
            }
        }

        internal static void TickGround()
        {
            if (Time.frameCount % 15 != 0 || connection == null || !randomizerOn())
            {
                return;
            }
            Dictionary<long, ApConnection.Pickup> pickups = connection.LocationPickups;
            MapControl map = MainManager.map;
            if (pickups == null || map == null)
            {
                return;
            }
            string mapName = map.mapid.ToString();
            NPCControl[] entities = null;
            HideFoundElsewhere(pickups, map, mapName, ref entities);
            foreach (KeyValuePair<long, ApConnection.Pickup> entry in pickups)
            {
                if (entry.Value.Map != mapName || (entry.Value.Regional >= 0 && connection.IsDone(entry.Key)))
                {
                    continue;
                }
                Describe(entry.Key, out _, out Sprite sprite, out _);
                if (sprite == null)
                {
                    continue;
                }
                entities = entities ?? map.GetComponentsInChildren<NPCControl>(true);
                foreach (NPCControl npc in entities)
                {
                    EntityControl entity = npc.entity;
                    if (npc.objecttype != NPCControl.ObjectTypes.Item || !IsPickup(entry.Value, npc)
                        || entity == null || entity.sprite == null)
                    {
                        continue;
                    }
                    // A crystal berry every time: the game shows its model again after it's hidden.
                    if (entity.animid == 3)
                    {
                        ShowAsSprite(entity, sprite);
                        Mark(entity, MarkColorOf(entry.Key));
                        continue;
                    }
                    if (entity.sprite.sprite != sprite)
                    {
                        entity.sprite.sprite = sprite;
                    }
                    // Sets the lift too (half the sprite's height, raised while marked).
                    Mark(entity, MarkColorOf(entry.Key));
                }
            }
        }

        // A one-time pickup another client on this slot found while this player is in its room goes at once, as the
        // game hides an entity (NPCControl.Start). Only one this save hasn't taken: the player's own pickup is the
        // game's to end.
        private static void HideFoundElsewhere(Dictionary<long, ApConnection.Pickup> pickups, MapControl map,
            string mapName,
            ref NPCControl[] entities)
        {
            MainManager mm = MainManager.instance;
            if (ItemReceiver.Busy(mm) != null)
            {
                return;
            }
            foreach (KeyValuePair<long, ApConnection.Pickup> entry in pickups)
            {
                ApConnection.Pickup pickup = entry.Value;
                if (pickup.Map != mapName || pickup.Regional >= 0 || pickup.Event >= 0 || !connection.IsDone(entry.Key)
                    || TakenHere(mm, pickup))
                {
                    continue;
                }
                entities = entities ?? map.GetComponentsInChildren<NPCControl>(true);
                foreach (NPCControl npc in entities)
                {
                    if (npc.gameObject.activeSelf && npc.objecttype == NPCControl.ObjectTypes.Item
                        && IsPickup(pickup, npc))
                    {
                        KeptOpen.KeepAway(npc);
                        npc.gameObject.SetActive(false);
                        log.LogInfo($"[swap] location {entry.Key}: found by another client on this slot; its pickup on {mapName} hidden");
                    }
                }
            }
        }

        private static bool TakenHere(MainManager mm, ApConnection.Pickup pickup) =>
            pickup.Berry >= 0 ? pickup.Berry < mm.crystalbflags.Length && mm.crystalbflags[pickup.Berry]
                : pickup.Flag >= 0 && pickup.Flag < mm.flags.Length && mm.flags[pickup.Flag];

        internal static bool IsPickup(ApConnection.Pickup pickup, NPCControl npc)
        {
            if (pickup.Berry >= 0)
            {
                return npc.entity != null && npc.entity.animid == 3 && npc.data != null && npc.data.Length > 0
                    && npc.data[0] == pickup.Berry;
            }
            if (pickup.Regional >= 0)
            {
                return npc.activationflag <= 0 && npc.regionalflag == pickup.Regional;
            }
            if (pickup.Event >= 0)
            {
                // A story pickup has no flag of its own: match the story event it starts (data[1]), not the entity
                // name.
                return npc.data != null && npc.data.Length > 1 && npc.data[1] == pickup.Event;
            }
            return npc.activationflag >= 0 && npc.activationflag == pickup.Flag;
        }

        // |giveitem,-1,<amount>| (berries) never reaches the stand-ins, so at a berry location it becomes a hand-over
        // of item 0 marked as that location, which the stand-ins then swap.
        private static long pendingBerries = -1;

        private static class Berries
        {
            [HarmonyPatch(typeof(MainManager), "SetText", typeof(string), typeof(int), typeof(float?), typeof(bool),
                typeof(bool), typeof(Vector3), typeof(Vector3), typeof(Vector2), typeof(Transform), typeof(NPCControl))]
            [HarmonyPrefix]
            public static void BerryPrefix(ref string text)
            {
                Dictionary<long, ApConnection.Give> gives = connection?.LocationGives;
                string map = MapName();
                if (text == null || gives == null || map == null || randomizerOn == null || !randomizerOn()
                    || !text.Contains("|giveitem,-1,"))
                {
                    return;
                }
                foreach (KeyValuePair<long, ApConnection.Give> entry in gives)
                {
                    ApConnection.Give give = entry.Value;
                    string token = "|giveitem,-1," + give.Item + ",";
                    int at = text.IndexOf(token, StringComparison.Ordinal);
                    if (give.Type != -1 || give.Map != map || at < 0)
                    {
                        continue;
                    }
                    text = text.Substring(0, at) + "|giveitem,0,0," + text.Substring(at + token.Length);
                    pendingBerries = entry.Key;
                    log.LogInfo($"[swap] location {entry.Key}: berry reward ({give.Item}) on {map} turned into a hand-over to swap");
                    return;
                }
            }
        }

        private static long FindPickup(NPCControl caller)
        {
            Dictionary<long, ApConnection.Pickup> pickups = connection.LocationPickups;
            string map = MapName();
            if (!randomizerOn() || pickups == null || map == null)
            {
                return -1;
            }
            foreach (KeyValuePair<long, ApConnection.Pickup> entry in pickups)
            {
                if (entry.Value.Map == map && IsPickup(entry.Value, caller))
                {
                    return entry.Key;
                }
            }
            return -1;
        }
    }
}
